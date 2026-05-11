using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bootstrap скрипт для инициализации необходимых синглтонов при старте игры.
/// Должен быть добавлен на объект в сцене или создан автоматически.
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
        EnsurePersistentSingleton(PassengerRegistry.Instance, "[PassengerRegistry]");
        EnsurePersistentSingleton(CouplesManager.Instance, "[CouplesManager]");
        EnsurePersistentSingleton(FieldEffectSystem.Instance, "[FieldEffectSystem]");
        EnsurePersistentSceneComponent<ClickDirectionManager>("ClickDirectionManager");
        EnsurePersistentSceneComponent<ManualPairingManager>("ManualPairingManager");
        EnsureEventSystem();
    }

    private void Awake()
    {
        EnsureRuntimeServices();
    }

    private static T EnsurePersistentSingleton<T>(T instance, string objectName) where T : Component
    {
        if (instance != null)
            return instance;

        T existing = Object.FindObjectOfType<T>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.transform.root.gameObject);
            return existing;
        }

        GameObject serviceObject = new GameObject(objectName);
        T service = serviceObject.AddComponent<T>();
        DontDestroyOnLoad(serviceObject);
        return service;
    }

    private static T EnsurePersistentSceneComponent<T>(string objectName) where T : Component
    {
        T existing = Object.FindObjectOfType<T>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.transform.root.gameObject);
            return existing;
        }

        GameObject serviceObject = new GameObject(objectName);
        T service = serviceObject.AddComponent<T>();
        DontDestroyOnLoad(serviceObject);
        return service;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    /// <summary>
    /// Вызывается при смене сцены для очистки кешей
    /// </summary>
    public static void OnSceneChange()
    {
        Couple.ClearCache();
        PassengerRegistry.Instance?.CleanupNullReferences();
    }
}
