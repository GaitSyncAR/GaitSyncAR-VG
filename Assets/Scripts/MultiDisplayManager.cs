using UnityEngine;

public class MultiDisplayManager : MonoBehaviour
{
    public Camera mainCamA;      // Main screen camera 1
    public Camera mainCamB;      // Main screen camera 2
    public Camera externalCam;   // External display camera

    private int lastDisplayCount;

    void Start()
    {
        // Initial setup
        SetupDisplays();
        lastDisplayCount = Display.displays.Length;

        // Subscribe to Unity display update event
        Display.onDisplaysUpdated += OnDisplaysUpdated;
    }

    void OnDestroy()
    {
        // Unsubscribe when destroyed
        Display.onDisplaysUpdated -= OnDisplaysUpdated;
    }

    void SetupDisplays()
    {
        int displayCount = Display.displays.Length;
        Debug.Log("Detected displays: " + displayCount);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Activate all external displays
        for (int i = 1; i < displayCount; i++)
        {
            if (!Display.displays[i].active)
            {
                Debug.Log("Activating display " + i);
                Display.displays[i].Activate();
            }
        }
    }

    void OnDisplaysUpdated()
    {
        int displayCount = Display.displays.Length;

        if (displayCount != lastDisplayCount)
        {
            Debug.Log("Display count changed. Re-setup displays.");
            SetupDisplays();
            lastDisplayCount = displayCount;
        }
    }
}
