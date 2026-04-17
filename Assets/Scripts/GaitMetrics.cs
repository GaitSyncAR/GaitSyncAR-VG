using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GaitMetrics : MonoBehaviour
{
    // --- Data Storage ---
    public List<StepRecord> LeftStepHistory = new List<StepRecord>();
    public List<StepRecord> RightStepHistory = new List<StepRecord>();

    // These hold the calculated Deltas in milliseconds for valid steps
    private List<float> validLeftDurationsMs = new List<float>();
    private List<float> validRightDurationsMs = new List<float>();

    // Tracking for stitched delta calculation
    private long lastLeftTimestampUs = 0;
    private long lastRightTimestampUs = 0;
    
    // Tracking for hardware continuity
    private long leftSessionOffsetUs = 0;
    private long rightSessionOffsetUs = 0;
    private long lastRawLeftUs = 0;
    private long lastRawRightUs = 0;

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
        long previous = isRightFoot ? lastRightTimestampUs : lastLeftTimestampUs;

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
        if (isRightFoot) lastRightTimestampUs = continuousTime;
        else lastLeftTimestampUs = continuousTime;
    }

    public void SaveSession()
    {
        SessionData data = new SessionData(
            System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm"),
            GetTemporalSymmetryRatio(),
            GetCadence(),
            GetStrideTimeVariability(),
            GetTemporalPhaseOffset(),
            ProfileManager.Instance.currentProfile.bpm,
            LeftStepHistory,
            RightStepHistory
        );

        string json = JsonUtility.ToJson(data, true);
        string filePath = Path.Combine(Application.persistentDataPath, $"Session_{data.sessionDate}.json");
        
        File.WriteAllText(filePath, json);
        Debug.Log($"Session Saved to: {filePath}");
    }

    private long GetContinuousTimestamp(bool isRightFoot, long rawHardwareTimeUs)
    {
        long lastRaw = isRightFoot ? lastRawRightUs : lastRawLeftUs;
        long currentOffset = isRightFoot ? rightSessionOffsetUs : leftSessionOffsetUs;

        // Get current absolute phone time in microseconds
        long currentPhoneTimeUs = System.DateTime.UtcNow.Ticks / 10;

        // DETECT INITIAL BOOT OR REBOOT
        if (lastRaw == 0 || rawHardwareTimeUs < lastRaw)
        {
            // Calculate the exact offset needed to map the sensor's 0-time to the phone's current time.
            long newOffset = currentPhoneTimeUs - rawHardwareTimeUs;

            if (isRightFoot) rightSessionOffsetUs = newOffset;
            else leftSessionOffsetUs = newOffset;

            if (lastRaw != 0) 
            {
                Debug.LogWarning($"[REBOOT DETECTED] {(isRightFoot ? "Right" : "Left")} sensor power-cycled. Resynced to phone time.");
            }
        }

        // Update the 'raw' tracker
        if (isRightFoot) lastRawRightUs = rawHardwareTimeUs;
        else lastRawLeftUs = rawHardwareTimeUs;

        // True Time = Sensor Time + (Phone Time - Sensor Time)
        return rawHardwareTimeUs + (isRightFoot ? rightSessionOffsetUs : leftSessionOffsetUs);
    }

    public void StartNewSession()
    {
        leftSessionOffsetUs = 0;
        rightSessionOffsetUs = 0;
        lastRawLeftUs = 0;
        lastRawRightUs = 0;
        lastLeftTimestampUs = 0;
        lastRightTimestampUs = 0;
        
        LeftStepHistory.Clear();
        RightStepHistory.Clear();
        validLeftDurationsMs.Clear();
        validRightDurationsMs.Clear();
    }

   private float GetMedian(List<float> list)
    {
        if (list.Count == 0) return 0f;
        var sorted = list.OrderBy(x => x).ToList();
        int midIndex = sorted.Count / 2;
        
        // If even number of elements, average the two middle ones
        if (sorted.Count % 2 == 0) 
        {
            return (sorted[midIndex - 1] + sorted[midIndex]) / 2f;
        }
        
        // If odd, just return the middle one
        return sorted[midIndex];
    }

    private float GetStandardDeviation(List<float> values)
    {
        if (values.Count < 2) return 0f;
        
        float avg = values.Average();
        float sumOfSquares = values.Sum(v => Mathf.Pow(v - avg, 2));
        
        // Using N-1 for sample standard deviation
        return Mathf.Sqrt(sumOfSquares / (values.Count - 1));
    }

    public float GetTemporalSymmetryRatio()
    {
        if (validLeftDurationsMs.Count < 3 || validRightDurationsMs.Count < 3)
        {
            Debug.LogError("Not enough data to calculate Temporal Symmetry Ratio!");
            return 1.0f;
        }

        // Use Medians to ignore any "weird but plausible" stumbling steps that could skew the average
        float medianL = GetMedian(validLeftDurationsMs);
        float medianR = GetMedian(validRightDurationsMs);
        if (medianR == 0 || medianL == 0) return 1.0f;
        return medianL / medianR;
    }

    public float GetCadence()
    {
        if (validLeftDurationsMs.Count == 0 && validRightDurationsMs.Count == 0) return 0f;

        // Get average stride duration in milliseconds
        float avgLeftStrideMs = validLeftDurationsMs.Count > 0 ? validLeftDurationsMs.Average() : 0f;
        float avgRightStrideMs = validRightDurationsMs.Count > 0 ? validRightDurationsMs.Average() : 0f;

        // Combine them for the overall average stride
        float overallAvgStrideMs = (avgLeftStrideMs + avgRightStrideMs) / 2f;
        if (overallAvgStrideMs == 0) return 0f;

        // 1 Stride = 2 Steps. 
        // 60,000ms in a minute. 
        // Cadence = (60000 / StrideTime) * 2
        return (60000f / overallAvgStrideMs) * 2f;
    }

    public float GetStrideTimeVariability()
    {
        if (validLeftDurationsMs.Count < 3 || validRightDurationsMs.Count < 3) return 0f;

        // CV = (Standard Deviation / Mean) * 100
        float leftMean = validLeftDurationsMs.Average();
        float leftStdDev = GetStandardDeviation(validLeftDurationsMs);
        float leftCV = (leftStdDev / leftMean) * 100f;

        float rightMean = validRightDurationsMs.Average();
        float rightStdDev = GetStandardDeviation(validRightDurationsMs);
        float rightCV = (rightStdDev / rightMean) * 100f;

        // Return the average variability of both legs
        return (leftCV + rightCV) / 2f;
    }

    public float GetTemporalPhaseOffset()
    {
        if (LeftStepHistory.Count < 3 || RightStepHistory.Count < 3) return 0.5f;

        List<float> phaseValues = new List<float>();

        // For every Left step, find the Right step immediately before and after it
        foreach (var leftStep in LeftStepHistory)
        {
            long tL = leftStep.timestampUs;

            // Find the Right strike immediately BEFORE this Left strike
            var prevRight = RightStepHistory.LastOrDefault(r => r.timestampUs < tL);
            // Find the Right strike immediately AFTER this Left strike
            var nextRight = RightStepHistory.FirstOrDefault(r => r.timestampUs > tL);

            // If we found a valid bracket
            if (prevRight.timestampUs != 0 && nextRight.timestampUs != 0)
            {
                long tR1 = prevRight.timestampUs;
                long tR2 = nextRight.timestampUs;

                long rightStrideDuration = tR2 - tR1;
                long offsetDuration = tL - tR1;

                // Prevent division by zero and weed out hardware glitches
                if (rightStrideDuration > 0)
                {
                    float phase = (float)offsetDuration / (float)rightStrideDuration;
                    
                    // Only accept plausible phase bounds (e.g., between 10% and 90% of the stride)
                    if (phase > 0.1f && phase < 0.9f)
                    {
                        phaseValues.Add(phase);
                    }
                }
            }
        }

        if (phaseValues.Count == 0) return 0.5f; // Default to perfect if math fails

        // Return the median phase offset (0.5 is perfect symmetry)
        var sortedPhases = phaseValues.OrderBy(p => p).ToList();
        return sortedPhases[sortedPhases.Count / 2];
    }
}