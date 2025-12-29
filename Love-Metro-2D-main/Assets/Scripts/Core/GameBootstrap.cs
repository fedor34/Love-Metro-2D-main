using UnityEngine;

/// <summary>
/// Bootstrap скрипт для инициализации необходимых синглтонов при старте игры.
/// Должен быть добавлен на объект в сцене или создан автоматически.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSingletons()
    {
        // Создаём PassengerRegistry если его ещё нет
        if (PassengerRegistry.Instance == null)
        {
            var registryObj = new GameObject("[PassengerRegistry]");
            registryObj.AddComponent<PassengerRegistry>();
            DontDestroyOnLoad(registryObj);
        }
    }

    private void Awake()
    {
        // Убеждаемся что PassengerRegistry существует
        EnsurePassengerRegistry();
    }

    private void EnsurePassengerRegistry()
    {
        if (PassengerRegistry.Instance == null)
        {
            var registryObj = new GameObject("[PassengerRegistry]");
            registryObj.AddComponent<PassengerRegistry>();
        }
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
