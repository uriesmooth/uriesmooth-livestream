using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AlphaRenderEnforcer : MonoBehaviour
{
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        
        // Force the camera background to completely transparent alpha layers
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        
        // Ensure standard project builds accept background process presentation
        Application.runInBackground = true;
    }
}
