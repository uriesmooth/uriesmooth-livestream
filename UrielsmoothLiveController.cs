using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

[Serializable]
public class Landmark { public float x; public float y; public float z; public float v; }

[Serializable]
public class TrackingPacket
{
    public Landmark[] pose;
    public Landmark[] left_hand;
    public Landmark[] right_hand;
}

public class UrielsmoothLiveController : MonoBehaviour
{
    [Header("Process Pipe Setup")]
    public string pythonExecutableName = "python.exe";

    [Header("Direct Skinned Mesh Assignment")]
    public SkinnedMeshRenderer faceMeshRenderer;
    public int eyeBlinkLeftIndex = 0;
    public int eyeBlinkRightIndex = 1;

    [Header("Direct Bone Linkage Assignments")]
    public Transform leftUpperArm;
    public Transform rightUpperArm;
    public Transform leftHandWrist;
    public Transform rightHandWrist;

    [Header("Direct Finger Joint Arrays (Proximal Links Only)")]
    public Transform[] leftFingers = new Transform[5]; // Thumb, Index, Middle, Ring, Little
    public Transform[] rightFingers = new Transform[5];

    [Header("Strict Filtering Constraints")]
    public float bodySmoothing = 0.30f;
    public float handPrioritySmoothing = 0.12f;
    private const float deadZoneThreshold = 0.005f;
    private const float maxVelocityClamp = 4.5f;

    // Background RAM Pipes Processing Layer
    private Process pythonProcess;
    private Thread pipeReadThread;
    private readonly object dataLock = new object();
    private string latestTrackingData = "";

    void Start()
    {
        // Enforce hard-capped application execution intervals
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        InitializeNetworklessPipe();
    }

    void InitializeNetworklessPipe()
    {
        pipeReadThread = new Thread(ReadFromProcessPipe);
        pipeReadThread.IsBackground = true;
        pipeReadThread.Start();
    }

    private void ReadFromProcessPipe()
    {
        try
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = pythonExecutableName;
            
            string scriptPath = Path.Combine(Application.streamingAssetsPath, "mediapipe_pipe.py");
            pythonProcess.StartInfo.Arguments = $"\"{scriptPath}\"";
            
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.RedirectStandardOutput = true;
            pythonProcess.StartInfo.RedirectStandardError = true;
            pythonProcess.StartInfo.CreateNoWindow = true;

            pythonProcess.Start();

            using (StreamReader reader = pythonProcess.StandardOutput)
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        lock (dataLock)
                        {
                            latestTrackingData = line;
                        }
                    }
                }
            }
        }
        catch (Exception) { /* Protect thread execution loops */ }
    }

    void Update()
    {
        string rawJson;
        lock (dataLock)
        {
            rawJson = latestTrackingData;
        }

        if (string.IsNullOrEmpty(rawJson)) return;

        try
        {
            TrackingPacket packet = JsonUtility.FromJson<TrackingPacket>(rawJson);
            
            // Core Upper Body Processing Pass
            if (packet.pose != null && packet.pose.Length > 12) 
            {
                if (leftUpperArm)
                {
                    Vector3 tDir = new Vector3(-packet.pose[11].x + packet.pose[13].x, -packet.pose[11].y + packet.pose[13].y, packet.pose[11].z - packet.pose[13].z).normalized;
                    leftUpperArm.rotation = Quaternion.Slerp(leftUpperArm.rotation, Quaternion.LookRotation(ApplyAntiDrift(leftUpperArm.position, tDir)), bodySmoothing);
                }
                if (faceMeshRenderer) ProcessFacialBlinks(packet.pose);
            }
            
            // Core Priority Hand Processing Pass
            if (packet.left_hand != null && packet.left_hand.Length > 0 && leftHandWrist) ProcessPriorityHand(packet.left_hand, leftHandWrist, leftFingers);
            if (packet.right_hand != null && packet.right_hand.Length > 0 && rightHandWrist) ProcessPriorityHand(packet.right_hand, rightHandWrist, rightFingers);
        }
        catch (Exception) { /* Protect primary game thread execution engine loops */ }
    }

    void ProcessPriorityHand(Landmark[] handPoints, Transform handWrist, Transform[] fingers)
    {
        Vector3 wristPos = new Vector3(-handPoints[0].x, -handPoints[0].y, handPoints[0].z);
        Vector3 indexBase = new Vector3(-handPoints[5].x, -handPoints[5].y, handPoints[5].z);
        Vector3 handDirection = ApplyAntiDrift(handWrist.position, (indexBase - wristPos).normalized);

        handWrist.rotation = Quaternion.Slerp(handWrist.rotation, Quaternion.LookRotation(handDirection), handPrioritySmoothing);

        for (int i = 0; i < 5; i++)
        {
            if (fingers[i] == null) continue;
            int startIdx = 1 + (i * 4);
            Vector3 baseSegment = new Vector3(-handPoints[startIdx].x, -handPoints[startIdx].y, handPoints[startIdx].z);
            Vector3 tipSegment  = new Vector3(-handPoints[startIdx + 3].x, -handPoints[startIdx + 3].y, handPoints[startIdx + 3].z);
            
            float distance = Vector3.Distance(baseSegment, tipSegment);
            float rotationAngle = Mathf.Clamp(distance * 92f, 0f, 85f);

            fingers[i].localRotation = Quaternion.Slerp(fingers[i].localRotation, Quaternion.Euler(rotationAngle, 0, 0), handPrioritySmoothing);
        }
    }

    void ProcessFacialBlinks(Landmark[] pose)
    {
        float targetLeftBlink = (pose[2].v * 100f < 35f) ? 100f : 0f;
        float targetRightBlink = (pose[5].v * 100f < 35f) ? 100f : 0f;

        faceMeshRenderer.SetBlendShapeWeight(eyeBlinkLeftIndex, Mathf.Lerp(faceMeshRenderer.GetBlendShapeWeight(eyeBlinkLeftIndex), targetLeftBlink, bodySmoothing));
        faceMeshRenderer.SetBlendShapeWeight(eyeBlinkRightIndex, Mathf.Lerp(faceMeshRenderer.GetBlendShapeWeight(eyeBlinkRightIndex), targetRightBlink, bodySmoothing));
    }

    Vector3 ApplyAntiDrift(Vector3 currentPosition, Vector3 targetPosition)
    {
        float deltaDistance = Vector3.Distance(currentPosition, targetPosition);
        if (deltaDistance < deadZoneThreshold) return currentPosition;
        if (deltaDistance > maxVelocityClamp)
        {
            return currentPosition + ((targetPosition - currentPosition).normalized * maxVelocityClamp);
        }
        return targetPosition;
    }

    void OnApplicationQuit()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.Dispose();
        }
        if (pipeReadThread != null && pipeReadThread.IsAlive) pipeReadThread.Abort();
    }
}
