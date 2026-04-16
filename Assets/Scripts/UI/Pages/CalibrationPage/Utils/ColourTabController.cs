using UnityEngine;
using UnityEngine.UIElements;

public class ColourTabController
{
    private VisualElement _container;
    private Material      _mat;

    public void Initialize(CalibrationPageController parent, VisualElement container, Material mat)
    {
        _container = container;
        _mat = mat;

        var rSlider = _container.Q("Red_Slider").Q<SliderInt>();
        var gSlider = _container.Q("Green_Slider").Q<SliderInt>();
        var bSlider = _container.Q("Blue_Slider").Q<SliderInt>();

        Color c = _mat.color;
        rSlider.value = Mathf.RoundToInt(c.r * 255f);
        gSlider.value = Mathf.RoundToInt(c.g * 255f);
        bSlider.value = Mathf.RoundToInt(c.b * 255f);

        void ApplyColour()
        {
            var nc = new Color(rSlider.value / 255f, gSlider.value / 255f, bSlider.value / 255f);
            _mat.SetColor("_BaseColor",    nc);
            _mat.SetColor("_SpecColor",    nc);
            _mat.SetColor("_EmissionColor",nc);
            ProfileManager.Instance.currentProfile.metronomeColour = nc;
            UIEventBus.EmitColor(nc);
        }

        rSlider.RegisterValueChangedCallback(_ => ApplyColour());
        gSlider.RegisterValueChangedCallback(_ => ApplyColour());
        bSlider.RegisterValueChangedCallback(_ => ApplyColour());
    }
}
