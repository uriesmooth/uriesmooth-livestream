using UnityEngine;

public class AntiDriftFilter : MonoBehaviour
{
    public static AntiDriftFilter Instance;

    [Header("Jitter Mitigation")]
    public float deadZoneThreshold = 0.005f; // Ignore tiny movements below this threshold (stops shaking)
    
    [Header("Drift Protection")]
    public float maxVelocityClamp = 5.0f; // Max units a joint can move per frame (stops snapping)

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    // Filters and returns the sanitized target position
    public Vector3 FilterPosition(Vector3 currentPos, Vector3 targetPos)
    {
        float distance = Vector3.Distance(currentPos, targetPos);

        // 1. Apply Dead-Zone Filter (Kills micro-jitters)
        if (distance < deadZoneThreshold)
        {
            return currentPos;
        }

        // 2. Apply Velocity Clamp (Prevents sudden tracking explosions)
        if (distance > maxVelocityClamp)
        {
            Vector3 direction = (targetPos - currentPos).normalized;
            targetPos = currentPos + (direction * maxVelocityClamp);
        }

        return targetPos;
    }
}
