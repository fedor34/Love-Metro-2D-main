using UnityEngine;

public static class ParallaxBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureParallaxRuntime()
    {
        if (Object.FindObjectOfType<GameInitializer>() == null)
        {
            new GameObject("GameInitializer", typeof(GameInitializer));
            Diagnostics.Log("[ParallaxBootstrap] Created GameInitializer at runtime.");
        }
    }
}
