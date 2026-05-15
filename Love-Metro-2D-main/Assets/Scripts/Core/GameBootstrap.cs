using UnityEngine;

/// <summary>
/// Compatibility facade for runtime service initialization.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSingletons()
    {
        EnsureRuntimeServices();
    }

    public static void EnsureRuntimeServices()
    {
        LoveMetro.Core.RuntimeCompositionRoot.BindActiveScene(LoveMetro.Core.RuntimeCompositionOptions.CoreDefaults);
    }

    private void Awake()
    {
        EnsureRuntimeServices();
    }

    public static void OnSceneChange()
    {
        LoveMetro.Core.RuntimeCompositionRoot.CleanupAfterSceneChange();
        LoveMetro.Core.RuntimeCompositionRoot.BindActiveScene(LoveMetro.Core.RuntimeCompositionOptions.CoreDefaults);
    }
}
