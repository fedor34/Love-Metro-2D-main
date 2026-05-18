using UnityEngine;
using UnityEngine.SceneManagement;

public static class ParallaxBootstrap
{
    private static bool _subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (_subscribed)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        _subscribed = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeToSceneLoads()
    {
        if (_subscribed)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        _subscribed = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureParallaxRuntime()
    {
        LoveMetro.Core.RuntimeCompositionRoot.BindActiveScene(LoveMetro.Core.RuntimeCompositionOptions.GameplayDefaults);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureParallaxRuntime();
    }
}
