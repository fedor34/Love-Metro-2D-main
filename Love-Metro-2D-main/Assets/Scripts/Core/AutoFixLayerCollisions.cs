using UnityEngine;

/// <summary>
/// Автоматически включает необходимые коллизии между слоями при старте игры.
/// ВРЕМЕННОЕ РЕШЕНИЕ - лучше настроить в Project Settings → Physics 2D.
/// </summary>
public class AutoFixLayerCollisions : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void FixCollisions()
    {
        int defaultLayer = LayerMask.NameToLayer("Default");
        int wallLayer = LayerMask.NameToLayer("Wall");
        int softWallLayer = LayerMask.NameToLayer("SoftWall");
        int fallingLayer = LayerMask.NameToLayer("Falling");
        
        // Проверяем текущие настройки
        bool fallingVsSoftWall = !Physics2D.GetIgnoreLayerCollision(fallingLayer, softWallLayer);
        bool fallingVsWall = !Physics2D.GetIgnoreLayerCollision(fallingLayer, wallLayer);
        bool defaultVsWall = !Physics2D.GetIgnoreLayerCollision(defaultLayer, wallLayer);
        bool defaultVsSoftWall = !Physics2D.GetIgnoreLayerCollision(defaultLayer, softWallLayer);
        
        Debug.Log($"[AutoFixLayerCollisions] BEFORE FIX:");
        Debug.Log($"  Default <-> Wall: {defaultVsWall}");
        Debug.Log($"  Default <-> SoftWall: {defaultVsSoftWall}");
        Debug.Log($"  Falling <-> Wall: {fallingVsWall}");
        Debug.Log($"  Falling <-> SoftWall: {fallingVsSoftWall}");
        
        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Включаем коллизию Falling <-> SoftWall
        if (!fallingVsSoftWall)
        {
            Physics2D.IgnoreLayerCollision(fallingLayer, softWallLayer, false);
            Debug.LogWarning($"[AutoFixLayerCollisions] ✓ FIXED: Falling <-> SoftWall collision ENABLED");
        }
        
        // Также убеждаемся что Falling <-> Wall включена
        if (!fallingVsWall)
        {
            Physics2D.IgnoreLayerCollision(fallingLayer, wallLayer, false);
            Debug.LogWarning($"[AutoFixLayerCollisions] ✓ FIXED: Falling <-> Wall collision ENABLED");
        }
        
        // Убеждаемся что Default может коллизировать с границами
        if (!defaultVsWall)
        {
            Physics2D.IgnoreLayerCollision(defaultLayer, wallLayer, false);
            Debug.LogWarning($"[AutoFixLayerCollisions] ✓ FIXED: Default <-> Wall collision ENABLED");
        }
        
        if (!defaultVsSoftWall)
        {
            Physics2D.IgnoreLayerCollision(defaultLayer, softWallLayer, false);
            Debug.LogWarning($"[AutoFixLayerCollisions] ✓ FIXED: Default <-> SoftWall collision ENABLED");
        }
        
        // Проверяем результат
        fallingVsSoftWall = !Physics2D.GetIgnoreLayerCollision(fallingLayer, softWallLayer);
        fallingVsWall = !Physics2D.GetIgnoreLayerCollision(fallingLayer, wallLayer);
        defaultVsWall = !Physics2D.GetIgnoreLayerCollision(defaultLayer, wallLayer);
        defaultVsSoftWall = !Physics2D.GetIgnoreLayerCollision(defaultLayer, softWallLayer);
        
        Debug.Log($"[AutoFixLayerCollisions] AFTER FIX:");
        Debug.Log($"  Default <-> Wall: {defaultVsWall}");
        Debug.Log($"  Default <-> SoftWall: {defaultVsSoftWall}");
        Debug.Log($"  Falling <-> Wall: {fallingVsWall}");
        Debug.Log($"  Falling <-> SoftWall: {fallingVsSoftWall}");
        
        if (fallingVsSoftWall && fallingVsWall && defaultVsWall && defaultVsSoftWall)
        {
            Debug.Log($"[AutoFixLayerCollisions] ✓✓✓ ALL COLLISIONS ENABLED! ✓✓✓");
        }
        else
        {
            Debug.LogError($"[AutoFixLayerCollisions] ❌ SOME COLLISIONS STILL DISABLED!");
        }
    }
}



