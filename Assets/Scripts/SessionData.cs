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
    public List<StepRecord> leftHistory;
    public List<StepRecord> rightHistory;
    
    // Constructor
    public SessionData(
        string sessionDate, 
        float temporalSymmetryRatio, 
        float cadence, 
        float strideTimeVariability, 
        float temporalPhaseOffset, 
        float targetCadence, 
        List<StepRecord> leftHistory, 
        List<StepRecord> rightHistory
        )
    {
        this.sessionDate = sessionDate;
        this.temporalSymmetryRatio = temporalSymmetryRatio;
        this.cadence = cadence;
        this.strideTimeVariability = strideTimeVariability;
        this.temporalPhaseOffset = temporalPhaseOffset;
        this.targetCadence = targetCadence;
        this.leftHistory = leftHistory;
        this.rightHistory = rightHistory;
    }
}