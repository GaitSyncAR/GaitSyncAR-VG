using System;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIEventBus
{
    // ── Calibration -> Remote ──
    public static event Action<int> BPMChanged;
    public static event Action<bool>   MetronomeRunningChanged;

    // ── Remote -> Calibration ──
    public static event Action<Color> ColorChanged;

    // ── Invoke helpers ──
    public static void EmitBPM(float value)         => BPMChanged?.Invoke(Mathf.RoundToInt(value));
    public static void EmitRunning(bool running)    => MetronomeRunningChanged?.Invoke(running);
    public static void EmitColor(Color c)           => ColorChanged?.Invoke(c);
    public static void EmitProfileApplied(string n) => ProfileApplied?.Invoke(n);

    public static void EmitProfileListChanged()     => ProfileListChanged?.Invoke();

    // ── Profile list changed (save / delete / rename / reset) ──
    public static event Action ProfileListChanged;

    // ── A profile was applied and loaded ──
    public static event Action<string> ProfileApplied;
}