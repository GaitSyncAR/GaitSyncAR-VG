using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Globalization;

public class SessionPageController : PageController
{
    // class fields
    private readonly float STRIDE_VARIABILITY_STABLE_TRESHOLD = 0.33f; // 33% of max variability considered stable
    private readonly float MAX_STRIDE_VARIABILITY = 30f;
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
        UpdateTitle(_sessionData);
    }

    private void showTemporalSymmetry(float symmetry = 0f)
    {
        var bar = Q<VisualElement>("TemporalSymmetryFill");
        var resultLabel = Q<Label>("TemporalSymmetryResult");


        float clamped = Mathf.Clamp(symmetry, 0.7f, 1.3f);
        float normalized = Mathf.InverseLerp(0.7f, 1.3f, clamped);
        bar.style.width = Length.Percent(normalized * 100f);
        resultLabel.text = $"{symmetry:F2}";
        Color parsedColor;

        if (symmetry >= 0.95f && symmetry <= 1.05f)
        {
            // (balanced gait)
            ColorUtility.TryParseHtmlString("#4CAF50", out parsedColor);
        }
        else
        {
            // BAD (asymmetry)
            ColorUtility.TryParseHtmlString("#F44336", out parsedColor);
        }

        bar.style.backgroundColor = parsedColor;
    }

    private void showStrideVariability(float variability = 0f)
    {
        var bar = Q<VisualElement>("StrideVariabilityFill");
        var resultLabel = Q<Label>("StrideVariabilityResult");
        var statusLabel = Q<Label>("StrideVariabilityStatus");

        bar.style.width = Length.Percent(Math.Min(variability / MAX_STRIDE_VARIABILITY, 1f) * 100);
        resultLabel.text = $"{variability:F2}";
    
        float passedThreshold = STRIDE_VARIABILITY_STABLE_TRESHOLD * MAX_STRIDE_VARIABILITY;
        Color parsedColor;

        statusLabel.text = variability <= passedThreshold ? "Stable" : "Unstable";
        if (variability <= passedThreshold) {
            ColorUtility.TryParseHtmlString("#4CAF50", out parsedColor);
        } else {
            ColorUtility.TryParseHtmlString("#F44336", out parsedColor);
        }
        statusLabel.style.color = parsedColor;
        bar.style.backgroundColor = parsedColor;
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

        // bar will start from the center point (0.5) and grow left or right based on offset direction
        // has 0.5% padding from center
        float centerPercent = 50f;
        float offsetPercent = offset * 100f;
        float paddingPercent = 0.45f;
        if (offsetPercent <= 50)
        {
            float paddedOffset = offsetPercent + paddingPercent;
            bar.style.width = Length.Percent(centerPercent - paddedOffset);
            bar.style.left = Length.Percent(paddedOffset - paddingPercent);
        }
        else
        {
            float barWidthPercent = offsetPercent - centerPercent;
            bar.style.left = Length.Percent(centerPercent + paddingPercent);
            bar.style.width = Length.Percent(barWidthPercent - paddingPercent);
        }

        
        ResultLabel.text = $"{offset:F2}";
    }
    private float CalculateStabilityScore(SessionData sessionData)
    {
        // 1. Symmetry Penalty (Max 3 points lost)
        // Range is 0.7 to 1.3. Ideal is 1.0.
        float symmetryDeviation = Math.Abs(1.0f - sessionData.temporalSymmetryRatio);
        float symmetryPenalty = 3f * Math.Min(1f, symmetryDeviation / 0.15f);

        // 2. Variability Penalty (Max 3 points lost)
        // Tied to UI's MAX_STRIDE_VARIABILITY (30). 
        // They lose points progressively up to 30.
        float variabilityPenalty = 3f * Math.Min(1f, sessionData.strideTimeVariability / MAX_STRIDE_VARIABILITY);

        // 3. Cadence Penalty (Max 2 points lost)
        // Tied to UI's BPM_TOLERANCE. 
        // If they are off by more than double the green zone tolerance (10 BPM), they lose both points.
        float cadencePenalty = 0f;
        if (sessionData.targetCadence > 0) 
        {
            float cadenceDeviation = Math.Abs(sessionData.cadence - sessionData.targetCadence);
            float maxAllowedDeviation = BPM_TOLERANCE * 2f; 
            cadencePenalty = 2f * Math.Min(1f, cadenceDeviation / maxAllowedDeviation);
        }

        // 4. Phase Offset Penalty (Max 2 points lost)
        float idealPhase = 0.5f;
        float phaseDeviation = Math.Abs(idealPhase - sessionData.temporalPhaseOffset);
        // 15% (0.15) deviation from ideal gives full penalty
        float phasePenalty = 2f * Math.Min(1f, phaseDeviation / 0.15f); 

        // Calculate Total
        float totalPenalty = symmetryPenalty + variabilityPenalty + cadencePenalty + phasePenalty;
        float rawScore = 10f - totalPenalty;

        // Clamp between 0 and 10 and optionally round to 1 decimal place for UI aesthetics
        return (float)Math.Round(Math.Clamp(rawScore, 0f, 10f), 1);
    }

    private void UpdateTitle(SessionData sessionData)
    {
        var titleLabel = Q<Label>("SessionTitle");
        float stabilityScore = CalculateStabilityScore(sessionData);
        string formattedScore = stabilityScore.ToString("F1");

        string formattedDate = DateTime.TryParseExact(
            sessionData.sessionDate.Trim(),
            "yyyy-MM-dd_HH-mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate)
        ? parsedDate.ToString("dd/MM/yyyy • HH:mm")
        : sessionData.sessionDate;

        titleLabel.text = $"Session Report {formattedDate} | Stability: {formattedScore}/10";
    }
}
