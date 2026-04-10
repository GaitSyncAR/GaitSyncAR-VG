using UnityEngine;
using System.Collections.Generic;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BasicBLEScanner : MonoBehaviour
{
    private bool isScanning = false;
    
    // -40 is basically touching the phone. -90 is across the house.
    // -60 is close to phone
    private int closeByThreshold = -60; 

    void Start()
    {
        #if UNITY_ANDROID
        RequestAndroidPermissions();
        #else
        InitializeBLE();
        #endif
    }

    private void RequestAndroidPermissions()
    {
#if UNITY_ANDROID
        List<string> permissionsNeeded = new List<string>();

        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
            permissionsNeeded.Add("android.permission.BLUETOOTH_SCAN");

        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
            permissionsNeeded.Add("android.permission.BLUETOOTH_CONNECT");

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            permissionsNeeded.Add(Permission.FineLocation);

        if (permissionsNeeded.Count > 0)
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => { CheckIfAllPermissionsGranted(); };
            callbacks.PermissionDenied += (perm) => { Debug.LogError("Permission Denied: " + perm); };
            
            Permission.RequestUserPermissions(permissionsNeeded.ToArray(), callbacks);
        }
        else
        {
            InitializeBLE();
        }
#endif
    }

#if UNITY_ANDROID
    private void CheckIfAllPermissionsGranted()
    {
        if (Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
        {
            InitializeBLE();
        }
    }
#endif

    private void InitializeBLE()
    {
        BluetoothLEHardwareInterface.Initialize(true, false, () => {
            Debug.Log("BLE Plugin Booted. Starting simple scan...");
            StartBasicScan();
        }, (error) => {
            Debug.LogError("BLE Initialization Error: " + error);
        });
    }

    private void StartBasicScan()
    {
        if (isScanning) return;
        isScanning = true;

        // Passing 'null' for the UUID array tells the plugin to find everything
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, null, (address, name, rssi, bytes) => {
            
            // Filter: Only care if it has a valid RSSI and is physically close
            if (rssi >= closeByThreshold && rssi < 0)
            {
                string displayName = string.IsNullOrEmpty(name) ? "Unnamed Device" : name;
                Debug.Log($"<color=green>[CLOSE DEVICE]</color> {displayName} | MAC: {address} | RSSI: {rssi}");
            }
            
        }, false, false);
    }
}