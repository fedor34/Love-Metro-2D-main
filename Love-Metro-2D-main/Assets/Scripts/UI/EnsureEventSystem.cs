using UnityEngine;
using UnityEngine.UI;

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

        // Ensure canvases have GraphicRaycaster
        var canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.GetComponent<GraphicRaycaster>() == null)
            {
                c.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        // Cursor defaults for menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;
    }
}

