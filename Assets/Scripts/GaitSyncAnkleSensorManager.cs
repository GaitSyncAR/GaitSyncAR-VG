using UnityEngine;
using System;

public class GaitSyncAnkleSensorManager : MonoBehaviour
{
    private string targetLeftSensorName = "";
    private string targetRightSensorName = "";

    void Start()
    {
        // 1. Check if the app was launched via NFC Tap
        string deepLinkURL = Application.absoluteURL;

        if (!string.IsNullOrEmpty(deepLinkURL) && deepLinkURL.Contains("gaitsync://connect"))
        {
            HandleNFCTap(deepLinkURL);
        }
        else
        {
            // 2. Normal app launch (User just tapped the Unity App Icon)
            HandleNormalLaunch();
        }
    }

    private void HandleNFCTap(string url)
    {
        Debug.Log("App launched via NFC Tap: " + url);

        // Parse the URL to see which sensor was tapped
        if (url.Contains("left"))
        {
            targetLeftSensorName = "GaitSync-Left";
            PlayerPrefs.SetString("SavedLeftSensor", targetLeftSensorName);
            PlayerPrefs.Save();
            
            Debug.Log("Left Sensor saved. Initiating BLE Scan...");
            StartBLEScan(targetLeftSensorName);
        }
        else if (url.Contains("right"))
        {
            targetRightSensorName = "GaitSync-Right";
            PlayerPrefs.SetString("SavedRightSensor", targetRightSensorName);
            PlayerPrefs.Save();
            
            Debug.Log("Right Sensor saved. Initiating BLE Scan...");
            StartBLEScan(targetRightSensorName);
        }
    }

    private void HandleNormalLaunch()
    {
        // Check our memory for saved sensors
        targetLeftSensorName = PlayerPrefs.GetString("SavedLeftSensor", "");
        targetRightSensorName = PlayerPrefs.GetString("SavedRightSensor", "");

        if (targetLeftSensorName != "" || targetRightSensorName != "")
        {
            Debug.Log("Saved sensors found! Starting background auto-reconnect...");
            // Pass whatever names we found to the BLE scanner
            StartBLEScan(targetLeftSensorName, targetRightSensorName); 
        }
        else
        {
            Debug.Log("No saved devices. Waiting for NFC Tap...");
            // Do nothing. Show the "Tap your shoe to the phone" UI prompt.
        }
    }

    public void ForgetDevices()
    {
        // Tied to a UI button in your Unity Settings menu
        PlayerPrefs.DeleteKey("SavedLeftSensor");
        PlayerPrefs.DeleteKey("SavedRightSensor");
        PlayerPrefs.Save();
        
        DisconnectBLE();
        Debug.Log("Devices Forgotten. App will not auto-connect next time.");
    }

    private void StartBLEScan(params string[] targetNames)
    {
    }
}