using UnityEngine;
using UnityEngine.UIElements;

public class CalibrationPageController : PageController
{
    // ── Expose these to tab controllers ──
    public Transform MetronomeObject  { get; set; }
    public Transform MetronomeBar     { get; set; }
    public Transform MetronomeArmVisuals { get; set; }
    public float     MovementStep    { get; set; } = 0.5f;
    public float     ScaleStep        { get; set; } = 0.1f;

    private VisualElement _positionControls;
    private VisualElement _shapeControls;
    private VisualElement _colourControls;
    
    private Transform _metronomeObject;
    private Material  _targetMaterial;

    // ── Initialise with extra refs (called from UIManager) ──
    public void InitWithRefs(
        UIDocument doc,
        Transform  metronomeObject,
        Transform  metronomeBar,
        Transform  metronomeArmVisuals,
        float      movementStep,
        float      scaleStep,
        Material   targetMaterial)
    {
        base.Initialize(doc);

        MetronomeObject   = metronomeObject;
        MetronomeBar      = metronomeBar;
        MetronomeArmVisuals = metronomeArmVisuals;
        MovementStep      = movementStep;
        ScaleStep         = scaleStep;
        _targetMaterial   = targetMaterial;

        _positionControls = Q<VisualElement>("PositionControls");
        _shapeControls    = Q<VisualElement>("ShapeControls");
        _colourControls   = Q<VisualElement>("ColourControls");

        Q<Button>("PosBtn").clicked   += () => ShowTab("pos");
        Q<Button>("ShapeBtn").clicked += () => ShowTab("shape");
        Q<Button>("ColourBtn").clicked+= () => ShowTab("col");

        new PositionTabController().Initialize(this, _positionControls);
        new ShapeTabController().Initialize(this, _shapeControls);
        new ColourTabController().Initialize(this, _colourControls, targetMaterial);

        ShowTab("pos");
    }

    private void ShowTab(string tab)
    {
        _positionControls.style.display = tab == "pos"   ? DisplayStyle.Flex : DisplayStyle.None;
        _shapeControls.style.display    = tab == "shape"  ? DisplayStyle.Flex : DisplayStyle.None;
        _colourControls.style.display   = tab == "col"    ? DisplayStyle.Flex : DisplayStyle.None;
        PlayHaptic();
    }

    // ══════════════════════════════════════════════════════════
    //  Methods called by PositionTabController & ShapeTabController
    // ══════════════════════════════════════════════════════════

    public void Move(Vector3 delta)
    {
        if (MetronomeObject != null)
        {
            MetronomeObject.position += delta;
            ProfileManager.Instance.currentProfile.metronomePosition = MetronomeObject.position;
        }
        PlayHaptic();
    }

    public void ScaleUniform(float amount)
    {
        if (MetronomeObject == null) return;

        Vector3 newScale = MetronomeObject.localScale + Vector3.one * amount;
        newScale = ClampScale(newScale);
        MetronomeObject.localScale = newScale;
        ProfileManager.Instance.currentProfile.metronomeSize = newScale;

        PlayHaptic();
    }

    public void ScaleStretch(float amount)
    {
        if (MetronomeBar != null)
        {
            Vector3 barScale = MetronomeBar.localScale;
            barScale.y += amount;
            barScale.y  = Mathf.Max(0.1f, barScale.y);
            MetronomeBar.localScale = barScale;
        }

        if (MetronomeArmVisuals != null)
        {
            float scaled = amount / 25f;
            Vector3 armScale = MetronomeArmVisuals.localScale;
            armScale.z = Mathf.Max(0.1f, armScale.z + scaled);
            MetronomeArmVisuals.localScale = armScale;

            Vector3 armPos = MetronomeArmVisuals.localPosition;
            armPos.y -= scaled * 0.5f;
            MetronomeArmVisuals.localPosition = armPos;
        }

        PlayHaptic();
    }

    private static Vector3 ClampScale(Vector3 s)
    {
        if (s.x < 0.1f) s = Vector3.one * 0.1f;
        return s;
    }

    public override void OnPageHide()
    {
        // Grab the currently active profile
        var profile = ProfileManager.Instance.currentProfile;

        // Pull the live, modified values from the 3D scene and overwrite the profile's data
        if (_metronomeObject != null)
        {
            profile.metronomePosition = _metronomeObject.position;
            profile.metronomeSize = _metronomeObject.localScale;
        }

        if (MetronomeBar != null)
        {
            profile.metronomeBarScaleY = MetronomeBar.localScale.y;
        }

        if (_targetMaterial != null)
        {
            // Make sure this matches the exact shader property name your material uses
            profile.metronomeColour = _targetMaterial.GetColor("_BaseColor"); 
        }

        // Command the ProfileManager to write this updated data to your JSON/Save file
        ProfileManager.Instance.SaveProfile();
        
        // Update PlayerPrefs to ensure it remembers this was the last used profile
        PlayerPrefs.SetString("CurrentProfile", profile.profileName);
        PlayerPrefs.Save();
        ProfileManager.Instance.SaveProfile();

        // Tell the UI to refresh in case we overwrote a template
        UIEventBus.EmitProfileListChanged();
        
        Debug.Log("[Calibration] Saved Position, Shape, and Colour to profile: " + profile.profileName);
    }
}
