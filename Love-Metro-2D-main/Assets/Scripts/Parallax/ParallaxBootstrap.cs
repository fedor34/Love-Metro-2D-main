using UnityEngine;

public static class ParallaxBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureParallaxRuntime()
    {
        // Гарантируем наличие GameInitializer, который создаёт все нужные системы.
        if (Object.FindObjectOfType<GameInitializer>() == null)
        {
            new GameObject("GameInitializer", typeof(GameInitializer));
            Debug.Log("[ParallaxBootstrap] GameInitializer created at runtime");
        }
    }
}