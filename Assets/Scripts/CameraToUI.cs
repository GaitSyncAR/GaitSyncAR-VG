using UnityEngine;
using UnityEngine.UIElements;

public class CameraToUI : MonoBehaviour
{
    public Camera targetCamera;
    public UIDocument uiDocument;
    public VisualElement displayElement;
    public RenderTexture renderTexture;

    private Texture2D tempTexture;

    void Start()
    {
        // Getting the VisualElement from UIDocument if not assigned
        if (displayElement == null)
            displayElement = uiDocument.rootVisualElement.Q<VisualElement>("CameraDisplay");

        // Creating a temporary Texture2D the same size as the RenderTexture
        tempTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
    }

    void Update()
    {
        // Setting active RenderTexture so we can read pixels
        RenderTexture.active = renderTexture;

        // Reading pixels into the temporary Texture2D
        tempTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tempTexture.Apply();

        // Assigning it to the VisualElement background
        displayElement.style.backgroundImage = new StyleBackground(tempTexture);

        // Cleaning up
        RenderTexture.active = null;
    }
}
