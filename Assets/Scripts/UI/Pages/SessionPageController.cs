using UnityEngine;
using UnityEngine.UIElements;
using System;

public class SessionPageController : PageController
{
    // class fields
    private readonly float TEMPORAL_SYMMETRY_TARGET = 1f;
    private readonly float STRIDE_VARIABILITY_STABLE_TRESHOLD = 3f;
    private readonly float BPM_TOLERANCE = 5f; // BPM tolerance for optimal cadence
    private SessionData _sessionData;

    // constructor
    public SessionPageController(UIDocument uiDocument, VisualElement pageRoot) : base(uiDocument, pageRoot)
    {
    }

    public void SetSessionData(SessionData data)
    {
        _sessionData = data;
        showTemporalSymmetry(data.temporalSymmetryRatio);
        showStrideVariability(data.strideTimeVariability);
        showCadence(data.cadence, data.targetCadence);
        showtemporalPhaseOffset(data.temporalPhaseOffset);
        updateTitle(_sessionData);
    }

    private void showTemporalSymmetry(float symmetry = 0f)
    {
        var bar = Q<VisualElement>("TemporalSymmetryFill");
        var ResultLabel = Q<Label>("TemporalSymmetryResult");
    }

    private void showStrideVariability(float variability = 0f)
    {
        var bar = Q<VisualElement>("StrideVariabilityFill");
        var resultLabel = Q<Label>("StrideVariabilityResult");
        var statusLabel = Q<Label>("StrideVariabilityStatus");
    }

    private void showCadence(float cadence = 0f, float targetCadence = 0f)
    {
        var bar = Q<VisualElement>("CadenceFill");
        var optimalPoint = Q<VisualElement>("CadenceOptimal");
        var resultLabel =  Q<Label>("CadenceResult");
        var targetLabel =  Q<Label>("CadenceTarget");

        float maxCadence = targetCadence * 2;
        bar.style.width = Length.Percent((Math.Min(cadence, maxCadence) / maxCadence) * 100);

        float totalToleranceUnits = BPM_TOLERANCE * 2f;
        float optimalWidthPercent = (totalToleranceUnits / maxCadence) * 100f;
        optimalPoint.style.width = Length.Percent(optimalWidthPercent);
        optimalPoint.style.left = Length.Percent((100 - optimalWidthPercent) / 2f);

        resultLabel.text = $"{Math.Round(cadence)}";
        targetLabel.text = $"Target: {Math.Round(targetCadence)}";
    }

    private void showtemporalPhaseOffset(float offset = 0f)
    {
        var bar = Q<VisualElement>("PhaseOffsetFill");
        var ResultLabel = Q<Label>("PhaseOffsetResult");
    }

    private void updateTitle(SessionData sessionData)
    {
        var titleLabel = Q<Label>("SessionTitle");
        int stabilityScore = Mathf.RoundToInt(sessionData.temporalSymmetryRatio * 10);
        titleLabel.text = $"Session Report - {sessionData.sessionDate} | Stability: {stabilityScore}/10";
    }
}
