using UnityEngine;

public static class ParallaxBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureParallaxRuntime()
    {
        LoveMetro.Core.RuntimeCompositionRoot.BindActiveScene(LoveMetro.Core.RuntimeCompositionOptions.GameplayDefaults);
    }
}
