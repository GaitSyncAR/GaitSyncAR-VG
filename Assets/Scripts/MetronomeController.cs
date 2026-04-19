using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MetronomeController : MonoBehaviour
{
    [Header("Metronome Parts")]
    public Transform metronomeBar;
    public Transform metronomeArm;
    public Transform Metronome => transform;
    public Material  targetMaterial;

    [Header("Metronome Settings")]
    [Tooltip("Beats per minute")]
    public float bpm = 120f;
    public bool  isRunning = false;
    [Tooltip("Maximum angle (degrees) left/right from center")]
    public float maxAngle = 30f;

    [Header("Audio Settings")]
    public AudioClip tickSound;
    [Range(0f, 1f)]
    public float tickVolume = 1.0f;

    // -- Internal State --
    private AudioSource _audioSource;
    private float       _phase = 0f;
    private int         _lastHalfCycle = 0;

    // ----------------------------------------------------------
    // Lifecycle & Events
    // ----------------------------------------------------------
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        // Automatically listen to the UI Event Bus
        UIEventBus.BPMChanged              += HandleBPMChanged;
        UIEventBus.MetronomeRunningChanged += HandleRunningChanged;
        UIEventBus.ColorChanged            += SetColor;
    }

    private void OnDisable()
    {
        // unsubscribes to prevent memory leaks
        UIEventBus.BPMChanged              -= HandleBPMChanged;
        UIEventBus.MetronomeRunningChanged -= HandleRunningChanged;
        UIEventBus.ColorChanged            -= SetColor;
    }

    private void LateUpdate()
    {
        if (!isRunning) return;
        
        float bps = bpm / 60f;
        float angularFrequency = bps * Mathf.PI;

        _phase += Time.deltaTime * angularFrequency;
        float angle = maxAngle * Mathf.Sin(_phase);

        // Rotate around local X axis
        metronomeArm.localRotation = Quaternion.Euler(angle, 0f, 0f);

        int currentHalfCycle = (int)((_phase + Mathf.PI / 2f) / Mathf.PI);
        if (currentHalfCycle > _lastHalfCycle)
        {
            PlayTick();
            _lastHalfCycle = currentHalfCycle;
        }
    }

    private void PlayTick()
    {
        if (tickSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(tickSound, tickVolume);
        }
    }

    // ---------------------------------------------------------- 
    // Public API (For UI & Profiles)
    // ----------------------------------------------------------
    private void HandleBPMChanged(int newBpm)
    {
        bpm = newBpm;
        
        if (isRunning) 
        {
            SendBleSyncPacket();
        }
    }

    private void HandleRunningChanged(bool runningState)
    {
        isRunning = runningState;
        SendBleSyncPacket();
    }

    public void ApplyProfile(UserProfile profile)
    {
        if (profile == null) return;

        bpm                    =    profile.bpm;
        transform.position     =    profile.metronomePosition;
        transform.localScale   =    profile.metronomeSize;
        metronomeBar.localScale = new Vector3(metronomeBar.localScale.x, profile.metronomeBarScaleY, metronomeBar.localScale.z);
        
        SetColor(profile.metronomeColour);
    }

    public void SetColor(Color c)
    {
        if (targetMaterial != null)
        {
            targetMaterial.SetColor("_BaseColor",     c);
            targetMaterial.SetColor("_SpecColor",     c);
            targetMaterial.SetColor("_EmissionColor", c);
        }
    }

    public void ApplyStretch(float stretchAmount)
    {
        if (metronomeBar != null)
        {
            Vector3 barScale = metronomeBar.localScale;
            barScale.y = Mathf.Max(0.1f, barScale.y + stretchAmount);
            metronomeBar.localScale = barScale;
        }
    }

    private Vector3 ClampScale(Vector3 s)
    {
        if (s.x < 0.1f) s = Vector3.one * 0.1f;
        return s;
    }

    public void UniformScale(float amount)
    {
        Vector3 newScale = Metronome.transform.localScale + Vector3.one * amount;
        newScale = ClampScale(newScale);
        Metronome.transform.localScale = newScale;
    }

    public void Move(Vector3 delta)
    {
        transform.position += delta;
    }

    public void SendBleSyncPacket()
    {
        if (BLEManager.Instance == null || !BLEManager.Instance.allConnected) return;

        if (!isRunning)
        {
            // Send STOP command
            BLEManager.Instance.SendMetronomeSync(false, (int)bpm, 0, false);
            return;
        }

        // Offset Calculations
        float msPerBeat = 60000f / bpm;
        float phaseFraction = Mathf.Repeat(_phase + Mathf.PI / 2f, Mathf.PI) / Mathf.PI;
        int phaseOffsetMs = Mathf.RoundToInt(phaseFraction * msPerBeat);

        // logic triggers a tick when currentHalfCycle increments.
        // Even half-cycles (0, 2, 4) mean the arm is swinging RIGHT.
        // Odd half-cycles (1, 3, 5) mean the arm is swinging LEFT.
        int currentHalfCycle = (int)((_phase + Mathf.PI / 2f) / Mathf.PI);
        bool isNextBeatRight = !(currentHalfCycle % 2 == 0);

        // Send PLAY command with the exact offset
        BLEManager.Instance.SendMetronomeSync(true, (int)bpm, phaseOffsetMs, isNextBeatRight);
    }
}