using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UISettingsControllerV3 : MonoBehaviour
{
    [Header("UI Setup")]
    public UIDocument uiDocument;
    
    [Header("Metronome Settings")]
    public Transform metronomeObject;
    public Renderer metronomeRenderer; // Assign the object's renderer here
    public float movementStep = 0.5f; // How much it moves per click
    public float scaleStep = 0.1f;    // How much it grows per click
    public int bpm = 120;

    // --- PRIVATE UI REFERENCES ---
    private VisualElement root;
    private VisualElement remotePage;
    private VisualElement calibrationPage;
    
    // Remote Page Elements
    private Label bpmLabel;
    private Button startStopBtn;
    private bool isRunning = false;

    // Calibration Page Containers
    private VisualElement positionControls;
    private VisualElement shapeControls;
    private VisualElement colourControls;

    void Start()
    {
        // Ensuring we are actually playing the game
        // This prevents the code from running while the Editor is just loading
        if (!Application.isPlaying) return;

        if (uiDocument == null)
        {
            Debug.LogError("UI Document is not assigned!");
            return;
        }

        root = uiDocument.rootVisualElement.Q<VisualElement>("Root");

        // 1. INITIALIZE PAGES
        SetupNavigation();

        // 2. INITIALIZE REMOTE CONTROLS (Walking Mode)
        SetupRemotePage();

        // 3. INITIALIZE CALIBRATION TABS
        SetupCalibrationTabs();

        // 4. INITIALIZE SETUP CONTROLS (Stepper Buttons)
        SetupPositionControls();
        SetupShapeControls();
        SetupColorControls();
    }

    // =========================================================
    // 1. NAVIGATION LOGIC
    // =========================================================
    // testing changes v2
    private void SetupNavigation()
    {
        remotePage = root.Q<VisualElement>("RemotePage");
        calibrationPage = root.Q<VisualElement>("CalibrationPage");

        // Button to go to Settings
        var toSettingsBtn = remotePage.Q<Button>(); // It's inside the Title label
        // testing crash

        // Button to go back to Remote
        var backBtn = root.Q<Button>("BackBtn");

        toSettingsBtn.RegisterCallback<ClickEvent>(e => SwitchPage(showCalibration: true));
        backBtn.RegisterCallback<ClickEvent>(e => SwitchPage(showCalibration: false));
    }

    private void SwitchPage(bool showCalibration)
    {
        if (showCalibration)
        {
            // HIDE REMOTE PAGE
            if (remotePage != null)
            {
                remotePage.style.display = DisplayStyle.None;
                remotePage.style.visibility = Visibility.Hidden; // Double-tap: Hide visually too
            }

            // SHOW CALIBRATION PAGE
            if (calibrationPage != null)
            {
                calibrationPage.style.display = DisplayStyle.Flex;
                calibrationPage.style.visibility = Visibility.Visible;
            }
        }
        else
        {
            // HIDE CALIBRATION PAGE
            if (calibrationPage != null)
            {
                calibrationPage.style.display = DisplayStyle.None;
                calibrationPage.style.visibility = Visibility.Hidden;
            }

            // SHOW REMOTE PAGE
            if (remotePage != null)
            {
                remotePage.style.display = DisplayStyle.Flex;
                remotePage.style.visibility = Visibility.Visible;
            }
        }
    }

    // =========================================================
    // 2. REMOTE PAGE LOGIC
    // =========================================================
    private void SetupRemotePage()
    {
        bpmLabel = root.Q<Label>("BPM-lbl");
        var increaseBtn = root.Q<Button>("increase");
        var decreaseBtn = root.Q<Button>("decrease");
        startStopBtn = root.Q<Button>("StartStop");

        increaseBtn.clicked += () => ChangeBPM(5);
        decreaseBtn.clicked += () => ChangeBPM(-5);
        startStopBtn.clicked += ToggleStartStop;

        UpdateBPMDisplay();
    }

    private void ChangeBPM(int amount)
    {
        bpm += amount;
        if (bpm < 10) bpm = 10; // Safety floor
        UpdateBPMDisplay();
        PlayHaptic();
    }

    private void UpdateBPMDisplay()
    {
        bpmLabel.text = bpm.ToString();
    }

    private void ToggleStartStop()
    {
        isRunning = !isRunning;
        
        if (isRunning)
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
        
        // TODO: Hook into your audio/metronome logic here
        // e.g., MetronomeSystem.SetRunning(isRunning);
    }

    // =========================================================
    // 3. CALIBRATION TABS
    // =========================================================
    private void SetupCalibrationTabs()
    {
        positionControls = root.Q<VisualElement>("PositionControls");
        shapeControls = root.Q<VisualElement>("ShapeControls");
        colourControls = root.Q<VisualElement>("ColourControls");

        var posBtn = root.Q<Button>("PosBtn");
        var shapeBtn = root.Q<Button>("ShapeBtn");
        var colBtn = root.Q<Button>("ColourBtn");

        posBtn.clicked += () => ShowTab("pos");
        shapeBtn.clicked += () => ShowTab("shape");
        colBtn.clicked += () => ShowTab("col");

        // Default to Position tab
        ShowTab("pos");
    }

    private void ShowTab(string tabName)
    {
        // Hide all
        positionControls.style.display = DisplayStyle.None;
        shapeControls.style.display = DisplayStyle.None;
        colourControls.style.display = DisplayStyle.None;

        // Show active
        switch (tabName)
        {
            case "pos": positionControls.style.display = DisplayStyle.Flex; break;
            case "shape": shapeControls.style.display = DisplayStyle.Flex; break;
            case "col": colourControls.style.display = DisplayStyle.Flex; break;
        }
        PlayHaptic();
    }

    // =========================================================
    // 4. STEPPER CONTROLS (Position/Shape)
    // =========================================================
    private void SetupPositionControls()
    {
        // The UXML uses the same name "Horizontal_slot" for all 3 rows. 
        // We must access them by index.
        List<VisualElement> rows = positionControls.Query("Horizontal_slot").ToList();

        // --- Row 0: X Axis ---
        var leftBtnX = rows[0].Q<Button>("Left");
        var rightBtnX = rows[0].Q<Button>("Right");
        var labelX = rows[0].Q<Button>("XPos"); // Middle button used as label

        leftBtnX.clicked += () => MoveObject(new Vector3(-movementStep, 0, 0), labelX);
        rightBtnX.clicked += () => MoveObject(new Vector3(movementStep, 0, 0), labelX);

        // --- Row 1: Y Axis ---
        var downBtnY = rows[1].Q<Button>("Right"); // UXML named it "Right" (Down arrow)
        var upBtnY = rows[1].Q<Button>("Left");    // UXML named it "Left" (Up arrow)
        var labelY = rows[1].Q<Button>("XPos");

        upBtnY.clicked += () => MoveObject(new Vector3(0, movementStep, 0), labelY);
        downBtnY.clicked += () => MoveObject(new Vector3(0, -movementStep, 0), labelY);

        // --- Row 2: Z Axis ---
        var farBtnZ = rows[2].Q<Button>("Right"); // "Far"
        var nearBtnZ = rows[2].Q<Button>("Left");  // "Near"
        var labelZ = rows[2].Q<Button>("XPos");

        nearBtnZ.clicked += () => MoveObject(new Vector3(0, 0, -movementStep), labelZ);
        farBtnZ.clicked += () => MoveObject(new Vector3(0, 0, movementStep), labelZ);
    }

    private void SetupShapeControls()
    {
        List<VisualElement> rows = shapeControls.Query("Horizontal_slot").ToList();

        // --- Row 0: Uniform Size ---
        var shrinkBtn = rows[0].Q<Button>("Left");
        var growBtn = rows[0].Q<Button>("Right");
        
        shrinkBtn.clicked += () => ScaleObject(-scaleStep, false);
        growBtn.clicked += () => ScaleObject(scaleStep, false);

        // --- Row 1: X Stretch ---
        var squishBtn = rows[1].Q<Button>("Left");
        var stretchBtn = rows[1].Q<Button>("Right");

        squishBtn.clicked += () => ScaleObject(-scaleStep, true);
        stretchBtn.clicked += () => ScaleObject(scaleStep, true);
    }

    private void MoveObject(Vector3 delta, Button displayLabel)
    {
        if (metronomeObject == null) return;
        metronomeObject.position += delta;
        PlayHaptic();
    }

    private void ScaleObject(float amount, bool isStretchOnly)
    {
        if (metronomeObject == null) return;

        if (isStretchOnly)
        {
            // Only affect X axis
            Vector3 newScale = metronomeObject.localScale;
            newScale.x += amount;
            // Prevent inverting
            if(newScale.x < 0.1f) newScale.x = 0.1f; 
            metronomeObject.localScale = newScale;
        }
        else
        {
            // Affect all axes
            metronomeObject.localScale += Vector3.one * amount;
        }
        PlayHaptic();
    }

    // =========================================================
    // 5. COLOR CONTROLS
    // =========================================================
    private void SetupColorControls()
    {
        // Notice: In UXML, Sliders are children of the VisualElements named "Red_Slider", etc.
        var rSlider = colourControls.Q("Red_Slider").Q<SliderInt>();
        var gSlider = colourControls.Q("Green_Slider").Q<SliderInt>();
        var bSlider = colourControls.Q("Blue_Slider").Q<SliderInt>();

        // Init default color
        if (metronomeRenderer != null)
        {
             Color current = metronomeRenderer.sharedMaterial.color;
             rSlider.value = (int)(current.r * 100);
             gSlider.value = (int)(current.g * 100);
             bSlider.value = (int)(current.b * 100);
        }

        System.Action<int> onColorChange = (val) => 
        {
            Color newCol = new Color(rSlider.value / 100f, gSlider.value / 100f, bSlider.value / 100f);
            UpdateMetronomeColor(newCol);
        };

        rSlider.RegisterValueChangedCallback(evt => onColorChange(evt.newValue));
        gSlider.RegisterValueChangedCallback(evt => onColorChange(evt.newValue));
        bSlider.RegisterValueChangedCallback(evt => onColorChange(evt.newValue));
    }

    private void UpdateMetronomeColor(Color c)
    {
        if (metronomeRenderer == null) return;

        metronomeRenderer.sharedMaterial.color = c; 
        
        // If URP/HDRP or Emission is used:
        metronomeRenderer.sharedMaterial.SetColor("_BaseColor", c);
        metronomeRenderer.sharedMaterial.SetColor("_EmissionColor", c);
    }

    // =========================================================
    // UTILS
    // =========================================================
    private void PlayHaptic()
    {
        // Simple vibration for feedback
        #if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
        #endif
    }
}