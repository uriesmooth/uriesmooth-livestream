using UnityEngine;

public class TrackingDiagnostics : MonoBehaviour
{
    public MediaPipeReceiver receiver;
    private float frameCounter = 0;
    private float timeCounter = 0;
    private float currentFps = 0;
    private string connectionStatus = "Disconnected";

    void Update()
    {
        // 1. Calculate engine frame rate
        frameCounter++;
        timeCounter += Time.unscaledDeltaTime;
        if (timeCounter >= 1.0f)
        {
            currentFps = frameCounter;
            frameCounter = 0;
            timeCounter = 0f;
        }

        // 2. Evaluate incoming packet stability
        if (receiver != null && !string.IsNullOrEmpty(receiver.GetLatestPacket()))
        {
            connectionStatus = "Connected (Healthy)";
        }
        else
        {
            connectionStatus = "Waiting for Python Bridge...";
        }
    }

    void OnGUI()
    {
        // Draw a minimalist, clean heads-up diagnostic layout for the streamer
        GUI.Box(new Rect(10, 10, 260, 85), "Urielsmooth-livestream Core Status");
        GUI.Label(new Rect(20, 30, 240, 20), $"Engine Target: {currentFps} FPS");
        GUI.Label(new Rect(20, 50, 240, 20), $"Data Pipeline: {connectionStatus}");
        
        // VRAM warning thresholds for GTX 1650 (4GB total limit)
        long allocatedVRAM = SystemInfo.graphicsMemorySize;
        GUI.Label(new Rect(20, 70, 240, 20), $"Hardware VRAM Available: {allocatedVRAM} MB");
    }
}
