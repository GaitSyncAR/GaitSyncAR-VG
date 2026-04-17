using UnityEngine;
using UnityEngine.UIElements;

public class CalibrationPageController : PageController
{
    // --- Exposing to tab controllers ---
    public MetronomeController Metronome { get; set; }
    public float MovementStep { get; set; } = 0.5f;
    public float ScaleStep    { get; set; } = 0.1f;

    private VisualElement _positionControls;
    private VisualElement _shapeControls;
    private VisualElement _colourControls;

    // public tab controllers
    public PositionTabController _positionTabController;
    public ShapeTabController _shapeTabController;
    public ColourTabController _colourTabController;

    // --- Initialising ---
    public CalibrationPageController(
        UIDocument doc,
        VisualElement pageRoot,
        MetronomeController metronomeController,
        float movementStep,
        float scaleStep) : base(doc, pageRoot)
    {
        Metronome    = metronomeController;
        MovementStep = movementStep;
        ScaleStep    = scaleStep;

        _positionControls = Q<VisualElement>("PositionControls");
        _shapeControls    = Q<VisualElement>("ShapeControls");
        _colourControls   = Q<VisualElement>("ColourControls");

        Q<Button>("PosBtn").clicked   += () => ShowTab("pos");
        Q<Button>("ShapeBtn").clicked += () => ShowTab("shape");
        Q<Button>("ColourBtn").clicked+= () => ShowTab("col");

        // initialising tab controllers
        _positionTabController = new PositionTabController(this, _positionControls, metronomeController);
        _shapeTabController = new ShapeTabController(this, _shapeControls, metronomeController);
        _colourTabController = new ColourTabController(this, _colourControls, metronomeController);

        ShowTab("pos");
    }

    private void ShowTab(string tab)
    {
        _positionControls.style.display = tab == "pos"   ? DisplayStyle.Flex : DisplayStyle.None;
        _shapeControls.style.display    = tab == "shape" ? DisplayStyle.Flex : DisplayStyle.None;
        _colourControls.style.display   = tab == "col"   ? DisplayStyle.Flex : DisplayStyle.None;
        PlayHaptic();
    }

    public override void OnPageHide()
    {
        var profile = ProfileManager.Instance.currentProfile;

        // Pulling modified values from the Controller and overwriting the profile's data
        if (Metronome != null)
        {
            profile.metronomePosition = Metronome.transform.position;
            profile.metronomeSize     = Metronome.transform.localScale;

            if (Metronome.metronomeBar != null)
            {
                profile.metronomeBarScaleY = Metronome.metronomeBar.localScale.y;
            }

            if (Metronome.targetMaterial != null)
            {
                profile.metronomeColour = Metronome.targetMaterial.GetColor("_BaseColor"); 
            }
        }

        ProfileManager.Instance.SaveProfile();
        PlayerPrefs.SetString("CurrentProfile", profile.profileName);
        PlayerPrefs.Save();
        
        UIEventBus.EmitProfileListChanged();
        
        Debug.Log("[Calibration] Saved Position, Shape, and Colour to profile: " + profile.profileName);
    }
}