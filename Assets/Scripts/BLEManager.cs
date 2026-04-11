using UnityEngine;
using System.Collections.Generic;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BLEManager : MonoBehaviour
{
    private bool isScanning = false;
    private string connectedDeviceAddress = null;

    // Nordic UART Service UUIDs
    private readonly string nusServiceUUID = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
    private readonly string nusTxCharacteristicUUID = "6e400003-b5a3-f393-e0a9-e50e24dcca9e"; // Transmission subscribe characteristic
    
    // -40 is basically touching the phone. -90 is across the house.
    private int closeByThreshold = -60; 

    void Start()
    {
    #if UNITY_ANDROID
        RequestAndroidPermissions();
    #else
        InitializeBLE();
    #endif
    }

    private void InitializeBLE()
    {
        BluetoothLEHardwareInterface.Initialize(true, false, () => {
            Debug.Log("BLE Plugin Booted. Starting simple scan...");
            StartFilteredScan();
        }, (error) => {
            Debug.LogError("BLE Initialization Error: " + error);
        });
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

    private void CheckIfAllPermissionsGranted()
    {
        Debug.Log("All permissions granted. Initializing BLE...");
        InitializeBLE();
    }

    private void StartFilteredScan()
    {
        if (isScanning) return;
        isScanning = true;

        string[] scanFilter = new string[] { nusServiceUUID };

        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(
            scanFilter,
            null,
            (address, name, rssi, bytes) => 
            { 
                Debug.Log($"Found: {name} | MAC: {address} | RSSI: {rssi}");
                
                // Connect to the first device that gets close enough
                if (connectedDeviceAddress == null && rssi >= closeByThreshold)
                {
                    connectedDeviceAddress = address; // Lock it so we don't try connecting multiple times
                    
                    // stopping scanning before connecting to prevent radio conflicts
                    BluetoothLEHardwareInterface.StopScan();
                    isScanning = false;
                    
                    // Initiate Connection
                    ConnectToDevice(address);
                }
            }, 
            false, 
            false  
        );
    }

    private void ConnectToDevice(string deviceAddress)
    {
        Debug.Log($"<color=yellow>Attempting to connect to {deviceAddress}...</color>");

        BluetoothLEHardwareInterface.ConnectToPeripheral(deviceAddress, 
            // Callback 1: Connection Success
            (address) => {
                Debug.Log($"<color=green>Connected successfully to {address}!</color>");
            },
            
            // Callback 2: Service Discovered
            (address, serviceUUID) => {
                Debug.Log($"Discovered Service: {serviceUUID}");
            },
            
            // Callback 3: Characteristic Discovered
            (address, serviceUUID, characteristicUUID) => {
                Debug.Log($"Discovered Characteristic: {characteristicUUID}");
                
                // If we found the TX characteristic, subscribe to it immediately!
                if (characteristicUUID.ToLower() == nusTxCharacteristicUUID)
                {
                    SubscribeToDeviceMessages(address, serviceUUID, characteristicUUID);
                }
        },
        
        // Callback 4: Disconnected
        (address) => {
            Debug.Log($"<color=red>Disconnected from {address}.</color>");
            connectedDeviceAddress = null; // Reset so we can scan/connect again
        });
    }

    private void SubscribeToDeviceMessages(string address, string service, string characteristic)
    {
        Debug.Log($"Subscribing to TX Characteristic: {characteristic}...");

        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(address, service, characteristic, 
            // Notification action setup successfully
            (notifyAddress, notifyCharacteristic) => {
                Debug.Log("<color=cyan>Subscription active! Waiting for messages...</color>");
            },
            
            // Data received from the nRF52840
            (notifyAddress, notifyCharacteristic, dataBytes) => {
                
                string message = System.Text.Encoding.UTF8.GetString(dataBytes);
                Debug.Log($"<color=white><b>[nRF52840 SAYS]:</b></color> {message}");
            }
        );
    }
}