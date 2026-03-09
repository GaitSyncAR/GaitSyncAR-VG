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
    private static extern bool xr_device_provider_is_product_id_valid(int product_id);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_get_gl_pose_carina(IntPtr handle, float[] pose, double predict_time, IntPtr pose_status);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_set_film_mode(IntPtr handle, float voltage);

    [Header("Tracking Setup")]
    public Transform targetCamera; // Main Camera

    // State tracking
    private float[] poseArray = new float[7]; 
    private IntPtr deviceHandle = IntPtr.Zero;
    private AndroidJavaObject usbConnection; 
    
    // Permission tracking
    private bool isWaitingForPermission = false;
    private int pendingProductId = 0;

    void Start()
    {
        Debug.LogWarning("[VITURE] Script Alive. Starting background USB monitor...");

        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.LogError("[VITURE] This script must run on an Android device.");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogError("[VITURE] Target Camera is not assigned!");
        }

        // Start the background loop that constantly looks for the glasses
        StartCoroutine(UsbMonitorRoutine());
    }

    // ==========================================
    // 1. BACKGROUND USB MONITOR (HOT-PLUGGING)
    // ==========================================
    private IEnumerator UsbMonitorRoutine()
    {
        while (true)
        {
            // Only search if we aren't connected AND aren't currently waiting for a permission popup
            if (deviceHandle == IntPtr.Zero && !isWaitingForPermission)
            {
                CheckForGlasses();
            }
            
            // Wait 2 seconds before checking again (saves battery)
            yield return new WaitForSeconds(2.0f);
        }
    }

    private void CheckForGlasses()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject usbManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "usb"))
        using (AndroidJavaObject deviceList = usbManager.Call<AndroidJavaObject>("getDeviceList"))
        using (AndroidJavaObject values = deviceList.Call<AndroidJavaObject>("values"))
        using (AndroidJavaObject iterator = values.Call<AndroidJavaObject>("iterator"))
        {
            while (iterator.Call<bool>("hasNext"))
            {
                using (AndroidJavaObject usbDevice = iterator.Call<AndroidJavaObject>("next"))
                {
                    int productId = usbDevice.Call<int>("getProductId");

                    if (xr_device_provider_is_product_id_valid(productId))
                    {
                        if (usbManager.Call<bool>("hasPermission", usbDevice))
                        {
                            OpenAndInitialize(usbManager, usbDevice, productId);
                        }
                        else
                        {
                            RequestUsbPermission(usbManager, usbDevice, currentActivity, productId);
                        }
                        return; // Found the glasses, stop searching this loop
                    }
                }
            }
        }
    }

    // ==========================================
    // 2. PERMISSION HANDLING
    // ==========================================
    private void RequestUsbPermission(AndroidJavaObject usbManager, AndroidJavaObject usbDevice, AndroidJavaObject currentActivity, int productId)
    {
        try
        {
            string ACTION_USB_PERMISSION = "com.viture.usb.PERMISSION";
            
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", ACTION_USB_PERMISSION))
            {
                string packageName = currentActivity.Call<string>("getPackageName");
                intent.Call<AndroidJavaObject>("setPackage", packageName);

                int flags = 33554432; // FLAG_MUTABLE
                
                using (AndroidJavaClass pendingIntentClass = new AndroidJavaClass("android.app.PendingIntent"))
                {
                    using (AndroidJavaObject pendingIntent = pendingIntentClass.CallStatic<AndroidJavaObject>(
                        "getBroadcast", currentActivity, 0, intent, flags))
                    {
                        isWaitingForPermission = true;
                        pendingProductId = productId;

                        usbManager.Call("requestPermission", usbDevice, pendingIntent);
                        Debug.LogWarning("[VITURE] Permission dialog triggered! Waiting for focus return...");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VITURE] Failed to request USB permission: " + e.Message);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // When the user taps Allow/Deny, Unity regains focus.
        if (hasFocus && isWaitingForPermission)
        {
            isWaitingForPermission = false; 
            Debug.LogWarning("[VITURE] App regained focus! Re-checking USB permissions in 0.5s...");
            StartCoroutine(RetryConnectionAfterDelay());
        }
    }

    private IEnumerator RetryConnectionAfterDelay()
    {
        // Give Android OS a moment to update its internal permission state
        yield return new WaitForSeconds(0.5f);
        CheckForGlasses(); 
    }

    // ==========================================
    // 3. INITIALIZATION & HARDWARE CONTROL
    // ==========================================
    private void OpenAndInitialize(AndroidJavaObject usbManager, AndroidJavaObject usbDevice, int productId)
    {
        usbConnection = usbManager.Call<AndroidJavaObject>("openDevice", usbDevice);
        if (usbConnection != null)
        {
            int fd = usbConnection.Call<int>("getFileDescriptor");
            
            deviceHandle = xr_device_provider_create(productId, fd);
            if (deviceHandle != IntPtr.Zero)
            {
                int res = xr_device_provider_initialize(deviceHandle, null, null);
                if (res == 0)
                {
                    // Boot up the glasses
                    xr_device_provider_start(deviceHandle);
                    
                    // --- TURN OFF ELECTROCHROMIC FILTER ---
                    // 0.0f = fully transparent/off. 1.0f = fully dark.
                    int filmResult = xr_device_provider_set_film_mode(deviceHandle, 0.0f);
                    Debug.LogWarning($"[VITURE] Electrochromic film set to OFF. Result: {filmResult}");

                    // Start the head tracking loop
                    StartCoroutine(PoseTrackingRoutine());
                }
            }
        }
    }

    // ==========================================
    // 4. METRONOME POSE TRACKING
    // ==========================================
    private IEnumerator PoseTrackingRoutine()
    {
        int disconnectFailsafe = 0;

        while (deviceHandle != IntPtr.Zero)
        {
            int result = xr_device_provider_get_gl_pose_carina(deviceHandle, poseArray, 0, IntPtr.Zero);

            if (result == 0)
            {
                disconnectFailsafe = 0; // Reset failsafe on success

                if (targetCamera != null)
                {
                    // Convert to Unity coordinate system
                    Quaternion rawRot = new Quaternion(-poseArray[4], -poseArray[5], poseArray[6], poseArray[3]);

                    // METRONOME LOGIC: Isolate the Z-axis (roll) and lock X and Y
                    targetCamera.localRotation = Quaternion.Euler(0f, 0f, rawRot.eulerAngles.z);
                }
            }
            else
            {
                // If we fail to get the pose multiple times in a row, the glasses were likely unplugged
                disconnectFailsafe++;
                if (disconnectFailsafe > 10)
                {
                    Debug.LogWarning("[VITURE] Glasses disconnected mid-app! Cleaning up...");
                    CleanupDevice();
                    yield break; // Exit this coroutine. The monitor will wait for a new connection.
                }
            }
            
            yield return null; 
        }
    }

    // ==========================================
    // 5. CLEANUP
    // ==========================================
    private void CleanupDevice()
    {
        if (deviceHandle != IntPtr.Zero)
        {
            xr_device_provider_stop(deviceHandle);
            xr_device_provider_shutdown(deviceHandle);
            xr_device_provider_destroy(deviceHandle);
            deviceHandle = IntPtr.Zero;
        }

        if (usbConnection != null)
        {
            usbConnection.Call("close");
            usbConnection.Dispose();
            usbConnection = null;
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        CleanupDevice();
    }
}