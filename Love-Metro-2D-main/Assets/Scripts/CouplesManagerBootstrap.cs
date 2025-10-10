using UnityEngine;

public static class CouplesManagerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureManager()
    {
        if (CouplesManager.Instance == null)
        {
            var go = new GameObject("CouplesManager");
            go.AddComponent<CouplesManager>();
            Object.DontDestroyOnLoad(go);
        }
    }
}

