using UnityEngine;
using UnityEngine.UIElements;
using System;

public class RemotePageController : PageController
{
    private MetronomeController _metronome;
    private Label        _bpmLabel;
    private Button       _startStopBtn;

    private int _currentBpm;

    public RemotePageController(UIDocument doc, VisualElement pageRoot, MetronomeController metronome) : base(doc, pageRoot)
    {
        _metronome = metronome;

        _bpmLabel     = Q<Label>("BPM-lbl");
        _startStopBtn = Q<Button>("StartStop");

        Q<Button>("increase").clicked += () => ChangeBPM(5);
        Q<Button>("decrease").clicked += () => ChangeBPM(-5);
        _startStopBtn.clicked         += ToggleStartStop;

        UIEventBus.BPMChanged              += UpdateBPMDisplay;
        UIEventBus.MetronomeRunningChanged += RefreshStartStopButton;

        _currentBpm = (int)_metronome.bpm;
        UpdateBPMDisplay(_currentBpm);
    }

    public override void OnPageHide()
    {
        ProfileManager.Instance.currentProfile.bpm = _metronome.bpm;
        Debug.Log($"[RemotePageController] Saving BPM: {_metronome.bpm} at profile: {ProfileManager.Instance.currentProfile.profileName}");
        ProfileManager.Instance.SaveProfile();
        PlayerPrefs.SetString("CurrentProfile", ProfileManager.Instance.currentProfile.profileName);
        PlayerPrefs.Save();
    }

    private void ChangeBPM(int amount)
    {
        _metronome.bpm = Math.Clamp(_metronome.bpm + amount, 0, 255f);
        ProfileManager.Instance.currentProfile.bpm = _metronome.bpm;

        _currentBpm = (int)_metronome.bpm;
        UIEventBus.EmitBPM(_currentBpm);
        PlayHaptic();
    }

    private void ToggleStartStop()
    {
        // check if either both sensors are connected
        // we don't want to start the session if only one sensor is connected
        if (!BLEManager.Instance.allConnected && _metronome.isRunning == false)
        {
            PopupManager.Instance.ShowPopup(titleText: "Please Connect Both Sensors.", 
            actionText: "Start Anyways", 
            includeInputField: false, 
            onAction: awnser =>
            {
                Debug.Log($"[RemotePageController] Popup result: {awnser}");
                _metronome.isRunning = !_metronome.isRunning;
                UIEventBus.EmitRunning(_metronome.isRunning);
                PlayHaptic();
            });
            Debug.LogWarning("Cannot start metronome: Only one sensor is connected.");
            return;
        }
        else
        {
            _metronome.isRunning = !_metronome.isRunning;
            UIEventBus.EmitRunning(_metronome.isRunning);
            PlayHaptic();
        }
    }

    private void UpdateBPMDisplay(int bpm)
    {
        if (_bpmLabel != null) _bpmLabel.text = bpm.ToString();
    }

    private void RefreshStartStopButton(bool running)
    {
        if (_startStopBtn == null) return;

        if (running)
        {
            _startStopBtn.style.backgroundColor = new StyleColor(new Color(0f, 0.89f, 1f));
            _startStopBtn.text                   = "■ STOP";
            _startStopBtn.style.color            = Color.black;
        }
        else
        {
            _startStopBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            _startStopBtn.text                  = "▶ START";
            _startStopBtn.style.color           = Color.white;
        }
    }
}
