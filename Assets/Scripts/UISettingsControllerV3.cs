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

    [Tooltip("Popup template for confirmation dialogs (e.g., deleting profiles)")]
    public VisualTreeAsset popupTemplate;

    [Tooltip("Popup template for yes/no confirmation dialogs")]
    public VisualTreeAsset yesNoPopupTemplate;

    [Tooltip("Ankle Sensor Connected Icon")]
    public Texture2D ankleSensorConnectedIcon;

    [Tooltip("Ankle Sensor Disconnected Icon")]
    public Texture2D ankleSensorDisconnectedIcon;

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

    // other private fields
    private string _selectedTemplateName = "";

    // Singletons
    private PopupManager popupManager;
    private ProfileManager profileManager;

    void Start()
    {
        if (!Application.isPlaying) return;
        if (uiDocument == null) return;

        // Fetching the root element of the UI document
        root = uiDocument.rootVisualElement;

        // Initializing PopupManager with necessary references
        popupManager = PopupManager.Instance;
        popupManager.Initialize(uiDocument, popupTemplate, yesNoPopupTemplate);
        // Fetching the ProfileManager instance
        profileManager = ProfileManager.Instance;

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

        // 5. Setup battery level listener
        BLEManager.OnBatteryLevelReceived += (deviceName, level) => 
        {
            // Updating label text of RightSensorBattery
            var batteryLabel = root.Q<Label>(deviceName == "GaitSync-Right" ? "RightSensorBattery" : "LeftSensorBattery");
            if (batteryLabel != null) { batteryLabel.text = level.ToString() + "%"; }
        };

        // setup connection listeners to update sensor icons
        BLEManager.OnDeviceDisconnected += (deviceName) =>
        {
            var sensorBtn = root.Q<Button>(deviceName == "GaitSync-Right" ? "RightAnkleSensor" : "LeftAnkleSensor");
            if (sensorBtn != null) { sensorBtn.iconImage = ankleSensorDisconnectedIcon; }

            var batteryLabel = root.Q<Label>(deviceName == "GaitSync-Right" ? "RightSensorBattery" : "LeftSensorBattery");
            if (batteryLabel != null) { batteryLabel.text = ""; }
        };

        BLEManager.OnDeviceReconnected += (deviceName) =>
        {
            var sensorBtn = root.Q<Button>(deviceName == "GaitSync-Right" ? "RightAnkleSensor" : "LeftAnkleSensor");
            if (sensorBtn != null) { sensorBtn.iconImage = ankleSensorConnectedIcon; }
        };

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
        PlayerPrefs.SetString("CurrentProfile", profileManager.currentProfile.profileName); 
        profileManager.SaveProfile();
        PlayerPrefs.Save();
        Debug.Log("Settings Saved!");
    }

    private void LoadSettings()
    {  
        // check if we have a profile saved, if not create default
        if (PlayerPrefs.HasKey("CurrentProfile"))
        {
            string profileName = PlayerPrefs.GetString("CurrentProfile");
            profileManager.LoadProfile(profileName);
        }
        else
        {
            profileManager.currentProfile = new UserProfile("Default");
            profileManager.SaveProfile();
            SaveSettings(); // Saving the default profile immediately to ensure we have a file for future loads
        }

        // 1. Loading Beats Per Minute (BPM)
        if (metronomeArm!= null)
        {
            metronomeArm.bpm = profileManager.currentProfile.bpm;
            UpdateBPMDisplay();
        }

        // 2. Loading Metronome Position & Uniform Scal
        if (metronomeObject != null)
        {
            metronomeObject.position = profileManager.currentProfile.metronomePosition;
            metronomeObject.localScale = profileManager.currentProfile.metronomeSize;
        }

        // load colour
        if (metronomeRenderer != null)
        {
            Color c = profileManager.currentProfile.metronomeColour;
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
            s.y = profileManager.currentProfile.metronomeBarScaleY;
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
        profileManager.currentProfile.bpm = metronomeArm.bpm;

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

        // Querying all the buttons on the Templates Page
        Button saveCurrentSettingsBtn = root.Q<Button>("SaveCurrentSettingsBtn");
        Button applySettingsBtn = root.Q<Button>("ApplySettingsBtn");
        Button renameBtn = root.Q<Button>("RenameBtn");
        Button resetBtn = root.Q<Button>("ResetBtn");
        Button deleteBtn = root.Q<Button>("DeleteBtn");

        // Hook up button functionality
        saveCurrentSettingsBtn.clicked += OnSaveCurrentSettingsClicked;
        applySettingsBtn.clicked += ApplySelectedTemplate;
        renameBtn.clicked += OnRenameClicked;
        resetBtn.clicked += OnResetToDefaultClicked;
        deleteBtn.clicked += OnDeleteClicked;

        PopulateProfileList();
    }

    public void PopulateProfileList()
    {
        if (profileScrollView == null) return;

        // Clearing out old entries before repopulating
        profileScrollView.Clear();

        // fertching and looping over saved profiles to populate UI
        List<string> savedProfiles = profileManager.GetAvailableProfiles();
        foreach (string profileName in savedProfiles)
        {
            VisualElement newRow = profileRowTemplate.Instantiate();
            Button rowButton = newRow.Q<Button>("SavedTemplateBtn"); 
            
            if (rowButton != null)
            {
                // Set the text of the button to the profile name
                rowButton.text = profileName;
            }

            // Adding functionality on click
            rowButton.clicked += () => SelectTemplate(profileName);

            profileScrollView.Add(newRow);
        }

        Debug.Log($"Loaded {savedProfiles.Count} profiles into the UI.");
    }

    private void SelectTemplate(string templateName)
    {
        _selectedTemplateName = templateName;
        
        Label selectedTitle = root.Q<Label>("SelectedTitle");
        if (selectedTitle != null)
        {
            selectedTitle.text = $"Selected Template: {templateName}";
        }

        PlayHaptic();
    }

    private void OnDeleteClicked()
    {
        if (string.IsNullOrEmpty(_selectedTemplateName)) return;

        popupManager.ShowPopup(
            titleText: $"Delete '{_selectedTemplateName}'?",
            actionText: "Delete",
            onCancel: () => { Debug.Log("Deletion cancelled."); PlayHaptic(); },
            onAction: (input) => 
            {
                profileManager.DeleteProfile(_selectedTemplateName);
                PopulateProfileList();
                SelectTemplate(""); // Clear selection after deletion
                PlayHaptic();
            },
            includeInputField: false
        );
    }

    public void OnResetToDefaultClicked()
    {
        popupManager.ShowPopup(
            titleText: $"Reset Current Template Settings to Default?",
            actionText: "Reset",
            onCancel: () => { Debug.Log("Reset cancelled."); PlayHaptic(); },
            onAction: (input) => 
            {
                // reseting current profile to default values but keeping same profile name
                var oldName = profileManager.currentProfile.profileName;
                profileManager.currentProfile = new UserProfile(oldName);
                SaveSettings();
                LoadSettings();
                PopulateProfileList();
                PlayHaptic();
            },
            includeInputField: false
        );
    }

    private void ApplySelectedTemplate()
    {
        if (string.IsNullOrEmpty(_selectedTemplateName)) return;

        // check if we aren't applying the currently loaded profile (no need to re-apply the same settings)
        if (PlayerPrefs.GetString("CurrentProfile") == _selectedTemplateName)
        {
            Debug.Log("Selected template is already the current profile. No changes applied.");
            return;
        }

         popupManager.ShowPopup(
            titleText: $"Apply '{_selectedTemplateName}' Settings?",
            actionText: "Apply",
            onCancel: () => { Debug.Log("Apply cancelled."); PlayHaptic(); },
            onAction: (input) => 
            {
                // save old settings before applying new ones
                SaveSettings();

                // Load the profile and apply it
                PlayerPrefs.SetString("CurrentProfile", _selectedTemplateName);
                profileManager.LoadProfile(_selectedTemplateName);
                LoadSettings(); // Apply the loaded settings to the UI and metronome
                SaveSettings(); // Save the applied profile as the current settings

                PlayHaptic();
            },
            includeInputField: false
        );
    }

    // ================ POPUPS ==================================
    private void OnSaveCurrentSettingsClicked()
    {
        // Use the PopupManager to spawn our default popup
        popupManager.ShowPopup(
            titleText: "Save New Template",
            actionText: "Save",
            onCancel: () => 
            {
                Debug.Log("Save cancelled by user.");
                PlayHaptic();
            },
            onAction: (inputName) => 
            {
                if (string.IsNullOrEmpty(inputName)) return; // Prevent empty names

                // Logic to save as a new profile
                profileManager.currentProfile.profileName = inputName;
                SaveSettings(); 
                PopulateProfileList(); // Refresh the UI list
                PlayHaptic();
                print($"New template '{inputName}' saved and applied.");
            }
        );
        print("Save Current Settings button clicked, popup should be displayed.");
    }

    private void OnRenameClicked()
    {
        if (string.IsNullOrEmpty(_selectedTemplateName)) return;

        popupManager.ShowPopup(
            titleText: $"Rename '{_selectedTemplateName}'",
            actionText: "Rename",
            onCancel: () => { Debug.Log("Rename cancelled."); PlayHaptic(); },
            onAction: (newName) => 
            {
                if (string.IsNullOrEmpty(newName)) return;
                
                Debug.Log($"Renaming {_selectedTemplateName} to {newName}");
                profileManager.RenameProfile(_selectedTemplateName, newName);
                
                PopulateProfileList();
            }
        );
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
                profileManager.currentProfile.metronomeColour = newColor;
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
        profileManager.currentProfile.metronomePosition = metronomeObject.position;

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
            profileManager.currentProfile.metronomeSize = newScale;
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