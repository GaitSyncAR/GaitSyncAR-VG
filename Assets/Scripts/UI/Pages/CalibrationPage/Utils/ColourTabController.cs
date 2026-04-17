using UnityEngine;
using UnityEngine.UIElements;

public class ColourTabController
{
    private VisualElement _container;
    private MetronomeController _metronome;

    public void Initialize(CalibrationPageController parent, VisualElement container, MetronomeController metronome)
    {
        _container = container;
        _metronome = metronome;

        var rSlider = _container.Q("Red_Slider").Q<SliderInt>();
        var gSlider = _container.Q("Green_Slider").Q<SliderInt>();
        var bSlider = _container.Q("Blue_Slider").Q<SliderInt>();
        var rLabel = _container.Q("RedVal").Q<Label>();
        var gLabel = _container.Q("GreenVal").Q<Label>();
        var bLabel = _container.Q("BlueVal").Q<Label>();

        Color c = metronome.targetMaterial.color;
        rSlider.value = Mathf.RoundToInt(c.r * 255f);
        gSlider.value = Mathf.RoundToInt(c.g * 255f);
        bSlider.value = Mathf.RoundToInt(c.b * 255f);

        void ApplyColour()
        {
            // updating slider labels
            if (rLabel != null) rLabel.text = "Red: " + rSlider.value.ToString();
            if (gLabel != null) gLabel.text = "Green: " + gSlider.value.ToString();
            if (bLabel != null) bLabel.text = "Blue: " + bSlider.value.ToString();

            // changing colour
            var nc = new Color(rSlider.value / 255f, gSlider.value / 255f, bSlider.value / 255f);
            metronome.SetColor(nc);
            UIEventBus.EmitColor(nc);
            ProfileManager.Instance.currentProfile.metronomeColour = nc;
        }

        rSlider.RegisterValueChangedCallback(_ => ApplyColour());
        gSlider.RegisterValueChangedCallback(_ => ApplyColour());
        bSlider.RegisterValueChangedCallback(_ => ApplyColour());

        // first time updating ui
        ApplyColour();
    }
}
