using UnityEngine;
using System;

[Serializable]
public class Landmark { public float x; public float y; public float z; public float v; }

[Serializable]
public class TrackingPacket
{
    public Landmark[] pose;
    public Landmark[] left_hand;
    public Landmark[] right_hand;
}

public class HolisticLandmarkProcessor : MonoBehaviour
{
    public MediaPipeReceiver receiver;
    public Animator avatarAnimator;

    // Bone transforms for retargeting mapping
    private Transform leftShoulder, leftUpperArm, leftLowerArm, leftHand;
    private Transform rightShoulder, rightUpperArm, rightLowerArm, rightHand;
    private Transform[] leftFingers = new Transform[5];
    private Transform[] rightFingers = new Transform[5];

    [Range(0.01f, 0.9f)] public float bodySmoothing = 0.3f;
    [Range(0.01f, 0.9f)] public float handPrioritySmoothing = 0.15f; // Lower values mean sharper, faster hand response

    void Start()
    {
        if (!avatarAnimator) avatarAnimator = GetComponent<Animator>();
        MapHumanoidBones();
    }

    void MapHumanoidBones()
    {
        // Core tracking joints mapping
        leftShoulder = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        leftUpperArm = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        leftLowerArm = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        leftHand = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftHand);

        rightShoulder = avatarAnimator.GetBoneTransform(HumanBodyBones.RightShoulder);
        rightUpperArm = avatarAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLowerArm = avatarAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rightHand = avatarAnimator.GetBoneTransform(HumanBodyBones.RightHand);

        // Distribute finger references for priority tracking
        leftFingers[0] = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
        leftFingers[1] = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
        leftFingers[2] = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
        leftFingers[3] = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftRingProximal);
        leftFingers[4] = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);

        rightFingers[0] = avatarAnimator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
        rightFingers[1] = avatarAnimator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        rightFingers[2] = avatarAnimator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        rightFingers[3] = avatarAnimator.GetBoneTransform(HumanBodyBones.RightRingProximal);
        rightFingers[4] = avatarAnimator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
    }

    void Update()
    {
        string rawData = receiver.GetLatestPacket();
        if (string.IsNullOrEmpty(rawData)) return;

        try
        {
            TrackingPacket packet = JsonUtility.FromJson<TrackingPacket>(rawData);
            
            if (packet.pose != null && packet.pose.Length > 0) ProcessBodyPose(packet.pose);
            if (packet.left_hand != null && packet.left_hand.Length > 0) ProcessHand(packet.left_hand, leftHand, leftFingers, true);
            if (packet.right_hand != null && packet.right_hand.Length > 0) ProcessHand(packet.right_hand, rightHand, rightFingers, false);
        }
        catch (Exception) { /* Fail-silent to prevent runtime frame stalls */ }
    }

    void ProcessBodyPose(Landmark[] pose)
    {
        // Invert X-axis coordinates for mirror-matching stream output
        Vector3 targetLeftArmDir = new Vector3(-pose[11].x + pose[13].x, -pose[11].y + pose[13].y, pose[11].z - pose[13].z).normalized;
        if (leftUpperArm)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetLeftArmDir);
            leftUpperArm.rotation = Quaternion.Slerp(leftUpperArm.rotation, targetRot, bodySmoothing);
        }
    }

    void ProcessHand(Landmark[] handPoints, Transform handWrist, Transform[] fingers, bool isLeft)
    {
        // High-Priority gesture calculation
        Vector3 wristPos = new Vector3(-handPoints[0].x, -handPoints[0].y, handPoints[0].z);
        Vector3 indexBase = new Vector3(-handPoints[5].x, -handPoints[5].y, handPoints[5].z);
        Vector3 handDirection = (indexBase - wristPos).normalized;

        if (handWrist)
        {
            Quaternion targetHandRot = Quaternion.LookRotation(handDirection);
            handWrist.rotation = Quaternion.Slerp(handWrist.rotation, targetHandRot, handPrioritySmoothing);
        }

        // Fast finger curling retargeting mapped to standard tracking index steps
        for (int i = 0; i < fingers.Length; i++)
        {
            if (fingers[i] == null) continue;
            int startIdx = 1 + (i * 4);
            Vector3 basePt = new Vector3(-handPoints[startIdx].x, -handPoints[startIdx].y, handPoints[startIdx].z);
            Vector3 tipPt = new Vector3(-handPoints[startIdx + 3].x, -handPoints[startIdx + 3].y, handPoints[startIdx + 3].z);
            
            float curlDistance = Vector3.Distance(basePt, tipPt);
            float angle = Mathf.Clamp(curlDistance * 90f, 0f, 85f); // Prevent unnatural hyper-extensions

            fingers[i].localRotation = Quaternion.Slerp(fingers[i].localRotation, Quaternion.Euler(angle, 0, 0), handPrioritySmoothing);
        }
    }
}
