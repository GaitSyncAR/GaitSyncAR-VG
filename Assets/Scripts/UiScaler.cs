using UnityEngine;
using UnityEngine.UIElements;

public class UiScaler : MonoBehaviour
{
    [Tooltip("Assign the UIDocument that holds your UI Toolkit elements")]
    public UIDocument uiDocument;

    [Tooltip("Reference resolution to scale from")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);

    private VisualElement root;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("Please assign a UIDocument in the inspector!");
            return;
        }

        root = uiDocument.rootVisualElement;
        ScaleApplySettings();
        ScaleAllLabels();
    }

    public void ScaleAllLabels()
    {
        if (root == null) return;

        // Calculating scale factor
        float widthScale = Screen.width / referenceResolution.x;
        float heightScale = Screen.height / referenceResolution.y;
        float scaleFactor = Mathf.Min(widthScale, heightScale);

        // Finding all Labels recursively
        var labels = root.Query<Label>().ToList();

        foreach (var label in labels)
        {
            // Getting resolved font size (or fallback to 14)
            float originalSize = label.resolvedStyle.fontSize;
            label.style.fontSize = originalSize * scaleFactor;
        }
    }

    public void ScaleApplySettings()
    {
        if (root == null) return;

        // 1. Finding the ApplySettingsButton
        var applySettingsButton = root.Q<Button>("ApplySettingsBtn");
        if (applySettingsButton == null) return;

        var container = applySettingsButton.parent;
        
        // container is relative to screen size
        float containerWidth = Screen.width * 0.7f;
        float containerHeight = Screen.height * 0.9f;

        // 3. Calculating the new size (50% of container width)
        float size = containerWidth * 0.5f;

        // 4. Apply size
        applySettingsButton.style.width = size;
        applySettingsButton.style.height = size;

        // 5. Centering the button
        float leftPos = (Screen.width*0.3f) + (containerWidth/2f) - (size / 2f);
        float topPos = (containerHeight / 2f) - (size / 2f);

        applySettingsButton.style.left = leftPos;
        applySettingsButton.style.top = topPos;
        
        // 6. Ensure Absolute positioning is active
        applySettingsButton.style.position = Position.Absolute;
    }
}