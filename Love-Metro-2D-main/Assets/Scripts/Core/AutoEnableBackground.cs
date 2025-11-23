using UnityEngine;

/// <summary>
/// Автоматически включает все слои фона при старте игры
/// </summary>
public class AutoEnableBackground : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnableBackgroundLayers()
    {
        Debug.Log("[AutoEnableBackground] Поиск и включение фоновых слоев...");
        
        // Ищем объект Background
        GameObject background = GameObject.Find("Background");
        
        if (background == null)
        {
            Debug.LogWarning("[AutoEnableBackground] Объект 'Background' не найден!");
            return;
        }
        
        // Включаем сам Background
        if (!background.activeSelf)
        {
            background.SetActive(true);
            Debug.Log("[AutoEnableBackground] ✓ Background включен");
        }
        
        // Включаем все дочерние объекты
        int enabledCount = 0;
        foreach (Transform child in background.transform)
        {
            if (!child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
                enabledCount++;
                Debug.Log($"[AutoEnableBackground] ✓ Включен слой: {child.name}");
            }
        }
        
        Debug.Log($"[AutoEnableBackground] ========================================");
        Debug.Log($"[AutoEnableBackground] Всего включено слоёв: {enabledCount}");
        Debug.Log($"[AutoEnableBackground] ========================================");
    }
}



