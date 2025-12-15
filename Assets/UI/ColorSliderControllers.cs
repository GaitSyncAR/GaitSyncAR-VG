using UnityEngine;
using UnityEngine.UIElements;

public class ColorSliderController : MonoBehaviour
{
    public UIDocument uiDocument;
    public Material targetMaterial;

    private VisualElement colorPreview;
    private Slider redSlider;
    private Slider greenSlider;
    private Slider blueSlider;

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        redSlider = root.Q<Slider>("RedSlider");
        greenSlider = root.Q<Slider>("GreenSlider");
        blueSlider = root.Q<Slider>("BlueSlider");
        colorPreview = root.Q<VisualElement>("ColourPreview");

        // Register callbacks
        redSlider.RegisterValueChangedCallback(evt => UpdateColor());
        greenSlider.RegisterValueChangedCallback(evt => UpdateColor());
        blueSlider.RegisterValueChangedCallback(evt => UpdateColor());

        // Initialize color
        UpdateColor();
    }

    private void UpdateColor()
    {
        // create color from slider values
        float r = redSlider.value;
        float g = greenSlider.value;
        float b = blueSlider.value;
        Color newColor = new Color(r, g, b, 1f);

        // Set base color (diffuse)
        targetMaterial.SetColor("_BaseColor", newColor);
        targetMaterial.SetColor("_SpecColor", newColor);

        // Set emission (glow)
        targetMaterial.SetColor("_EmissionColor", newColor);

        // set preview color
        colorPreview.style.backgroundColor = new StyleColor(newColor);
    }
}
