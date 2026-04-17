using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    public UIDocument         uiDocument;
    public VisualTreeAsset    profileRowTemplate;
    public VisualTreeAsset    popupTemplate;
    public VisualTreeAsset    yesNoPopupTemplate;

    [Header("Metronome")]
    public MetronomeController  metronome;
    public RenderTexture        metronomePreviewTexture;

    [Header("Movement Settings")]
    public float movementStep = 0.5f;
    public float scaleStep   = 0.1f;

    [Header("Sensor Icons")]
    public Texture2D ankleConnectedIcon;
    public Texture2D ankleDisconnectedIcon;

    // -- Page controllers --
    private RemotePageController       _remoteCtrl;
    private TemplatesPageController   _templatesCtrl;
    private CalibrationPageController _calibrationCtrl;

    // -- Navigation --
    private VisualElement _root;
    private VisualElement _currentPage;
    private readonly List<VisualElement> _pages = new();

    void Awake()
    {
        PopupManager.Instance.Initialize(uiDocument, popupTemplate, yesNoPopupTemplate);
    }

    void Start()
    {
        if (!Application.isPlaying || uiDocument == null) return;

        _root = uiDocument.rootVisualElement;
        LoadAndApplyProfile();

        _remoteCtrl      = new RemotePageController();
        _templatesCtrl  = new TemplatesPageController();
        _calibrationCtrl = new CalibrationPageController();

        _remoteCtrl.Initialize(uiDocument, metronome);
        _templatesCtrl.Initialize(uiDocument, profileRowTemplate, popupTemplate, yesNoPopupTemplate);
        _calibrationCtrl.InitWithRefs(uiDocument, metronome, movementStep, scaleStep);

        BuildNavigation();

        BLEManager.OnBatteryLevelReceived += OnBatteryReceived;
        BLEManager.OnDeviceDisconnected   += OnDisconnected;
        BLEManager.OnDeviceReconnected    += OnReconnected;

        var preview = _root.Q<VisualElement>("CalibrationPage")?.Q<VisualElement>("Metronome");
        if (preview != null && metronomePreviewTexture != null)
            preview.style.backgroundImage = Background.FromRenderTexture(metronomePreviewTexture);

        // Initialise UiScaler
        var uiScaler = GetComponent<UiScaler>();
        uiScaler?.ScaleAllLabels();

        UIEventBus.ProfileApplied += OnProfileApplied;
    }

    void OnDisable()
    {
        if (!Application.isPlaying) return;

        // Safely check if the manager and profile still exist before saving
        if (ProfileManager.Instance != null && ProfileManager.Instance.currentProfile != null)
        {
            ProfileManager.Instance.SaveProfile();
            PlayerPrefs.SetString("CurrentProfile", ProfileManager.Instance.currentProfile.profileName);
            PlayerPrefs.Save();
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) OnDisable();
    }

    // ----------------------------------------------------------
    // Navigation
    // ----------------------------------------------------------
    private void BuildNavigation()
    {
        var remote    = _root.Q<VisualElement>("RemotePage");
        var calib     = _root.Q<VisualElement>("CalibrationPage");
        var templates = _root.Q<VisualElement>("TemplatesPage");

        _pages.Add(remote);
        _pages.Add(calib);
        _pages.Add(templates);

        _root.Q<Button>("ToSettings").RegisterCallback<ClickEvent>(_ => ShowPage(calib));
        _root.Q<Button>("ToTemplates").RegisterCallback<ClickEvent>(_ => ShowPage(templates));

        foreach (var btn in _root.Query<Button>("BackBtn").ToList())
            btn.RegisterCallback<ClickEvent>(_ => ShowPage(remote));
    }

    private void ShowPage(VisualElement page)
    {
        if (page == null || page == _currentPage) return;

        // ── Page lifecycle ──
        if (_currentPage?.name == "CalibrationPage")
            _calibrationCtrl.OnPageHide();

        foreach (var p in _pages)
            p.style.display = (p == page) ? DisplayStyle.Flex : DisplayStyle.None;

        if (page.name == "RemotePage")
            _remoteCtrl.OnPageShow();

        _currentPage = page;
    }

    // ----------------------------------------------------------
    // BLE
    // ----------------------------------------------------------
    private void OnBatteryReceived(string device, int level)
    {
        var lbl = _root.Q<Label>(device == "GaitSync-Right"
            ? "RightSensorBattery" : "LeftSensorBattery");
        if (lbl != null) lbl.text = $"{level}%";
    }

    private void OnDisconnected(string device)
    {
        var btn = _root.Q<Button>(device == "GaitSync-Right"
            ? "RightAnkleSensor" : "LeftAnkleSensor");
        if (btn != null) btn.iconImage = ankleDisconnectedIcon;

        var lbl = _root.Q<Label>(device == "GaitSync-Right"
            ? "RightSensorBattery" : "LeftSensorBattery");
        if (lbl != null) lbl.text = "";
    }

    private void OnReconnected(string device)
    {
        var btn = _root.Q<Button>(device == "GaitSync-Right"
            ? "RightAnkleSensor" : "LeftAnkleSensor");
        if (btn != null) btn.iconImage = ankleConnectedIcon;
    }

    // ----------------------------------------------------------
    // Profile Save / Load
    // ----------------------------------------------------------
    private void OnProfileApplied(string profileName)
    {
        Debug.Log($"[UIManager] Heard the apply button! Loading profile: {profileName}");
        LoadAndApplyProfile();
    }

    private void LoadAndApplyProfile()
    {
        if (!PlayerPrefs.HasKey("CurrentProfile"))
        {
            ProfileManager.Instance.currentProfile = new UserProfile("Default");
            ProfileManager.Instance.SaveProfile();
        }
        else
        {
            ProfileManager.Instance.LoadProfile(PlayerPrefs.GetString("CurrentProfile"));
        }

        UserProfile profile = ProfileManager.Instance.currentProfile;
        Color c = profile.metronomeColour;
            var tab = _root.Q<VisualElement>("ColourControls");
            if (tab != null)
            {
                tab.Q("Red_Slider").Q<SliderInt>().value   = (int)(c.r * 255);
                tab.Q("Green_Slider").Q<SliderInt>().value = (int)(c.g * 255);
                tab.Q("Blue_Slider").Q<SliderInt>().value  = (int)(c.b * 255);
            }

        UIEventBus.EmitBPM(profile.bpm);
        metronome.ApplyProfile(profile);
        Debug.Log("Profile loaded and applied.");
    }
}
