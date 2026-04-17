using UnityEngine;
using UnityEngine.UIElements;

public class ColourTabController
{
    private VisualElement _container;
    private MetronomeController _metronome;
    private CalibrationPageController _parent;

    // sliders
    private SliderInt rSlider;
    private SliderInt gSlider;
    private SliderInt bSlider;
    // labels
    private Label rLabel;
    private Label gLabel;
    private Label bLabel;

    public ColourTabController(CalibrationPageController parent, VisualElement container, MetronomeController metronome)
    {
        _container = container;
        _metronome = metronome;
        _parent = parent;

        rSlider = _container.Q("Red_Slider").Q<SliderInt>();
        gSlider = _container.Q("Green_Slider").Q<SliderInt>();
        bSlider = _container.Q("Blue_Slider").Q<SliderInt>();
        rLabel = _container.Q("RedVal").Q<Label>();
        gLabel = _container.Q("GreenVal").Q<Label>();
        bLabel = _container.Q("BlueVal").Q<Label>();

        rSlider.RegisterValueChangedCallback(_ => ApplyColour());
        gSlider.RegisterValueChangedCallback(_ => ApplyColour());
        bSlider.RegisterValueChangedCallback(_ => ApplyColour());

        UIEventBus.ProfileApplied += _ => onDataReload();
    }

    public void onDataReload()
    {
        Color c = ProfileManager.Instance.currentProfile.metronomeColour;
        rSlider.value = (int)(c.r * 255);
        gSlider.value = (int)(c.g * 255);
        bSlider.value = (int)(c.b * 255);
        ApplyColour();
    }

    public void ApplyColour()
    {
        // updating slider labels
        if (rLabel != null) rLabel.text = "Red: " + rSlider.value.ToString();
        if (gLabel != null) gLabel.text = "Green: " + gSlider.value.ToString();
        if (bLabel != null) bLabel.text = "Blue: " + bSlider.value.ToString();

        // changing colour
        var nc = new Color(rSlider.value / 255f, gSlider.value / 255f, bSlider.value / 255f);
        UIEventBus.EmitColor(nc);
        
        ProfileManager.Instance.currentProfile.metronomeColour = nc;
    }
}
