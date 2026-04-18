using System;
using UnityEngine;
using UnityEngine.UIElements;

public class PopupManager
{
    // SINGLETON Setup
    private static PopupManager _instance;
    public static PopupManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PopupManager();
            }
            return _instance;
        }
    }
    private PopupManager() { } 

    // Class Fields
    private VisualTreeAsset _defaultPopupTemplate;
    private VisualTreeAsset _yesNoPopupTemplate;
    private UIDocument _mainUIDocument;
    private VisualElement _currentPopupInstance;
    private bool isInitialized = false;

    // Must be called once before using the PopupManager to provide necessary UI Toolkit references.
    public void Initialize(UIDocument document, VisualTreeAsset popupTemplate, VisualTreeAsset yesNoPopupTemplate)
    {
        if (isInitialized) {
            Debug.LogWarning("PopupManager is already initialized. Reinitialization is not recommended.");
            return;
        } else {
            _mainUIDocument = document;
            _defaultPopupTemplate = popupTemplate;
            _yesNoPopupTemplate = yesNoPopupTemplate;
            isInitialized = true;
        }
    }

    // Spawns a popup based on the default template.
    public void ShowPopup(string titleText, string actionText, Action onCancel = null, Action<string> onAction = null, bool includeInputField = true)
    {
        if (!isInitialized)
        {
            Debug.LogError("PopupManager is not initialized! Call PopupManager.Instance.Initialize(...) first.");
            return;
        }

        if (_defaultPopupTemplate == null || _mainUIDocument == null || _yesNoPopupTemplate == null)
        {
            Debug.LogError("PopupManager is not properly initialized! Ensure both UIDocument and Popup Template are assigned.");
            return;
        }

        // Clean up any existing popups just in case
        ClosePopup();
        Debug.Log("Spawning popup with title: " + titleText);

        // Locate template and clone
        _currentPopupInstance = includeInputField ? _defaultPopupTemplate.Instantiate() : _yesNoPopupTemplate.Instantiate();

        // Moving it over entire screen
        _currentPopupInstance.style.position = Position.Absolute;
        _currentPopupInstance.style.top = 0;
        _currentPopupInstance.style.bottom = 0;
        _currentPopupInstance.style.left = 0;
        _currentPopupInstance.style.right = 0;
        _currentPopupInstance.style.width = new StyleLength(Length.Percent(100));
        _currentPopupInstance.style.height = new StyleLength(Length.Percent(100));

        // Fetching elements inside the newly created popup
        Label titleLabel = _currentPopupInstance.Q<Label>("Title");
        TextField inputField = _currentPopupInstance.Q<TextField>();
        Button actioBtn = _currentPopupInstance.Q<Button>("ActionBtn");
        Button cancelBtn = _currentPopupInstance.Q<Button>("CancelBtn");
        Label actionBtnLabel = actioBtn?.Q<Label>();
        Label cancelBtnLabel = cancelBtn?.Q<Label>();

        // Applying Changes
        if (titleLabel != null) titleLabel.text = titleText;
        if (actionBtnLabel != null) actionBtnLabel.text = actionText;

        // Injecting Actions and Close Logic
        actioBtn?.RegisterCallback<ClickEvent>(e => 
        {
            string inputText = inputField != null ? inputField.value : "";
            onAction?.Invoke(inputText);
            ClosePopup();
        });

        cancelBtn?.RegisterCallback<ClickEvent>(e => 
        {
            onCancel?.Invoke();
            ClosePopup();
        });

        // Adding to the main UI root
        _mainUIDocument.rootVisualElement.Add(_currentPopupInstance);
        UiScaler.ScaleTextElements(_currentPopupInstance.Query<TextElement>().ToList());
    }

    public void ClosePopup()
    {
        if (_currentPopupInstance != null && _mainUIDocument != null && _mainUIDocument.rootVisualElement.Contains(_currentPopupInstance))
        {
            _mainUIDocument.rootVisualElement.Remove(_currentPopupInstance);
            _currentPopupInstance = null;
        }
    }
}