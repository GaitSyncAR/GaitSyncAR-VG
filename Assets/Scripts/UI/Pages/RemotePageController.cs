using UnityEngine;
using UnityEngine.UIElements;
using System;

public class RemotePageController : PageController
{
    private MetronomeController _metronome;
    private Label        _bpmLabel;
    private Button       _startStopBtn;

    private int _currentBpm;

    public new void Initialize(UIDocument doc, MetronomeController metronome)
    {
        base.Initialize(doc);
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
        _metronome.bpm = Mathf.Max(0, _metronome.bpm + amount);
        ProfileManager.Instance.currentProfile.bpm = _metronome.bpm;

        _currentBpm = (int)_metronome.bpm;
        UIEventBus.EmitBPM(_currentBpm);
        PlayHaptic();
    }

    private void ToggleStartStop()
    {
        _metronome.isRunning = !_metronome.isRunning;
        UIEventBus.EmitRunning(_metronome.isRunning);
        PlayHaptic();
    }

    // ── Parameter type matches the event ──
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
