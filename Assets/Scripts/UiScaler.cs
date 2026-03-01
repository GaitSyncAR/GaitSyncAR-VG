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
        // ScaleApplySettings();
        ScaleAllLabels();
    }

    public void ScaleAllLabels()
    {
        if (root == null) return;

        // Calculating scale factor
        float widthScale = Screen.width / referenceResolution.x;
        float heightScale = Screen.height / referenceResolution.y;
        float scaleFactor = widthScale; // Mathf.Min(widthScale, heightScale);

        // Finding all text elements recursively
        var textElements = root.Query<TextElement>().ToList();
        
        foreach (var textElement in textElements)
        {
            // Unregister first to prevent duplicate callbacks
            textElement.UnregisterCallback<GeometryChangedEvent>(OnTextElementResized);
            
            // Register the callback
            textElement.RegisterCallback<GeometryChangedEvent>(OnTextElementResized);
        }
    }

    private void OnTextElementResized(GeometryChangedEvent evt)
    {
        TextElement textEl = evt.target as TextElement;
        if (textEl == null) return;

        float currentHeight = textEl.resolvedStyle.height;
        float currentWidth = textEl.resolvedStyle.width;
        float newFontSize = 0;

        // If Unity hasn't figured out the height yet we abort to avoid NaN issues
        if (float.IsNaN(currentHeight) || currentHeight <= 0) 
        {
            return; 
        }

        // check for custom tags, they scale differently
        if (textEl.ClassListContains("giant-scaling"))
        {
            newFontSize = (Screen.height / referenceResolution.y) * 50f; // 20px at reference resolution, scales with height
        }
        else // generic scaling for normal text elements
        {
            float paddedCurrentHeight = currentHeight * 0.9f; // reduce height for padding
            float paddedCurrentWidth = currentWidth * 0.9f; // reduce width for padding

            // calc new font size based on height and width, using multipliers to control scaling
            // Max size allowed by the HEIGHT of the box
            float heightMultiplier = 0.6f; 
            float maxHeightFit = paddedCurrentHeight * heightMultiplier;

            // Max size allowed by the WIDTH based on TEXT LENGTH
            int charCount = textEl.text.Length;
            float approxCharWidthRatio = 0.6f; 
            
            float maxWidthFit = paddedCurrentWidth / (charCount * approxCharWidthRatio);
            newFontSize = Mathf.Min(maxHeightFit, maxWidthFit);
        }

        // Applying safe to prevent resizing loops
        if (Mathf.Abs(textEl.resolvedStyle.fontSize - newFontSize) > 1.0f)
        {
            textEl.style.fontSize = newFontSize;
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