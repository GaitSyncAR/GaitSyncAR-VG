using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class VitureManager : MonoBehaviour
{
    // Replace with the exact name of your compiled .so file (without "lib" or ".so")
    private const string DLL_NAME = "glasses";

    // Lifecycle Imports ---
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

    // Device Control Imports ---
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int xr_device_provider_get_duty_cycle(IntPtr handle);

    private IntPtr deviceHandle = IntPtr.Zero;

    void Start()
    {
        // On Android, must use Android's Java UsbManager to get these values
        // These are placeholder values.
        int productId = 0; 
        int fileDescriptor = 0; 

        // 1. Create
        deviceHandle = xr_device_provider_create(productId, fileDescriptor);
        
        if (deviceHandle != IntPtr.Zero)
        {
            // 2. Initialize
            int initResult = xr_device_provider_initialize(deviceHandle, null, null);
            if (initResult == 0) // 0 is Success
            {
                // 3. Start
                xr_device_provider_start(deviceHandle);
                
                // 4. Read Duty Cycle
                int dutyCycle = xr_device_provider_get_duty_cycle(deviceHandle);
                Debug.Log("Current Duty Cycle: " + dutyCycle);
            }
            else
            {
                Debug.LogError("Initialization failed with code: " + initResult);
            }
        }
    }

    void OnDestroy()
    {
        // 5. Cleanup
        if (deviceHandle != IntPtr.Zero)
        {
            xr_device_provider_stop(deviceHandle);
            xr_device_provider_shutdown(deviceHandle);
            xr_device_provider_destroy(deviceHandle);
            deviceHandle = IntPtr.Zero;
        }
    }
}