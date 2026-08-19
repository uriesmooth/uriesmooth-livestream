[Header("Gesture Amplification Multipliers")]
[Range(1.0f, 2.5f)] public float wristRotationMultiplier = 1.35f;
[Range(1.0f, 3.0f)] public float fingerCurlMultiplier = 1.50f;

void ProcessPriorityHand(Landmark[] handPoints, Transform handWrist, Transform[] fingers)
{
    Vector3 wristPos = new Vector3(-handPoints[0].x, -handPoints[0].y, handPoints[0].z);
    Vector3 indexBase = new Vector3(-handPoints[5].x, -handPoints[5].y, handPoints[5].z);
    Vector3 rawDirection = (indexBase - wristPos).normalized;
    
    // Apply Anti-Drift filter to the raw input vector
    Vector3 filteredDirection = ApplyAntiDrift(handWrist.position, rawDirection);

    // Calculate target rotation and amplify it using the wrist multiplier
    Quaternion targetRotation = Quaternion.LookRotation(filteredDirection);
    Quaternion amplifiedWristRot = Quaternion.Slerp(handWrist.rotation, targetRotation, handPrioritySmoothing * wristRotationMultiplier);
    handWrist.rotation = amplifiedWristRot;

    // Process individual finger segments using absolute distance multipliers
    for (int i = 0; i < 5; i++)
    {
        if (fingers[i] == null) continue;
        int startIdx = 1 + (i * 4);
        Vector3 baseSegment = new Vector3(-handPoints[startIdx].x, -handPoints[startIdx].y, handPoints[startIdx].z);
        Vector3 tipSegment  = new Vector3(-handPoints[startIdx + 3].x, -handPoints[startIdx + 3].y, handPoints[startIdx + 3].z);
        
        float distance = Vector3.Distance(baseSegment, tipSegment);
        
        // Finger Curl Multiplier amplifies distance changes, converting them into quick rotation angles
        float rotationAngle = Mathf.Clamp(distance * 92f * fingerCurlMultiplier, 0f, 85f);

        fingers[i].localRotation = Quaternion.Slerp(fingers[i].localRotation, Quaternion.Euler(rotationAngle, 0, 0), handPrioritySmoothing);
    }
}
