using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AlphaCaptureSetup : MonoBehaviour
{
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        
        // Force background rendering settings to output alpha 0 layers
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); 
        
        // Allow structural capture pass transformations
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30; // Caps engine overhead to give resource room to voice cloning
    }
}
