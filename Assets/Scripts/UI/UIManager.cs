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
    private SessionPageController     _sessionCtrl;

    // -- Navigation --
    private VisualElement _root;
    private VisualElement _currentPage;
    private VisualElement _lastPage;
    private readonly Dictionary<VisualElement, PageInterface> _pageMap = new();

    void Awake()
    {
        PopupManager.Instance.Initialize(uiDocument, popupTemplate, yesNoPopupTemplate);
    }

    void Start()
    {
        if (!Application.isPlaying || uiDocument == null) return;

        _root = uiDocument.rootVisualElement;
        LoadAndApplyProfile();

        _remoteCtrl      = new RemotePageController(uiDocument, _root.Q<VisualElement>("RemotePage"), metronome);
        _templatesCtrl   = new TemplatesPageController(uiDocument,  _root.Q<VisualElement>("TemplatesPage"), profileRowTemplate, popupTemplate, yesNoPopupTemplate);
        _calibrationCtrl = new CalibrationPageController(uiDocument, _root.Q<VisualElement>("CalibrationPage"), metronome, movementStep, scaleStep);
        _sessionCtrl     = new SessionPageController(uiDocument, _root.Q<VisualElement>("SessionReportPage"));

        _sessionCtrl.SetSessionData(SessionData.MockDataStable);

        BuildNavigation();

        UIEventBus.SessionSaved += ShowSessionSummary;
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

        // Testing
        // GaitMetrics.Instance.SaveSession(SessionData.UnstableMockData);
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
    public void RegisterPage(VisualElement pageElement, PageInterface pageCtrl)
    {
        if (pageCtrl == null) return;

        if (!_pageMap.ContainsKey(pageElement))
        {
            _pageMap.Add(pageElement, pageCtrl);
        }
    }
    private void BuildNavigation()
    {
        _root.Q<Button>("ToSettings").RegisterCallback<ClickEvent>(_ => ShowPage(_calibrationCtrl.PageRoot));
        _root.Q<Button>("ToTemplates").RegisterCallback<ClickEvent>(_ => ShowPage(_templatesCtrl.PageRoot));

        foreach (var btn in _root.Query<Button>("BackBtn").ToList())
            btn.RegisterCallback<ClickEvent>(_ => ShowPage(_remoteCtrl.PageRoot));

        // UI start
        ShowPage(_remoteCtrl.PageRoot);
    }

    private void ShowPage(VisualElement page)
    {
        if (page == null || page == _currentPage) return;
        // closing last page
        if (_currentPage != null && _pageMap.TryGetValue(_currentPage, out var oldCtrl))
        {
            oldCtrl.OnPageHide();
        }

        // Update tracking
        _lastPage = _currentPage;
        _currentPage = page;

        // Toggle Visibility
        foreach (var pageElement in _pageMap.Keys)
        {
            pageElement.style.display = (pageElement == page) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Entry logic for new page
        if (_pageMap.TryGetValue(page, out var newCtrl))
        {
            newCtrl.OnPageShow();
        }
    }
    // ----------------------------------------------------------
    // Session Popup
    // ----------------------------------------------------------
    public void ShowSessionSummary(SessionData sessionData)
    {
        // we neeed atleast 10 steps to calculate any meaningful metrics, so if we dont have that, we show a popup instead of the report page
        if(sessionData.leftStepHistory.Count + sessionData.rightStepHistory.Count <= 10) {
            PopupManager.Instance.ShowPopup(titleText: "Not enough step data recorded (>10). Unable to generate report.", actionText: "OK", includeInputField: false);
            return;
        }
        _sessionCtrl.SetSessionData(sessionData);
        ShowPage(_sessionCtrl.PageRoot);
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
        UIEventBus.EmitBPM(profile.bpm);
        UIEventBus.EmitColor(profile.metronomeColour);
        metronome.ApplyProfile(profile);
        Debug.Log("Profile loaded and applied.");
    }
}
