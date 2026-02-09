using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UISettingsControllerV3 : MonoBehaviour
{
    [Header("UI Setup")]
    public UIDocument uiDocument;
    
    [Header("Metronome References")]
    public MetronomeArm metronomeArm; 
    public Renderer metronomeRenderer;
    public Transform metronomeBar;
    public Transform metronomeArmVisuals;
    
    [Header("Movement Settings")]
    public Transform metronomeObject; // The object to move/scale (Parent)
    public float movementStep = 0.5f; 
    public float scaleStep = 0.1f;

    [Header("Preview Settings")]
    public RenderTexture metronomePreviewTexture;

    // --- PRIVATE UI REFERENCES ---
    private VisualElement root;
    private VisualElement remotePage;
    private VisualElement calibrationPage;
    private Label bpmLabel;
    private Button startStopBtn;
    
    // Calibration Page Containers
    private VisualElement positionControls;
    private VisualElement shapeControls;
    private VisualElement colourControls;

    void Start()
    {
        if (!Application.isPlaying) return;
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // 1. Setup Navigation
        SetupNavigation();

        // 2. Setup Remote (BPM Control)
        SetupRemotePage();

        // 3. Setup Calibration
        SetupCalibrationTabs();
        SetupPositionControls();
        SetupShapeControls();
        SetupColorControls();

        // Assigning rendered texture safely
        var metronomePreview = calibrationPage.Q<VisualElement>("Metronome");
        if (metronomePreview != null && metronomePreviewTexture != null)
        {
            metronomePreview.style.backgroundImage = Background.FromRenderTexture(metronomePreviewTexture);
        }
    }

    // =========================================================
    // NAVIGATION
    // =========================================================
    private void SetupNavigation()
    {
        remotePage = root.Q<VisualElement>("RemotePage");
        calibrationPage = root.Q<VisualElement>("CalibrationPage");

        var toSettingsBtn = root.Q<Button>("ToSettings"); // Settings button inside Title
        var backBtn = root.Q<Button>("BackBtn");

        toSettingsBtn.RegisterCallback<ClickEvent>(e => SwitchPage(true));
        backBtn.RegisterCallback<ClickEvent>(e => SwitchPage(false));
    }

    private void SwitchPage(bool showCalibration)
    {
        if (showCalibration)
        {
            if (remotePage != null) remotePage.style.display = DisplayStyle.None;
            if (calibrationPage != null) calibrationPage.style.display = DisplayStyle.Flex;
        }
        else
        {
            if (calibrationPage != null) calibrationPage.style.display = DisplayStyle.None;
            if (remotePage != null) remotePage.style.display = DisplayStyle.Flex;
        }
    }

    // =========================================================
    // REMOTE PAGE (BPM LOGIC UPDATED)
    // =========================================================
    private void SetupRemotePage()
    {
        bpmLabel = root.Q<Label>("BPM-lbl");
        
        var increaseBtn = root.Q<Button>("increase");
        var decreaseBtn = root.Q<Button>("decrease");
        startStopBtn = remotePage.Q<Button>("StartStop");

        increaseBtn.clicked += () => ChangeBPM(5);
        decreaseBtn.clicked += () => ChangeBPM(-5);
        startStopBtn.clicked += ToggleStartStop;

        UpdateBPMDisplay();
    }

    private void ChangeBPM(int amount)
    {
        if (metronomeArm == null) return;
        metronomeArm.bpm += amount;

        // Safety floor (cannot go below 0)
        if (metronomeArm.bpm < 0) metronomeArm.bpm = 0;

        UpdateBPMDisplay();
        PlayHaptic();
    }

    private void UpdateBPMDisplay()
    {
        if (metronomeArm != null && bpmLabel != null)
        {
            // Update the label with the real value
            bpmLabel.text = metronomeArm.bpm.ToString("0"); // "0" formats as whole number
        }
    }

    private void ToggleStartStop()
    {
        metronomeArm.isRunning = !metronomeArm.isRunning; // Toggle the actual metronome state
        
        if (metronomeArm.isRunning)
        {
            startStopBtn.style.backgroundColor = new StyleColor(new Color(0, 0.89f, 1f)); // Cyan
            startStopBtn.text = "■ STOP";
            startStopBtn.style.color = Color.black;
        }
        else
        {
            startStopBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f)); // Dark Grey
            startStopBtn.text = "▶ START";
            startStopBtn.style.color = Color.white;
        }
        PlayHaptic();
    }

    // =========================================================
    // CALIBRATION & TABS
    // =========================================================
    private void SetupCalibrationTabs()
    {
        positionControls = root.Q<VisualElement>("PositionControls");
        shapeControls = root.Q<VisualElement>("ShapeControls");
        colourControls = root.Q<VisualElement>("ColourControls");

        root.Q<Button>("PosBtn").clicked += () => ShowTab("pos");
        root.Q<Button>("ShapeBtn").clicked += () => ShowTab("shape");
        root.Q<Button>("ColourBtn").clicked += () => ShowTab("col");
        
        ShowTab("pos");
    }

    private void ShowTab(string tabName)
    {
        positionControls.style.display = DisplayStyle.None;
        shapeControls.style.display = DisplayStyle.None;
        colourControls.style.display = DisplayStyle.None;

        if (tabName == "pos") positionControls.style.display = DisplayStyle.Flex;
        else if (tabName == "shape") shapeControls.style.display = DisplayStyle.Flex;
        else if (tabName == "col") colourControls.style.display = DisplayStyle.Flex;
        
        PlayHaptic();
    }

    // =========================================================
    // POSITION / SHAPE / COLOR
    // =========================================================
    private void SetupPositionControls()
    {
        List<VisualElement> rows = positionControls.Query("Horizontal_slot").ToList();
        
        // X Axis
        rows[0].Q<Button>("Left").clicked += () => MoveObject(new Vector3(-movementStep, 0, 0));
        rows[0].Q<Button>("Right").clicked += () => MoveObject(new Vector3(movementStep, 0, 0));
        
        // Y Axis
        rows[1].Q<Button>("Left").clicked += () => MoveObject(new Vector3(0, movementStep, 0));
        rows[1].Q<Button>("Right").clicked += () => MoveObject(new Vector3(0, -movementStep, 0));
        
        // Z Axis
        rows[2].Q<Button>("Left").clicked += () => MoveObject(new Vector3(0, 0, -movementStep));
        rows[2].Q<Button>("Right").clicked += () => MoveObject(new Vector3(0, 0, movementStep));
    }

    private void SetupShapeControls()
    {
        List<VisualElement> rows = shapeControls.Query("Horizontal_slot").ToList();

        // Size
        rows[0].Q<Button>("Left").clicked += () => ScaleObject(-scaleStep, false);
        rows[0].Q<Button>("Right").clicked += () => ScaleObject(scaleStep, false);
        
        // Stretch
        rows[1].Q<Button>("Left").clicked += () => ScaleObject(-scaleStep * 20, true);
        rows[1].Q<Button>("Right").clicked += () => ScaleObject(scaleStep * 20, true);
    }

    private void SetupColorControls()
    {
        var rSlider = colourControls.Q("Red_Slider").Q<SliderInt>();
        var gSlider = colourControls.Q("Green_Slider").Q<SliderInt>();
        var bSlider = colourControls.Q("Blue_Slider").Q<SliderInt>();

        System.Action<int> onColorChange = (val) => 
        {
            if (metronomeRenderer != null)
            {
                Color newCol = new Color(rSlider.value / 100f, gSlider.value / 100f, bSlider.value / 100f);
                metronomeRenderer.sharedMaterial.color = newCol;
            }
        };

        rSlider.RegisterValueChangedCallback(evt => onColorChange(evt.newValue));
        gSlider.RegisterValueChangedCallback(evt => onColorChange(evt.newValue));
        bSlider.RegisterValueChangedCallback(evt => onColorChange(evt.newValue));
    }

    // =========================================================
    // UTILS
    // =========================================================
    private void MoveObject(Vector3 delta)
    {
        if (metronomeObject != null) metronomeObject.position += delta;
        PlayHaptic();
    }

    private void ScaleObject(float amount, bool isStretchOnly)
    {
        // CASE 1: STRETCH (WIDER BAR + LONGER ARM)
        if (isStretchOnly)
        {
            // 1. Stretch the BAR Width
            if (metronomeBar != null)
            {
                Vector3 barScale = metronomeBar.localScale;
                barScale.y += amount; 
                if (barScale.y < 0.1f) barScale.y = 0.1f;
                metronomeBar.localScale = barScale;
            }

            // 2. Stretch the ARM Length & Position
            if (metronomeArmVisuals != null)
            {
                amount /= 25f; // Scale down the stretch effect for the arm to keep it balanced with the bar growth
                // A. Scale the Length (z-Axis)
                Vector3 armScale = metronomeArmVisuals.localScale;
                armScale.z += amount; 
                
                // Safety: Don't let it vanish or flip
                if (armScale.z < 0.1f) 
                {
                    amount = 0; // Cancel the movement if we hit the limit
                    armScale.z = 0.1f;
                }
                metronomeArmVisuals.localScale = armScale;

                // B. Adjust Position (The "Pivot Fix")
                // Move the arm down by half the amount of growth
                // This keeps the top of the arm attached to the pivot!
                Vector3 armPos = metronomeArmVisuals.localPosition;
                armPos.y -= amount * 0.5f; 
                
                metronomeArmVisuals.localPosition = armPos;
            }
        }
        // CASE 2: SIZE PARENT (UNIFORM)
        else
        {
            if (metronomeObject == null) return;
            Vector3 newScale = metronomeObject.localScale + (Vector3.one * amount);
            if (newScale.x < 0.1f) newScale = Vector3.one * 0.1f;
            metronomeObject.localScale = newScale;
        }
        PlayHaptic();
    }

    private void PlayHaptic()
    {
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }
}