using UnityEngine;

public class MetronomeArm : MonoBehaviour
{
    [Header("Metronome Settings")]
    [Tooltip("Beats per minute")]
    public float bpm = 120f;

    [Tooltip("Maximum angle (degrees) left/right from center")]
    public float maxAngle = 30f;
    private float phase = 0f;

    private void LateUpdate()
    {
        // Beats per second
        float bps = bpm / 60f;

        // Advancing phase smoothly
        phase += Time.deltaTime * bps * 2f * Mathf.PI;

        float angle = maxAngle * Mathf.Sin(phase);

        // Rotate around local Z axis (change axis if your model needs X or Y)
        transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
