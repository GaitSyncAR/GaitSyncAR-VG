using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class VitureManager : MonoBehaviour
{
    private const string DLL_NAME = "glasses";

    // --- Lifecycle Imports ---
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr xr_device_provider_create(int product_id, int file_descriptor);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_initialize(IntPtr handle, string custom_config, string cache_file_dir);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_start(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_stop(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_shutdown(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void xr_device_provider_destroy(IntPtr handle);

    // --- Device Control Imports ---
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_get_duty_cycle(IntPtr handle);

    // --- Utility Import ---
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool xr_device_provider_is_product_id_valid(int product_id);

    // imports for rotation
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_get_gl_pose_carina(IntPtr handle, float[] pose, double predict_time, IntPtr pose_status);

    public Transform targetCamera; // Main Camera
    private float[] poseArray = new float[7]; // [px, py, pz, qw, qx, qy, qz]

    private IntPtr deviceHandle = IntPtr.Zero;
    private AndroidJavaObject usbConnection; // Keep this alive so the file descriptor doesn't close!

    void Start()
    {
        Debug.LogWarning("[VITURE] THE SCRIPT IS ALIVE AND RUNNING!");

        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.LogError("[VITURE] This script must run on an Android device.");
            return;
        }

        ConnectAndStart();
    }

    private void RequestUsbPermission(AndroidJavaObject usbManager, AndroidJavaObject usbDevice, AndroidJavaObject currentActivity)
    {
        try
        {
            string ACTION_USB_PERMISSION = "com.viture.usb.PERMISSION";
            
            // Creating the Intent
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", ACTION_USB_PERMISSION))
            {
                // Explicit request
                string packageName = currentActivity.Call<string>("getPackageName");
                intent.Call<AndroidJavaObject>("setPackage", packageName);

                // 2. Creating the PendingIntent with FLAG_MUTABLE (33554432)
                int flags = 33554432; 
                
                using (AndroidJavaClass pendingIntentClass = new AndroidJavaClass("android.app.PendingIntent"))
                {
                    using (AndroidJavaObject pendingIntent = pendingIntentClass.CallStatic<AndroidJavaObject>(
                        "getBroadcast", 
                        currentActivity, 
                        0, 
                        intent, 
                        flags))
                    {
                        // 3. Trigger the actual system popup
                        usbManager.Call("requestPermission", usbDevice, pendingIntent);
                        Debug.LogWarning("[VITURE] Permission dialog triggered! Check your phone screen.");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VITURE] Failed to request USB permission: " + e.Message);
        }
    }

    private void ConnectAndStart()
    {
        Debug.LogWarning("[VITURE] Starting USB Discovery...");
        
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject usbManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "usb"))
        using (AndroidJavaObject deviceList = usbManager.Call<AndroidJavaObject>("getDeviceList"))
        using (AndroidJavaObject values = deviceList.Call<AndroidJavaObject>("values"))
        using (AndroidJavaObject iterator = values.Call<AndroidJavaObject>("iterator"))
        {
            if (!iterator.Call<bool>("hasNext"))
            {
                Debug.LogError("[VITURE] No USB devices detected at all. Is the OTG/USB host enabled on your phone?");
            }

            while (iterator.Call<bool>("hasNext"))
            {
                using (AndroidJavaObject usbDevice = iterator.Call<AndroidJavaObject>("next"))
                {
                    int productId = usbDevice.Call<int>("getProductId");
                    Debug.LogWarning($"[VITURE] Checking Device: ProductID {productId}");

                    if (xr_device_provider_is_product_id_valid(productId))
                    {
                        Debug.LogWarning("[VITURE] VITURE Glasses detected! Requesting permission...");

                        if (usbManager.Call<bool>("hasPermission", usbDevice))
                        {
                            OpenAndInitialize(usbManager, usbDevice, productId);
                        }
                        else
                        {
                            // This is likely where you are stuck!
                            Debug.LogError("[VITURE] PERMISSION DENIED by Android. You need a Permission Intent.");
                            RequestUsbPermission(usbManager, usbDevice, currentActivity);
                        }
                        return; 
                    }
                }
            }
        }
    }

    private void OpenAndInitialize(AndroidJavaObject usbManager, AndroidJavaObject usbDevice, int productId)
    {
        usbConnection = usbManager.Call<AndroidJavaObject>("openDevice", usbDevice);
        if (usbConnection != null)
        {
            int fd = usbConnection.Call<int>("getFileDescriptor");
            Debug.LogWarning($"[VITURE] Connection Success! FD: {fd}. Calling Create...");
            
            deviceHandle = xr_device_provider_create(productId, fd);
            if (deviceHandle != IntPtr.Zero)
            {
                int res = xr_device_provider_initialize(deviceHandle, null, null);
                Debug.LogWarning($"[VITURE] Init Result: {res}");
                if (res == 0)
                {
                    xr_device_provider_start(deviceHandle);
                    StartCoroutine(LogHardwareInfoRoutine());
                }
            }
        }
        else
        {
            Debug.LogError("[VITURE] Failed to openDevice even with permission.");
        }
    }

    private void InitializeSDK(int productId, int fileDescriptor)
    {
        deviceHandle = xr_device_provider_create(productId, fileDescriptor);
        
        if (deviceHandle != IntPtr.Zero)
        {
            // Init
            int initResult = xr_device_provider_initialize(deviceHandle, null, null);
            if (initResult == 0) 
            {
                // Start
                xr_device_provider_start(deviceHandle);
                Debug.Log("[VITURE] SDK Started successfully! Starting data loop...");
                
                // Start Loop
                StartCoroutine(LogHardwareInfoRoutine());
            }
            else
            {
                Debug.LogError($"[VITURE] Initialization failed: {initResult}");
            }
        }
    }

    private IEnumerator LogHardwareInfoRoutine()
    {
        if (targetCamera == null)
        {
            Debug.LogError("[VITURE] Target Camera is null. Stopping pose tracking.");
            yield break; 
        }

        while (deviceHandle != IntPtr.Zero)
        {
            // predict_time = 0 for the most immediate data
            int result = xr_device_provider_get_gl_pose_carina(deviceHandle, poseArray, 0, IntPtr.Zero);

            if (result == 0)
            {
                // poseArray: [0=px, 1=py, 2=pz, 3=qw, 4=qx, 5=qy, 6=qz]
                Quaternion rawRot = new Quaternion(-poseArray[4], -poseArray[5], poseArray[6], poseArray[3]);

                // Applying only rotation, as dampening was found to bot be effective
                targetCamera.localRotation = Quaternion.Euler(0f, 0f, rawRot.eulerAngles.z);
            }
            
            yield return null; 
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();

        if (deviceHandle != IntPtr.Zero)
        {
            xr_device_provider_stop(deviceHandle);
            xr_device_provider_shutdown(deviceHandle);
            xr_device_provider_destroy(deviceHandle);
            deviceHandle = IntPtr.Zero;
        }

        // Cleaning up the Java USB connection
        if (usbConnection != null)
        {
            usbConnection.Call("close");
            usbConnection.Dispose();
            usbConnection = null;
        }
    }
}