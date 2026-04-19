using System.Collections.Generic;

public class BatteryWarningTracker
{
    private readonly int _lowBatteryThreshold;
    private readonly HashSet<string> _warnedDevices;

    public BatteryWarningTracker(int threshold = 20)
    {
        _lowBatteryThreshold = threshold;
        _warnedDevices = new HashSet<string>();
    }

    public void ProcessBatteryLevel(string deviceName, int batteryLevel)
    {
        if (batteryLevel <= _lowBatteryThreshold)
        {
            if (!_warnedDevices.Contains(deviceName))
            {
                TriggerLowBatteryPopup(deviceName, batteryLevel);
                _warnedDevices.Add(deviceName);
            }
        }
        else
        {
            // Reset the warning flag if it charges back up
            if (_warnedDevices.Contains(deviceName))
            {
                _warnedDevices.Remove(deviceName);
            }
        }
    }

    private void TriggerLowBatteryPopup(string deviceName, int batteryLevel)
    {
        string deviceSide = deviceName.Equals(BLEManager.Instance.rightSensorName) ? "Right" : "Left";
        PopupManager.Instance.ShowPopup(
            titleText: $"{deviceSide} sensor battery is low at {batteryLevel}%. Please recharge soon.", 
            actionText: "Ok", 
            includeInputField: false, 
            onAction: awnser =>{});
    }
}