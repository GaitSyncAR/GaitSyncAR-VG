using UnityEngine;

public class FlawlessGaitUI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform cameraTransform;

    [Header("Dampening (The Elastic String)")]
    [Range(1f, 15f)]
    public float catchUpSpeed = 5.0f; // Keep this around 5 for smooth gait dampening!
    
    [Header("FOV Limits")]
    public float maxAngle = 18f; // Try 18-20 for Viture glasses
    public bool lockHorizon = true; 

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        //anchroing obj to head
        transform.position = cameraTransform.position;

        // Flatten physical head direction to the horizon
        Vector3 targetForward = cameraTransform.forward;
        
        if (lockHorizon)
        {
            targetForward.y = 0; // Flatten the vector to the horizon
            
            // Preventing math errors if looking perfectly straight down
            if (targetForward.sqrMagnitude > 0.001f) 
            {
                targetForward.Normalize();
            }
            else 
            {
                targetForward = transform.forward; 
            }
        }
        
        // Convert the flattened direction back into a clean rotation
        Quaternion targetRotation = Quaternion.LookRotation(targetForward);

        // Calculating the drift
        float currentAngle = Quaternion.Angle(transform.rotation, targetRotation);

        // Applying snap and follow
        if (currentAngle > 170f)
        {
            // If it ends up behind you, snap instantly.
            transform.rotation = targetRotation;
        }
        else if (currentAngle > maxAngle)
        {
            // Sliding perfectly along the edge of the FOV
            float t = 1.0f - (maxAngle / currentAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
        else
        {
            // Elastic pull toward the center
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * catchUpSpeed);
        }
    }
}