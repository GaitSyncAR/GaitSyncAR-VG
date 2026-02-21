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
}