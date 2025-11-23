using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Сбрасывает includeLayers/excludeLayers на Rigidbody2D И Collider2D
/// 
/// Проблема: В Unity 2022+ если includeLayers = 0 (пустая маска),
/// физический движок обнаруживает столкновения (OnCollisionEnter2D срабатывает),
/// но НЕ ПРИМЕНЯЕТ физический отклик - объекты проходят сквозь друг друга!
/// 
/// Эта проблема может быть как у Rigidbody2D, так и у Collider2D (например, у границ поля)!
/// 
/// Решение: Сбрасываем includeLayers в -1 (все слои) для всех компонентов
/// </summary>
public class AutoFixRigidbodyLayers : MonoBehaviour
{
    private static bool _sceneHookInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        EnsureSceneHook();
        RunFix("initial scene load");
    }

    private static void EnsureSceneHook()
    {
        if (_sceneHookInitialized) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        _sceneHookInitialized = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RunFix($"scene '{scene.name}' loaded");
    }

    private static void RunFix(string reason)
    {
        Debug.Log("[AutoFixLayers] ========================================");
        Debug.Log($"[AutoFixLayers] FIXING PHYSICS LAYER MASKS... ({reason})");
        Debug.Log("[AutoFixLayers] ========================================");

        // ЧАСТЬ 1: Исправляем Rigidbody2D
        var allRigidbodies = Object.FindObjectsOfType<Rigidbody2D>();
        int rbFixedCount = 0;
        int rbTotalCount = 0;

        foreach (var rb in allRigidbodies)
        {
            rbTotalCount++;
            
            bool needsFix = rb.includeLayers.value == 0;
            string objName = rb.gameObject.name;
            string layerName = LayerMask.LayerToName(rb.gameObject.layer);
            
            Debug.Log($"[AutoFixLayers][RB] {objName} (Layer: {layerName})" +
                      $" BEFORE: includeLayers={rb.includeLayers.value}");
            
            if (needsFix)
            {
                rb.includeLayers = Physics2D.AllLayers;
                rb.excludeLayers = 0;
                
                rbFixedCount++;
                Debug.Log($"[AutoFixLayers][RB] ✓✓✓ FIXED: {objName} - includeLayers = {rb.includeLayers.value}");
            }
            else
            {
                Debug.Log($"[AutoFixLayers][RB] OK: {objName}");
            }
        }

        // ЧАСТЬ 2: Исправляем статические Collider2D (границы!)
        var allColliders = Object.FindObjectsOfType<Collider2D>();
        int colFixedCount = 0;
        int colTotalCount = 0;

        foreach (var col in allColliders)
        {
            // Проверяем только статические коллайдеры (границы)
            var rb = col.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Static)
                continue; // Пропускаем динамические объекты
            
            colTotalCount++;
            
            bool needsFix = col.includeLayers.value == 0;
            string objName = col.gameObject.name;
            string layerName = LayerMask.LayerToName(col.gameObject.layer);
            
            Debug.Log($"[AutoFixLayers][COL] {objName} (Layer: {layerName})" +
                      $" BEFORE: includeLayers={col.includeLayers.value}");
            
            if (needsFix)
            {
                col.includeLayers = Physics2D.AllLayers;
                col.excludeLayers = 0;
                
                colFixedCount++;
                Debug.LogWarning($"[AutoFixLayers][COL] ✓✓✓ FIXED BOUNDARY: {objName} - includeLayers = {col.includeLayers.value}");
            }
            else
            {
                Debug.Log($"[AutoFixLayers][COL] OK: {objName}");
            }
        }

        Debug.Log("[AutoFixLayers] ========================================");
        Debug.Log($"[AutoFixLayers] ✓ RIGIDBODY2D: Исправлено {rbFixedCount} из {rbTotalCount}");
        Debug.Log($"[AutoFixLayers] ✓ COLLIDER2D: Исправлено {colFixedCount} из {colTotalCount}");
        Debug.Log("[AutoFixLayers] ========================================");
        
        if (colFixedCount > 0)
        {
            Debug.LogWarning($"[AutoFixLayers] ⚠️ НАЙДЕНЫ И ИСПРАВЛЕНЫ ПРОБЛЕМНЫЕ ГРАНИЦЫ: {colFixedCount}");
            Debug.LogWarning("[AutoFixLayers] Это объясняет почему VIP персонажи проходили через нижнюю границу!");
        }
    }
}

