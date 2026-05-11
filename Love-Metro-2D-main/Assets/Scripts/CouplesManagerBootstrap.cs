using UnityEngine;

public static class CouplesManagerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureManager()
    {
        GameBootstrap.EnsureRuntimeServices();
    }
}

