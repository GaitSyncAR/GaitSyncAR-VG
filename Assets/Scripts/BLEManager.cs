using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BLEManager : MonoBehaviour
{
    // Singleton instance for global access
    public static BLEManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(this.gameObject); 
            return;
        } 

        Instance = this; 
        DontDestroyOnLoad(this.gameObject);
    }

    // --------------------------- Class Fields ---------------------------
    private bool isScanning = false;

    // Nordic UART Service UUIDs
    private readonly string nusServiceUUID = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
    private readonly string nusTxCharacteristicUUID = "6e400003-b5a3-f393-e0a9-e50e24dcca9e"; // Transmission subscribe characteristic
    private readonly string nusRxCharacteristicUUID = "6e400002-b5a3-f393-e0a9-e50e24dcca9e"; // Receive/Write characteristic

    // Multi-Device Management
    private int targetDeviceCount = 2; // left and right sensors
    private readonly List<string> pendingConnections = new List<string>();
    private readonly Dictionary<string, string> activeConnections = new Dictionary<string, string>(); // Maps MAC -> Name
    private Coroutine ClockUpkeepLoop = null;
    private bool foundRightSensor = false;
    private bool foundLeftSensor = false;
    private bool isSynced = false;
    private string leftSensorName = "GaitSync-Left";
    private string rightSensorName = "GaitSync-Right";
    public bool allConnected = false;

    // --------------------------- Events ---------------------------
    // bool = isRightFoot, long = timestamp
    public static event Action<bool, long> OnStepReceived; 
    
    // string = deviceName, int = batteryLevel
    public static event Action<string, int> OnBatteryLevelReceived;

    public static event Action<string> OnDeviceDisconnected; // string = deviceName

    public static event Action<string> OnDeviceReconnected; // string = deviceName

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

        // Reset trackers
        pendingConnections.Clear();
        foundLeftSensor = false;
        foundRightSensor = false;

        string[] scanFilter = new string[] { nusServiceUUID };

        Debug.Log($"Scanning for strictly 1 ({leftSensorName}) and 1 ({rightSensorName})...");

        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(
            scanFilter, null, (address, name, rssi, bytes) => 
            { 
                // If we found a new device we haven't logged yet
                if (!pendingConnections.Contains(address) && !activeConnections.ContainsKey(address))
                {
                    bool isRight = name.Equals(rightSensorName);
                    bool isLeft = name.Equals(leftSensorName);

                    // Checking if its a sensor pair that we need
                    if (isRight && !foundRightSensor)
                    {
                        Debug.Log($"Found Right Sensor: {name} ({address})");
                        foundRightSensor = true;
                        pendingConnections.Add(address);
                        activeConnections.Add(address, name); // Save name for later
                    }
                    else if (isLeft && !foundLeftSensor)
                    {
                        Debug.Log($"Found Left Sensor: {name} ({address})");
                        foundLeftSensor = true;
                        pendingConnections.Add(address);
                        activeConnections.Add(address, name);
                    }
                    else if (isRight || isLeft)
                    {
                        // It's a valid sensor, but we already have this foot
                        Debug.Log($"Skipping {name} ({address}) - Already found one for this foot.");
                    }

                    // Once we find exactly one left and one right sensor, STOP scanning and begin the queue
                    if (pendingConnections.Count == targetDeviceCount)
                    {
                        BluetoothLEHardwareInterface.StopScan();
                        isScanning = false;
                        ConnectNextDeviceInQueue();
                    }
                }
            }, false, false);
    }

    private void HandleDeviceDrop(string address)
    {
        Debug.LogWarning($"Connection dropped for device with MAC {address}");
        if (!activeConnections.ContainsKey(address)) return;
        if (pendingConnections.Contains(address)) return;
        
        string deviceName = activeConnections[address];
        pendingConnections.Add(address);
        isSynced = false;
        allConnected = false;

        OnDeviceDisconnected?.Invoke(deviceName);
        ConnectNextDeviceInQueue();
    }

    private void ConnectNextDeviceInQueue()
    {
        if (pendingConnections.Count == 0)
        {
            Debug.Log("ALL DEVICES SUCCESSFULLY CONNECTED!");

            // now sync all their clocks together, so we have a common timestamp for features
            SyncDeviceClocks();
            allConnected = true;
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
                SubscribeToDeviceMessages(address, service, characteristic);
            }
        },
        (address) => 
        {
            // Disconnect Callback
            HandleDeviceDrop(address);
        });
    }

    private void SubscribeToDeviceMessages(string address, string service, string characteristic)
    {
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(address, service, characteristic, 
        
            (notifyAddress, notifyCharacteristic) => {
                if (activeConnections.ContainsKey(notifyAddress)) {
                    // prevents ble reconnect callback conflict
                    Debug.Log($"Subscribed to {activeConnections[notifyAddress]}");
                }

                OnDeviceReconnected?.Invoke(activeConnections[notifyAddress]); // reconnect and connect are same UI wise
                ConnectNextDeviceInQueue();
            },
            
            (notifyAddress, notifyCharacteristic, dataBytes) => 
            {
                // empty data check
                if (dataBytes == null || dataBytes.Length == 0) return;

                // We use the fresh MAC address the OS just handed us to find out who really sent this.
                // Prevents ble reconnect callback conflict
                if (!activeConnections.ContainsKey(notifyAddress)) return;
                string trueDeviceName = activeConnections[notifyAddress];

                // The first byte indicates the message type
                int messageType = dataBytes[0]; 

                switch (messageType)
                {
                    case 1: // --- STEP EVENT ---
                        if (dataBytes.Length == 9 && isSynced)
                        {
                            long timestamp = BitConverter.ToInt64(dataBytes, 1);
                            bool isRightFoot = trueDeviceName.Equals(rightSensorName);
                            
                            // Broadcasting data if someone is listening
                            OnStepReceived?.Invoke(isRightFoot, timestamp);
                        }
                        break;

                    case 2: // --- BATTERY EVENT ---
                        if (dataBytes.Length == 2)
                        {
                            int batteryLevel = dataBytes[1];
                            // Debug.Log($"Battery Level from {trueDeviceName}: {batteryLevel}%");
                            OnBatteryLevelReceived?.Invoke(trueDeviceName, batteryLevel);
                        }
                        break;

                    case 4: // --- SYNC ACKNOWLEDGMENT EVENT ---
                        if (dataBytes.Length == 9)
                        {
                            isSynced = true;
                            long confirmedTime = BitConverter.ToInt64(dataBytes, 1);
                            Debug.Log($"------------ SUCCESS: {trueDeviceName} confirmed clock sync at timestamp {confirmedTime} ------------");

                            if (ClockUpkeepLoop == null) 
                            {
                                ClockUpkeepLoop = StartCoroutine(ClockUpkeep());
                            }
                        }
                    break;
                }
            });
    }

    private void SyncDeviceClocks()
    {
        byte[] payload = new byte[1] { 3 }; // Type 3: Hardware Sync Trigger

        foreach (var mac in activeConnections.Keys)
        {
            string deviceMac = mac; 
            string deviceName = activeConnections[mac];
            Debug.Log($"Sending clock sync trigger to {deviceName} ({deviceMac})");

            BluetoothLEHardwareInterface.WriteCharacteristic(
                deviceMac, 
                nusServiceUUID, 
                nusRxCharacteristicUUID, 
                payload, 
                payload.Length, 
                false, 
                (returnedId) => { 
                    // can't use this, as we have 2 identical devices, BLE gets confused
                }
            );
        }
    }

    private IEnumerator ClockUpkeep()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); 

            if (pendingConnections.Count == 0)
            {
                Debug.Log("Upkeep: Performing periodic clock resync...");
                SyncDeviceClocks();
            }
            else
            {
                Debug.LogWarning("Upkeep skipped: One or more sensors are offline.");
            }
        }
    }
}