using UnityEngine;

/// <summary>
/// Ensures that the main orthographic camera is zoomed out to a preset value
/// after every scene load.  This fixes the issue where the camera appears too
/// close in the final build compared to the editor.
/// </summary>
public static class CameraAutoZoom
{
    // Desired orthographic size. Feel free to tweak if you need a different zoom level.
    private const float TargetSize = 6.3124776f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AdjustCamera()
    {
        var cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = TargetSize;
        }
    }
}