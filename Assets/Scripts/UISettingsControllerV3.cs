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
    public Material targetMaterial;
    
    [Header("Movement Settings")]
    public Transform metronomeObject; // The object to move/scale (Parent)
    public float movementStep = 0.5f; 
    public float scaleStep = 0.1f;

    [Header("Preview Settings")]
    public RenderTexture metronomePreviewTexture;

    [Tooltip("UXML Template for profile rows in the Templates Page")]
    public VisualTreeAsset profileRowTemplate;

    // --- PRIVATE UI REFERENCES ---
    private VisualElement root;
    private VisualElement remotePage;
    private VisualElement calibrationPage;
    private Label bpmLabel;
    private Button startStopBtn;
    private List<VisualElement> _allPages = new List<VisualElement>();
    private VisualElement _currentPage;
    private VisualElement templatesPage;
    
    // Calibration Page Containers
    private VisualElement positionControls;
    private VisualElement shapeControls;
    private VisualElement colourControls;

    void Start()
    {
        if (!Application.isPlaying) return;
        if (uiDocument == null) return;

        // Fetching the root element of the UI document
        root = uiDocument.rootVisualElement;

        // 1. Setup Navigation
        SetupNavigation();

        // 2. Setup Remote (BPM Control)
        SetupRemotePage();

        // 3. Setup TemplatesPage
        SetupTemplatesPage();

        // 4. Setup Calibration
        SetupCalibrationTabs();
        SetupPositionControls();
        SetupShapeControls();
        SetupColorControls();

        // 5. LOAD SAVED SETTINGS
        LoadSettings();

        // 6. Assigning rendered texture safely
        var metronomePreview = calibrationPage.Q<VisualElement>("Metronome");
        if (metronomePreview != null && metronomePreviewTexture != null)
        {
            metronomePreview.style.backgroundImage = Background.FromRenderTexture(metronomePreviewTexture);
        }
    }

    void OnDisable()
    {
        // 4. SAVE SETTINGS ON EXIT (New!)
        if (Application.isPlaying)
        {
            SaveSettings();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // pauseStatus = true means the app is going to the background
        if (pauseStatus)
        {
            SaveSettings();
        }
    }

    // =========================================================
    // CURRENT SAVE & LOADING SYSTEM
    // =========================================================
    private void SaveSettings()
    {
        PlayerPrefs.SetString("CurrentProfile", ProfileManager.Instance.currentProfile.profileName); 
        ProfileManager.Instance.SaveProfile();
        PlayerPrefs.Save();
        Debug.Log("Settings Saved!");
    }

    private void LoadSettings()
    {  
        ProfileManager profileManagerObj = ProfileManager.Instance;

        // check if we have a profile saved, if not create default
        if (PlayerPrefs.HasKey("CurrentProfile"))
        {
            string profileName = PlayerPrefs.GetString("CurrentProfile");
            profileManagerObj.LoadProfile(profileName);
        }
        else
        {
            profileManagerObj.currentProfile = new UserProfile("Default");
            profileManagerObj.SaveProfile();
            SaveSettings(); // Saving the default profile immediately to ensure we have a file for future loads
        }

        // 1. Loading Beats Per Minute (BPM)
        if (metronomeArm!= null)
        {
            metronomeArm.bpm = profileManagerObj.currentProfile.bpm;
            UpdateBPMDisplay();
        }

        // 2. Loading Metronome Position & Uniform Scal
        if (metronomeObject != null)
        {
            metronomeObject.position = profileManagerObj.currentProfile.metronomePosition;
            metronomeObject.localScale = profileManagerObj.currentProfile.metronomeSize;
        }

        // load colour
        if (metronomeRenderer != null)
        {
            Color c = profileManagerObj.currentProfile.metronomeColour;
            targetMaterial.SetColor("_BaseColor", c);
            targetMaterial.SetColor("_SpecColor", c);
            targetMaterial.SetColor("_EmissionColor", c);

            // setting UI sliders to match loaded color
            var rSlider = colourControls.Q("Red_Slider").Q<SliderInt>();
            var gSlider = colourControls.Q("Green_Slider").Q<SliderInt>();
            var bSlider = colourControls.Q("Blue_Slider").Q<SliderInt>();
            rSlider.value = (int)(c.r * 255);
            gSlider.value = (int)(c.g * 255);
            bSlider.value = (int)(c.b * 255);
        }


        // 3. Load Stretch (Bar & Arm)
        /*
        if (metronomeBar != null)
        {
            Vector3 s = metronomeBar.localScale;
            s.y = profileManagerObj.currentProfile.metronomeBarScaleY;
            metronomeBar.localScale = s;
        }
        */

        // load local stretch (disabled)
        Debug.Log("Settings Loaded and applied!");
    }

    // =========================================================
    // NAVIGATION
    // =========================================================
    private void SetupNavigation()
    {
        // locating pages
        remotePage = root.Q<VisualElement>("RemotePage");
        calibrationPage = root.Q<VisualElement>("CalibrationPage");
        templatesPage = root.Q<VisualElement>("TemplatesPage");

        // adding pages to list for easy management
        _allPages.Add(remotePage);
        _allPages.Add(calibrationPage);
        _allPages.Add(templatesPage);

        // adding button callbacks
        var toSettingsBtn = root.Q<Button>("ToSettings"); // Settings button inside Title
        var toTemplatesBtn = root.Q<Button>("ToTemplates");

        // setting button functionality to switch between pages
        toSettingsBtn.RegisterCallback<ClickEvent>(e => ShowPage(calibrationPage));
        toTemplatesBtn.RegisterCallback<ClickEvent>(e => ShowPage(templatesPage));

        // Making all back btns return to the RemotePage (Main Menu)
        List<Button> backButtons = root.Query<Button>("BackBtn").ToList();
        foreach (Button btn in backButtons)
        {
            btn.RegisterCallback<ClickEvent>(e => ShowPage(remotePage));
        }
    }

    private void ShowPage(VisualElement targetPage)
    {
       if (targetPage == null) return;

        // Run your custom logic for leaving a specific page (like your old SaveSettings)
        if (_currentPage == calibrationPage && targetPage != calibrationPage)
        {
            SaveSettings();
        }

        // Turning all other pages off, and the target page on
        foreach (VisualElement page in _allPages)
        {
            if (page != null)
            {
                // If it's the target page, use Flex. Otherwise, use None.
                page.style.display = (page == targetPage) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // Update the current page tracker
        _currentPage = targetPage;
    }

    // =========================================================
    // REMOTE PAGE
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

        // Adding new value to current profile for saving
        ProfileManager.Instance.currentProfile.bpm = metronomeArm.bpm;

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
    // TEMPLATES PAGE
    // =========================================================
    private ScrollView profileScrollView;

    private void SetupTemplatesPage()
    {
        profileScrollView = root.Q<ScrollView>("ScrollView"); 
        PopulateProfileList();
    }

    public void PopulateProfileList()
    {
        if (profileScrollView == null) return;

        // Clearing out the dummy template
        profileScrollView.Clear();

        // fertching and looping over saved profiles to populate UI
        List<string> savedProfiles = ProfileManager.Instance.GetAvailableProfiles();
        foreach (string profileName in savedProfiles)
        {
            VisualElement newRow = profileRowTemplate.Instantiate();
            Button rowButton = newRow.Q<Button>("SavedTemplateBtn"); 
            
            if (rowButton != null)
            {
                // Set the text of the button to the profile name
                rowButton.text = profileName;
            }

            profileScrollView.Add(newRow);
        }

        Debug.Log($"Loaded {savedProfiles.Count} profiles into the UI.");
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
        if (targetMaterial == null) return;

        var rSlider = colourControls.Q("Red_Slider").Q<SliderInt>();
        var gSlider = colourControls.Q("Green_Slider").Q<SliderInt>();
        var bSlider = colourControls.Q("Blue_Slider").Q<SliderInt>();

        // initlize starting color values on sliders
        Color c = targetMaterial.color;
        rSlider.value = Mathf.RoundToInt(c.r * 255f);
        gSlider.value = Mathf.RoundToInt(c.g * 255f);
        bSlider.value = Mathf.RoundToInt(c.b * 255f);

        System.Action<int> onColorChange = (val) => 
        {
            if (metronomeRenderer != null)
            {
                Color newColor = new Color(rSlider.value / 255f , gSlider.value / 255f , bSlider.value / 255f, 1f);

                targetMaterial.SetColor("_BaseColor", newColor);
                targetMaterial.SetColor("_SpecColor", newColor);
                targetMaterial.SetColor("_EmissionColor", newColor);

                // apply to current profile for saving
                ProfileManager.Instance.currentProfile.metronomeColour = newColor;
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

        // updating current profile value for saving
        ProfileManager.Instance.currentProfile.metronomePosition = metronomeObject.position;

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

            // updating current profile value for saving
            ProfileManager.Instance.currentProfile.metronomeSize = newScale;
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