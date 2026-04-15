using UnityEngine;
using System.Collections.Generic;

public class GaitMetrics : MonoBehaviour
{
    // A simple container for our historical data
    [System.Serializable]
    public struct StepRecord
    {
        public bool isRightFoot;
        public long timestamp;

        public StepRecord(bool right, long time)
        {
            isRightFoot = right;
            timestamp = time;
        }
    }

    // --- The Master Data Lists ---
    // Session Data
    public List<StepRecord> LeftStepHistory = new List<StepRecord>();
    public List<StepRecord> RightStepHistory = new List<StepRecord>();

    private void OnEnable()
    {
        BLEManager.OnStepReceived += HandleStepEvent;
    }

    private void OnDisable()
    {
        BLEManager.OnStepReceived -= HandleStepEvent;
    }

    private void HandleStepEvent(bool isRightFoot, long timestamp)
    {
        if (isRightFoot)
        {
            // Create right record and add to list
            StepRecord newStep = new StepRecord(true, timestamp);
            RightStepHistory.Add(newStep);
            Debug.Log($"[LOGGED] Right Step #{RightStepHistory.Count} at {timestamp}ms");
        }
        else
        {
            // Create left record and add to list
            StepRecord newStep = new StepRecord(false, timestamp);
            LeftStepHistory.Add(newStep);
            Debug.Log($"[LOGGED] Left Step #{LeftStepHistory.Count} at {timestamp}ms");
        }
    }
}