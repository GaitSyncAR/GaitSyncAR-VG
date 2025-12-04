using UnityEngine;

public class MetronomeArm : MonoBehaviour
{
    [Header("Metronome Settings")]
    [Tooltip("Beats per minute")]
    public float bpm = 120f;

    [Tooltip("Maximum angle (degrees) left/right from center")]
    public float maxAngle = 30f;

    private float startTime;

    private void Start()
    {
        startTime = Time.time;
    }

    private void Update()
    {
        // Beats per second
        float bps = bpm / 60f;

        // Time since start in seconds
        float t = Time.time - startTime;

        // Use a sine wave to swing the arm
        float phase = t * bps * 2f * Mathf.PI;  // full sine cycle per beat
        float angle = maxAngle * Mathf.Sin(phase);

        // Rotate around local Z axis (change axis if your model needs X or Y)
        transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
