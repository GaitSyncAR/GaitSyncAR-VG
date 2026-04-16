// TemplatesPageController.cs
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class TemplatesPageController : PageController
{
    // ── UI element references ──
    private ScrollView      _scrollView;
    private string          _selectedName = "";
    private Label           _selectedTitle;

    // ── Template assets (passed in at init) ──
    private VisualTreeAsset _rowTemplate;
    private VisualTreeAsset _popupTemplate;
    private VisualTreeAsset _yesNoPopupTemplate;

    // ══════════════════════════════════════════════════════════
    //  Initialisation
    // ══════════════════════════════════════════════════════════

    public new void Initialize(
        UIDocument        doc,
        VisualTreeAsset   rowTemplate,
        VisualTreeAsset   popupTemplate,
        VisualTreeAsset   yesNoPopupTemplate)
    {
        base.Initialize(doc);

        _rowTemplate        = rowTemplate;
        _popupTemplate      = popupTemplate;
        _yesNoPopupTemplate = yesNoPopupTemplate;

        _scrollView   = Q<ScrollView>("ScrollView");
        _selectedTitle = Q<Label>("SelectedTitle");

        // ── Wire buttons ──
        Q<Button>("SaveCurrentSettingsBtn").clicked += OnSaveClicked;
        Q<Button>("ApplySettingsBtn").clicked       += OnApplyClicked;
        Q<Button>("RenameBtn").clicked               += OnRenameClicked;
        Q<Button>("ResetBtn").clicked                += OnResetClicked;
        Q<Button>("DeleteBtn").clicked               += OnDeleteClicked;

        // ── Listen for profile changes from other pages ──
        UIEventBus.ProfileListChanged += PopulateProfileList;
        UIEventBus.ProfileApplied    += OnExternalProfileApplied;

        // ── Initial population ──
        PopulateProfileList();
    }

    // ══════════════════════════════════════════════════════════
    //  Profile List Population
    // ══════════════════════════════════════════════════════════
    public void PopulateProfileList()
    {
        if (_scrollView == null) return;

        _scrollView.Clear();

        List<string> savedProfiles = ProfileManager.Instance.GetAvailableProfiles();

        foreach (string profileName in savedProfiles)
        {
            VisualElement row = _rowTemplate.Instantiate();
            Button      btn  = row.Q<Button>("SavedTemplateBtn");

            if (btn != null)
            {
                btn.text = profileName;

                // Safely check if currentProfile exists before reading its name
                var current = ProfileManager.Instance.currentProfile;
                if (current != null && profileName == current.profileName)
                {
                    btn.AddToClassList("selected");
                }

                btn.clicked += () => SelectProfile(profileName);
            }

            _scrollView.Add(row);
        }

        Debug.Log($"[Templates] Loaded {savedProfiles.Count} profiles into UI.");
    }

    // ══════════════════════════════════════════════════════════
    //  Selection
    // ══════════════════════════════════════════════════════════
    public void SelectProfile(string profileName)
    {
        _selectedName = profileName;

        if (_selectedTitle != null)
            _selectedTitle.text = string.IsNullOrEmpty(profileName)
                ? "No Template Selected"
                : $"Selected Template: {profileName}";

        // Remove "selected" class from all row buttons first
        foreach (var btn in _scrollView.Query<Button>("SavedTemplateBtn").ToList())
            btn.RemoveFromClassList("selected");

        // Re-add "selected" class to the matching button
        if (!string.IsNullOrEmpty(profileName))
        {
            foreach (var btn in _scrollView.Query<Button>("SavedTemplateBtn").ToList())
            {
                if (btn.text == profileName)
                {
                    btn.AddToClassList("selected");
                    break;
                }
            }
        }

        PlayHaptic();
    }

    // ══════════════════════════════════════════════════════════
    //  Button Callbacks
    // ══════════════════════════════════════════════════════════

    // ── SAVE ──────────────────────────────────────────────────
    private void OnSaveClicked()
    {
        PopupManager.Instance.ShowPopup(
            titleText:         "Save New Template",
            actionText:        "Save",
            includeInputField: true,
            onCancel:          () => Debug.Log("[Templates] Save cancelled."),
            onAction:          inputName =>
            {
                if (string.IsNullOrWhiteSpace(inputName))
                {
                    Debug.LogWarning("[Templates] Cannot save profile with an empty name.");
                    return;
                }

                ProfileManager.Instance.currentProfile.profileName = inputName.Trim();
                ProfileManager.Instance.SaveProfile();
                UIEventBus.EmitProfileListChanged();
                SelectProfile(inputName.Trim());
                PlayHaptic();

                Debug.Log($"[Templates] New template '{inputName}' saved.");
            }
        );
    }

    // ── APPLY ──────────────────────────────────────────────────
    private void OnApplyClicked()
    {
        if (string.IsNullOrEmpty(_selectedName)) return;

        // Skip if already the current profile
        if (PlayerPrefs.GetString("CurrentProfile") == _selectedName)
        {
            Debug.Log($"[Templates] '{_selectedName}' is already the current profile.");
            return;
        }

        PopupManager.Instance.ShowPopup(
            titleText:         $"Apply '{_selectedName}' Settings?",
            actionText:        "Apply",
            includeInputField: false,
            onCancel:          () => Debug.Log("[Templates] Apply cancelled."),
            onAction:          _ =>
            {
                // 1. Save the current profile before switching
                ProfileManager.Instance.SaveProfile();

                // 2. Mark this as the new current profile
                PlayerPrefs.SetString("CurrentProfile", _selectedName);

                // 3. Load and apply
                ProfileManager.Instance.LoadProfile(_selectedName);

                // 4. Notify all listeners (calibration page, remote page, etc.)
                UIEventBus.EmitProfileApplied(_selectedName);
                UIEventBus.EmitProfileListChanged();

                PlayHaptic();
                Debug.Log($"[Templates] Applied template '{_selectedName}'.");
            }
        );
    }

    // ── RENAME ─────────────────────────────────────────────────
    private void OnRenameClicked()
    {
        if (string.IsNullOrEmpty(_selectedName)) return;

        PopupManager.Instance.ShowPopup(
            titleText:         $"Rename '{_selectedName}'",
            actionText:        "Rename",
            includeInputField: true,
            onCancel:          () => Debug.Log("[Templates] Rename cancelled."),
            onAction:          newName =>
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    Debug.LogWarning("[Templates] Cannot rename to an empty name.");
                    return;
                }

                string trimmed = newName.Trim();

                ProfileManager.Instance.RenameProfile(_selectedName, trimmed);
                UIEventBus.EmitProfileListChanged();
                SelectProfile(trimmed);

                // If we renamed the currently loaded profile, update PlayerPrefs
                if (PlayerPrefs.GetString("CurrentProfile") == _selectedName)
                    PlayerPrefs.SetString("CurrentProfile", trimmed);

                PlayHaptic();
                Debug.Log($"[Templates] Renamed '{_selectedName}' → '{trimmed}'.");
            }
        );
    }

    // ── RESET ──────────────────────────────────────────────────
    private void OnResetClicked()
    {
        PopupManager.Instance.ShowPopup(
            titleText:         "Reset Current Template Settings to Default?",
            actionText:        "Reset",
            includeInputField: false,
            onCancel:          () => Debug.Log("[Templates] Reset cancelled."),
            onAction:          _ =>
            {
                string currentName = ProfileManager.Instance.currentProfile.profileName;

                // Replace current profile with a fresh default (same name)
                ProfileManager.Instance.currentProfile = new UserProfile(currentName);
                ProfileManager.Instance.SaveProfile();

                UIEventBus.EmitProfileListChanged();
                UIEventBus.EmitProfileApplied(currentName);

                PlayHaptic();
                Debug.Log("[Templates] Current template reset to defaults.");
            }
        );
    }

    // ── DELETE ──────────────────────────────────────────────────
    private void OnDeleteClicked()
    {
        if (string.IsNullOrEmpty(_selectedName)) return;

        PopupManager.Instance.ShowPopup(
            titleText:         $"Delete '{_selectedName}'?",
            actionText:        "Delete",
            includeInputField: false,
            onCancel:          () => Debug.Log("[Templates] Delete cancelled."),
            onAction:          _ =>
            {
                // 1Delete the targeted profile
                ProfileManager.Instance.DeleteProfile(_selectedName);
                
                List<string> remainingProfiles = ProfileManager.Instance.GetAvailableProfiles();

                // SAFETY CHECK: Did we just delete the absolute last profile?
                if (remainingProfiles.Count == 0)
                {
                    Debug.Log("[Templates] All profiles deleted. Generating a fallback 'Default' profile.");
                    ProfileManager.Instance.currentProfile = new UserProfile("Default");
                    ProfileManager.Instance.SaveProfile();
                    
                    remainingProfiles.Add("Default");
                }

                // SAFETY CHECK: Did we just delete the currently active profile?
                if (PlayerPrefs.GetString("CurrentProfile") == _selectedName)
                {
                    // Fall back to the first available profile (which will be "Default" if we just generated it)
                    string fallbackName = remainingProfiles[0];
                    Debug.Log($"[Templates] Active profile deleted. Falling back to '{fallbackName}'.");
                    
                    PlayerPrefs.SetString("CurrentProfile", fallbackName);
                    ProfileManager.Instance.LoadProfile(fallbackName);
                    
                    // Force the rest of the app to update to the new fallback profile
                    UIEventBus.EmitProfileApplied(fallbackName);
                }

                // 4. Refresh the UI
                UIEventBus.EmitProfileListChanged();
                SelectProfile("");  // clear selection

                PlayHaptic();
                Debug.Log($"[Templates] Deleted profile '{_selectedName}'.");
            }
        );
    }

    // ══════════════════════════════════════════════════════════
    //  External Events
    // ══════════════════════════════════════════════════════════
    private void OnExternalProfileApplied(string profileName)
    {
        // Refresh the "selected" highlight without re-fetching the list
        foreach (var btn in _scrollView.Query<Button>("SavedTemplateBtn").ToList())
        {
            if (btn.text == profileName)
                btn.AddToClassList("selected");
            else
                btn.RemoveFromClassList("selected");
        }

        _selectedTitle.text = $"Selected Template: {profileName}";
        _selectedName       = profileName;
    }
}
