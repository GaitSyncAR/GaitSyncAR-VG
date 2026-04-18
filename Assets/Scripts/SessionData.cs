using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SessionData
{
    public string sessionDate;
    public float temporalSymmetryRatio;
    public float cadence;
    public float strideTimeVariability;
    public float temporalPhaseOffset;
    public float targetCadence;
    
    // Raw Data Logs
    public List<StepRecord> leftStepHistory;
    public List<StepRecord> rightStepHistory;

    public static readonly SessionData MockDataStable = new SessionData (
        System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        0.85f,
        44,
        3.46f,
        0.0f,
        60,
        new List<StepRecord>
        {
            new StepRecord(false, 1020000),
            new StepRecord(false, 1835000),
            new StepRecord(false, 2670000),
            new StepRecord(false, 3490000),
            new StepRecord(false, 4355000), // slight delay (irregularity)
            new StepRecord(false, 5140000),
            new StepRecord(false, 5985000),
            new StepRecord(false, 6790000)
        },

        // RIGHT steps (true)
        new List<StepRecord>
        {
            new StepRecord(true, 600000),
            new StepRecord(true, 1420000),
            new StepRecord(true, 2255000),
            new StepRecord(true, 3070000),
            new StepRecord(true, 3890000),
            new StepRecord(true, 4700000),
            new StepRecord(true, 5535000),
            new StepRecord(true, 6350000)
        }
    );

    public static readonly SessionData UnstableMockData = new SessionData(
        System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        0.74f,   // clearly asymmetric
        38f,     // lower cadence (slower, irregular walking)
        12.8f,   // high variability
        0.11f,   // noticeable phase offset
        60f,

        // LEFT steps (false), slightly delayed + inconsistent
        new List<StepRecord>
        {
            new StepRecord(false, 1100000),
            new StepRecord(false, 2100000),
            new StepRecord(false, 3250000), // delay spike
            new StepRecord(false, 4300000),
            new StepRecord(false, 5600000), // long gap (hesitation)
            new StepRecord(false, 6900000),
            new StepRecord(false, 8200000),
            new StepRecord(false, 9500000)
        },

        // RIGHT steps (true), more frequent but uneven bursts
        new List<StepRecord>
        {
            new StepRecord(true, 600000),
            new StepRecord(true, 1500000),
            new StepRecord(true, 2350000),
            new StepRecord(true, 2900000),
            new StepRecord(true, 3600000),
            new StepRecord(true, 5100000), // mismatch gap
            new StepRecord(true, 6400000),
            new StepRecord(true, 7700000)
        }
    );
    
    // Constructor
    public SessionData(
        string sessionDate, 
        float temporalSymmetryRatio, 
        float cadence, 
        float strideTimeVariability, 
        float temporalPhaseOffset, 
        float targetCadence, 
        List<StepRecord> leftStepHistory, 
        List<StepRecord> rightStepHistory
        )
    {
        this.sessionDate = sessionDate;
        this.temporalSymmetryRatio = temporalSymmetryRatio;
        this.cadence = cadence;
        this.strideTimeVariability = strideTimeVariability;
        this.temporalPhaseOffset = temporalPhaseOffset;
        this.targetCadence = targetCadence;
        this.leftStepHistory = leftStepHistory;
        this.rightStepHistory = rightStepHistory;
    }
}