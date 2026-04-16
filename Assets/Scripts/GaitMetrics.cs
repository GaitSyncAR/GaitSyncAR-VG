using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GaitMetrics : MonoBehaviour
{
    [System.Serializable]
    public class SessionSaveData
    {
        public string sessionDate;
        public float finalSymmetry;
        public List<GaitMetrics.StepRecord> leftHistory;
        public List<GaitMetrics.StepRecord> rightHistory;
    }

    [System.Serializable]
    public struct StepRecord
    {
        public bool isRightFoot;
        public long timestamp; // Microseconds from hardware

        public StepRecord(bool right, long time)
        {
            isRightFoot = right;
            timestamp = time;
        }
    }

    // --- Data Storage ---
    public List<StepRecord> LeftStepHistory = new List<StepRecord>();
    public List<StepRecord> RightStepHistory = new List<StepRecord>();

    // These hold the calculated Deltas in milliseconds for valid steps
    private List<float> validLeftDurationsMs = new List<float>();
    private List<float> validRightDurationsMs = new List<float>();

    // Tracking for stitched delta calculation
    private long lastLeftTimestamp = 0;
    private long lastRightTimestamp = 0;
    
    // Tracking for hardware continuity
    private long leftSessionOffset = 0;
    private long rightSessionOffset = 0;
    private long lastRawLeft = 0;
    private long lastRawRight = 0;

    private void OnEnable() => BLEManager.OnStepReceived += HandleStepEvent;
    private void OnDisable() => BLEManager.OnStepReceived -= HandleStepEvent;

    private void HandleStepEvent(bool isRightFoot, long rawTimestamp)
    {
        // Getting cleaned, continuous timeline
        long continuousTime = GetContinuousTimestamp(isRightFoot, rawTimestamp);

        // Logging to history
        StepRecord newStep = new StepRecord(isRightFoot, continuousTime);
        if (isRightFoot) RightStepHistory.Add(newStep);
        else LeftStepHistory.Add(newStep);

        // Plausible Bounds for Analytics
        long previous = isRightFoot ? lastRightTimestamp : lastLeftTimestamp;

        if (previous != 0)
        {
            float deltaMs = (continuousTime - previous) / 1000f;

            if (deltaMs >= 300f && deltaMs <= 1500f)
            {
                if (isRightFoot) validRightDurationsMs.Add(deltaMs);
                else validLeftDurationsMs.Add(deltaMs);
                Debug.Log($"Valid step detected for {(isRightFoot ? "Right" : "Left")} foot, DELTA: {deltaMs} ms at {continuousTime / 1000f} ms");
            }
        }

        // Updating the continuous anchor for the next step's delta
        if (isRightFoot) lastRightTimestamp = continuousTime;
        else lastLeftTimestamp = continuousTime;
    }

    public float GetFinalSymmetry()
    {
        if (validLeftDurationsMs.Count < 3 || validRightDurationsMs.Count < 3)
        {
            Debug.LogError("Not enough data to calculate symmetry!");
            return 1.0f;
        }

        // Use Medians to ignore any "weird but plausible" stumbling steps that could skew the average
        float medianL = GetMedian(validLeftDurationsMs);
        float medianR = GetMedian(validRightDurationsMs);

        return medianL / medianR;
    }

    private float GetMedian(List<float> list)
    {
        var sorted = list.OrderBy(x => x).ToList();
        return sorted[sorted.Count / 2];
    }

    public void SaveSession()
    {
        SessionSaveData data = new SessionSaveData();
        data.sessionDate = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        data.finalSymmetry = GetFinalSymmetry();
        data.leftHistory = LeftStepHistory;
        data.rightHistory = RightStepHistory;

        string json = JsonUtility.ToJson(data, true);
        string filePath = Path.Combine(Application.persistentDataPath, $"Session_{data.sessionDate}.json");
        
        File.WriteAllText(filePath, json);
        Debug.Log($"Session Saved to: {filePath}");
    }

    private long GetContinuousTimestamp(bool isRightFoot, long rawHardwareTimeUs)
    {
        long lastRaw = isRightFoot ? lastRawRight : lastRawLeft;
        long currentOffset = isRightFoot ? rightSessionOffset : leftSessionOffset;

        // Get current absolute phone time in microseconds
        long currentPhoneTimeUs = System.DateTime.UtcNow.Ticks / 10;

        // DETECT INITIAL BOOT OR REBOOT
        if (lastRaw == 0 || rawHardwareTimeUs < lastRaw)
        {
            // Calculate the exact offset needed to map the sensor's 0-time to the phone's current time.
            long newOffset = currentPhoneTimeUs - rawHardwareTimeUs;

            if (isRightFoot) rightSessionOffset = newOffset;
            else leftSessionOffset = newOffset;

            if (lastRaw != 0) 
            {
                Debug.LogWarning($"[REBOOT DETECTED] {(isRightFoot ? "Right" : "Left")} sensor power-cycled. Resynced to phone time.");
            }
        }

        // Update the 'raw' tracker
        if (isRightFoot) lastRawRight = rawHardwareTimeUs;
        else lastRawLeft = rawHardwareTimeUs;

        // True Time = Sensor Time + (Phone Time - Sensor Time)
        return rawHardwareTimeUs + (isRightFoot ? rightSessionOffset : leftSessionOffset);
    }

    public void StartNewSession()
    {
        leftSessionOffset = 0;
        rightSessionOffset = 0;
        lastRawLeft = 0;
        lastRawRight = 0;
        lastLeftTimestamp = 0;
        lastRightTimestamp = 0;
        
        LeftStepHistory.Clear();
        RightStepHistory.Clear();
        validLeftDurationsMs.Clear();
        validRightDurationsMs.Clear();
    }
}