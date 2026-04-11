using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BLEManager : MonoBehaviour
{
    // --------------------------- Class Fields ---------------------------
    private bool isScanning = false;
    private string connectedDeviceAddress = null;

    // Nordic UART Service UUIDs
    private readonly string nusServiceUUID = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
    private readonly string nusTxCharacteristicUUID = "6e400003-b5a3-f393-e0a9-e50e24dcca9e"; // Transmission subscribe characteristic

    // Multi-Device Management
    private int targetDeviceCount = 2; // left and right sensors
    private List<string> pendingConnections = new List<string>();
    private Dictionary<string, string> activeConnections = new Dictionary<string, string>(); // Maps MAC -> Name
    
    // -40 is basically touching the phone. -90 is across the house.
    private int closeByThreshold = -60;

    // --------------------------- Events ---------------------------
    // Other scripts will listen to these. 
    // bool = isRightFoot, uint = timestamp
    public static event Action<bool, uint> OnStepReceived; 
    
    // string = deviceName, int = batteryLevel
    public static event Action<string, int> OnBatteryLevelReceived;

    // --------------------------- Methods ---------------------------
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
        pendingConnections.Clear();

        string[] scanFilter = new string[] { nusServiceUUID };

        Debug.Log("Scanning for 2 devices...");

        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(
            scanFilter, null, (address, name, rssi, bytes) => 
            { 
                // If we found a new device we haven't logged yet
                if (!pendingConnections.Contains(address) && !activeConnections.ContainsKey(address))
                {
                    Debug.Log($"Found Target: {name} ({address})");
                    pendingConnections.Add(address);
                    activeConnections.Add(address, name); // Save name for later

                    // Once we find exactly 2 devices, STOP scanning and begin the queue
                    if (pendingConnections.Count == targetDeviceCount)
                    {
                        BluetoothLEHardwareInterface.StopScan();
                        isScanning = false;
                        ConnectNextDeviceInQueue();
                    }
                }
            }, false, false);
    }

    private void ConnectNextDeviceInQueue()
    {
        if (pendingConnections.Count == 0)
        {
            Debug.Log("ALL DEVICES SUCCESSFULLY CONNECTED!");
            return;
        }

        // Pop the first MAC address off the list
        string nextMac = pendingConnections[0];
        pendingConnections.RemoveAt(0);
        string deviceName = activeConnections[nextMac];

        Debug.Log($"Connecting to {deviceName} ({nextMac})...");

        BluetoothLEHardwareInterface.ConnectToPeripheral(nextMac, 
        
        (address) => { /* Connected */ },
        (address, service) => { /* Service Found */ },
        (address, service, characteristic) => 
        {
            // Found the TX characteristic, Subscribing to it.
            if (characteristic.ToLower() == nusTxCharacteristicUUID)
            {
                SubscribeToDeviceMessages(address, service, characteristic, deviceName);
            }
        },
        (address) => 
        {
            // Disconnect Callback
            Debug.Log($"Lost connection to {address}.");
            activeConnections.Remove(address);
        });
    }

    private void SubscribeToDeviceMessages(string address, string service, string characteristic, string deviceName)
    {
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(address, service, characteristic, 
        
        (notifyAddress, notifyCharacteristic) => {
            Debug.Log($"Subscribed to {deviceName}!");
        },
        
        (notifyAddress, notifyCharacteristic, dataBytes) => 
        {
            // empty data check
            if (dataBytes == null || dataBytes.Length == 0) return;

            // The first byte indicates the message type
            int messageType = dataBytes[0]; 

            switch (messageType)
            {
                case 1: // --- STEP EVENT ---
                    if (dataBytes.Length == 5) 
                    {
                        uint timestamp = BitConverter.ToUInt32(dataBytes, 1);
                        bool isRightFoot = deviceName.Equals("GaitSync-Right");
                        
                        // Broadcasting data if someone is listening
                        OnStepReceived?.Invoke(isRightFoot, timestamp);
                    }
                    break;

                case 2: // --- BATTERY EVENT ---
                    if (dataBytes.Length == 2)
                    {
                        int batteryLevel = dataBytes[1];
                        OnBatteryLevelReceived?.Invoke(deviceName, batteryLevel);
                    }
                    break;
            }
        });
    }
}