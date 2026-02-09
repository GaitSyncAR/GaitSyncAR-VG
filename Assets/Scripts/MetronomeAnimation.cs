using UnityEngine;

public class MetronomeArm : MonoBehaviour
{
    [Header("Metronome Settings")]
    [Tooltip("Beats per minute")]
    public float bpm = 120f;

    [Header("IsRunning")]
    public bool isRunning = false;

    [Tooltip("Maximum angle (degrees) left/right from center")]
    public float maxAngle = 30f;

    [Header("Audio Settings")]
    [Tooltip("The ticking sound clip")]
    public AudioClip tickSound;
    
    [Tooltip("Volume of the tick")]
    [Range(0f, 1f)]
    public float tickVolume = 1.0f;

    private AudioSource audioSource;
    private float phase = 0f;
    private int lastHalfCycle = 0;

    private void Start()
    {
        // init audio source
        audioSource = GetComponent<AudioSource>();
    }

    private void LateUpdate()
    {
        if (!isRunning) return;
        
        // Beats per second
        float bps = bpm / 60f;
        float angularFrequency = bps * Mathf.PI;

        // Advancing phase smoothly
        phase += Time.deltaTime * angularFrequency;

        float angle = maxAngle * Mathf.Sin(phase);

        // Rotate around local Z axis (change axis if your model needs X or Y)
        transform.localRotation = Quaternion.Euler(angle, 0f, 0f);

        int currentHalfCycle = (int)((phase + Mathf.PI / 2f) / Mathf.PI);
        if (currentHalfCycle > lastHalfCycle)
        {
            PlayTick();
            lastHalfCycle = currentHalfCycle;
        }
    }

    private void PlayTick()
    {
        if (tickSound != null)
        {
            audioSource.PlayOneShot(tickSound, tickVolume);
        }
    }
}
