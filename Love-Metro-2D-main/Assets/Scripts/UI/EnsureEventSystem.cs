using UnityEngine;

/// <summary>
/// Guarantees that an EventSystem and at least one GraphicRaycaster exist at runtime.
/// Fixes builds where UI buttons do not react to clicks.
/// </summary>
public class EnsureEventSystem
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Ensure()
    {
        GameBootstrap.EnsureRuntimeServices();

        // Cursor defaults for menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;
    }
}

