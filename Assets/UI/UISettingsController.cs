using UnityEngine;
using UnityEngine.UIElements;

public class UISettingsController : MonoBehaviour
{
    public UIDocument uiDocument;
    public Material targetMaterial;

    private VisualElement colorPreview;
    private Slider redSlider;
    private Slider greenSlider;
    private Slider blueSlider;

    // 3D object to move/scale
    public Transform metronomeObject;

    private Slider xSlider;
    private Slider ySlider;
    private Slider sizeSlider;

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        // Color sliders
        redSlider = root.Q<Slider>("RedSlider");
        greenSlider = root.Q<Slider>("GreenSlider");
        blueSlider = root.Q<Slider>("BlueSlider");
        colorPreview = root.Q<VisualElement>("ColourPreview");

        // Transform sliders
        xSlider = root.Q<Slider>("XSlider");
        ySlider = root.Q<Slider>("YSlider");
        sizeSlider = root.Q<Slider>("SizeSlider");

        // Register callbacks
        redSlider.RegisterValueChangedCallback(evt => UpdateColor());
        greenSlider.RegisterValueChangedCallback(evt => UpdateColor());
        blueSlider.RegisterValueChangedCallback(evt => UpdateColor());

        xSlider.RegisterValueChangedCallback(evt => UpdateTransform());
        ySlider.RegisterValueChangedCallback(evt => UpdateTransform());
        sizeSlider.RegisterValueChangedCallback(evt => UpdateTransform());

        // Initialize
        UpdateColor();
        UpdateTransform();
    }

    private void UpdateColor()
    {
        float r = redSlider.value;
        float g = greenSlider.value;
        float b = blueSlider.value;
        Color newColor = new Color(r, g, b, 1f);

        targetMaterial.SetColor("_BaseColor", newColor);
        targetMaterial.SetColor("_SpecColor", newColor);
        targetMaterial.SetColor("_EmissionColor", newColor);

        colorPreview.style.backgroundColor = new StyleColor(newColor);
    }

    private void UpdateTransform()
    {
        if (metronomeObject == null) return;

        // Map slider values to position and scale
        float x = xSlider.value;
        float y = ySlider.value;
        float size = sizeSlider.value;

        // Set 3D object position
        Vector3 newPos = new Vector3(x, y, metronomeObject.position.z); // keep original Z
        metronomeObject.position = newPos;

        // Set uniform scale
        metronomeObject.localScale = Vector3.one * size;
    }
}
