using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Финальная ультимативная диагностика для выявления проблемы с границами.
/// </summary>
public class UltimateDiagnostic : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var go = new GameObject("UltimateDiagnostic");
        go.AddComponent<UltimateDiagnostic>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        Invoke(nameof(RunFullDiagnostic), 2f);
        InvokeRepeating(nameof(MonitorVIPPositions), 3f, 0.5f); // каждые 0.5 сек
    }

    private void RunFullDiagnostic()
    {
        Debug.Log("╔═══════════════════════════════════════════════════════════╗");
        Debug.Log("║  ULTIMATE DIAGNOSTIC - ПОЛНАЯ ДИАГНОСТИКА                 ║");
        Debug.Log("╚═══════════════════════════════════════════════════════════╝");
        
        // 1. Проверяем все границы
        CheckBoundaries();
        
        // 2. Проверяем всех VIP
        CheckVIPPassengers();
        
        // 3. Проверяем Layer Collision Matrix
        CheckLayerMatrix();
        
        Debug.Log("═══════════════════════════════════════════════════════════");
    }
    
    private void CheckBoundaries()
    {
        Debug.Log("\n[1] === ПРОВЕРКА ГРАНИЦ ===");
        
        var allColliders = FindObjectsOfType<Collider2D>();
        var boundaries = new List<Collider2D>();
        
        foreach (var col in allColliders)
        {
            if (col == null) continue;
            if (col.GetComponent<Passenger>() != null) continue;
            
            var rb = col.GetComponent<Rigidbody2D>();
            if (rb == null || rb.bodyType == RigidbodyType2D.Static)
            {
                boundaries.Add(col);
            }
        }
        
        Debug.Log($"Найдено статических коллайдеров: {boundaries.Count}");
        
        if (boundaries.Count == 0)
        {
            Debug.LogError("❌ КРИТИЧНО: Границы НЕ НАЙДЕНЫ!");
            Debug.LogError("   → Нужно добавить BoxCollider2D к объектам границ!");
            return;
        }
        
        // Выводим топ-10 самых больших (вероятно границы)
        boundaries.Sort((a, b) => GetColliderSize(b).CompareTo(GetColliderSize(a)));
        
        Debug.Log("\nТОП границ (по размеру):");
        for (int i = 0; i < Mathf.Min(10, boundaries.Count); i++)
        {
            var col = boundaries[i];
            Debug.Log($"  {i+1}. {col.gameObject.name}:");
            Debug.Log($"     Layer: {LayerMask.LayerToName(col.gameObject.layer)} ({col.gameObject.layer})");
            Debug.Log($"     Type: {col.GetType().Name}");
            Debug.Log($"     IsTrigger: {col.isTrigger}");
            Debug.Log($"     Size: {GetColliderSizeVec(col)}");
            Debug.Log($"     Position: {col.transform.position}");
        }
    }
    
    private void CheckVIPPassengers()
    {
        Debug.Log("\n[2] === ПРОВЕРКА VIP ПЕРСОНАЖЕЙ ===");
        
        var passengers = FindObjectsOfType<Passenger>();
        var vips = new List<Passenger>();
        
        foreach (var p in passengers)
        {
            if (IsVIP(p)) vips.Add(p);
        }
        
        Debug.Log($"Найдено VIP: {vips.Count}");
        
        foreach (var vip in vips)
        {
            var rb = vip.GetComponent<Rigidbody2D>();
            var col = vip.GetComponent<Collider2D>();
            var box = col as BoxCollider2D;
            
            Debug.Log($"\n  VIP: {vip.name}");
            Debug.Log($"    Position: {vip.transform.position}");
            Debug.Log($"    Layer: {LayerMask.LayerToName(vip.gameObject.layer)} ({vip.gameObject.layer})");
            Debug.Log($"    State: {vip.GetCurrentStateName()}");
            Debug.Log($"    Velocity: {rb.velocity} (magnitude: {rb.velocity.magnitude:F2})");
            Debug.Log($"    CollisionDetectionMode: {rb.collisionDetectionMode}");
            Debug.Log($"    Interpolation: {rb.interpolation}");
            Debug.Log($"    Collider Type: {col.GetType().Name}");
            Debug.Log($"    Collider IsTrigger: {col.isTrigger}");
            if (box != null)
            {
                Debug.Log($"    Collider Offset: {box.offset}");
                Debug.Log($"    Collider Size: {box.size}");
            }
            
            // Проверяем скорость - tunneling?
            if (rb.velocity.magnitude > 20f)
            {
                Debug.LogWarning($"    ⚠️ ВЫСОКАЯ СКОРОСТЬ! Может быть tunneling!");
                Debug.LogWarning($"       → Скорость {rb.velocity.magnitude:F2} > 20");
                
                if (rb.collisionDetectionMode != CollisionDetectionMode2D.Continuous)
                {
                    Debug.LogError($"    ❌ CollisionDetection НЕ Continuous!");
                    Debug.LogError($"       → Это может вызывать tunneling!");
                }
            }
        }
    }
    
    private void CheckLayerMatrix()
    {
        Debug.Log("\n[3] === LAYER COLLISION MATRIX ===");
        
        int defaultL = LayerMask.NameToLayer("Default");
        int wallL = LayerMask.NameToLayer("Wall");
        int softWallL = LayerMask.NameToLayer("SoftWall");
        int fallingL = LayerMask.NameToLayer("Falling");
        
        bool defWall = !Physics2D.GetIgnoreLayerCollision(defaultL, wallL);
        bool defSoft = !Physics2D.GetIgnoreLayerCollision(defaultL, softWallL);
        bool fallWall = !Physics2D.GetIgnoreLayerCollision(fallingL, wallL);
        bool fallSoft = !Physics2D.GetIgnoreLayerCollision(fallingL, softWallL);
        
        Debug.Log($"  Default <-> Wall: {(defWall ? "✓ ENABLED" : "❌ DISABLED")}");
        Debug.Log($"  Default <-> SoftWall: {(defSoft ? "✓ ENABLED" : "❌ DISABLED")}");
        Debug.Log($"  Falling <-> Wall: {(fallWall ? "✓ ENABLED" : "❌ DISABLED")}");
        Debug.Log($"  Falling <-> SoftWall: {(fallSoft ? "✓ ENABLED" : "❌ DISABLED")}");
        
        if (!defWall || !defSoft || !fallWall || !fallSoft)
        {
            Debug.LogError("❌ НЕКОТОРЫЕ КОЛЛИЗИИ ВЫКЛЮЧЕНЫ!");
            Debug.LogError("   → AutoFixLayerCollisions должен был их включить!");
        }
    }
    
    private void MonitorVIPPositions()
    {
        var passengers = FindObjectsOfType<Passenger>();
        
        foreach (var p in passengers)
        {
            if (!IsVIP(p)) continue;
            
            Vector3 pos = p.transform.position;
            var rb = p.GetComponent<Rigidbody2D>();
            
            // Проверяем выход за границы экрана (примерно)
            if (Mathf.Abs(pos.x) > 30 || Mathf.Abs(pos.y) > 20)
            {
                Debug.LogError($"❌❌❌ VIP {p.name} ВЫЛЕТЕЛ ЗА ГРАНИЦЫ! ❌❌❌");
                Debug.LogError($"  Position: {pos}");
                Debug.LogError($"  Velocity: {rb.velocity}");
                Debug.LogError($"  State: {p.GetCurrentStateName()}");
                Debug.LogError($"  Layer: {LayerMask.LayerToName(p.gameObject.layer)}");
                Debug.LogError($"  CollisionDetection: {rb.collisionDetectionMode}");
            }
        }
    }
    
    private bool IsVIP(Passenger p)
    {
        if (p.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        var anim = p.GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
            return anim.runtimeAnimatorController.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
        return false;
    }
    
    private float GetColliderSize(Collider2D col)
    {
        if (col is BoxCollider2D box) return box.size.x * box.size.y;
        if (col is CircleCollider2D circle) return circle.radius * circle.radius * Mathf.PI;
        if (col is CapsuleCollider2D capsule) return capsule.size.x * capsule.size.y;
        return 0;
    }
    
    private Vector2 GetColliderSizeVec(Collider2D col)
    {
        if (col is BoxCollider2D box) return box.size;
        if (col is CircleCollider2D circle) return new Vector2(circle.radius * 2, circle.radius * 2);
        if (col is CapsuleCollider2D capsule) return capsule.size;
        return Vector2.zero;
    }
}



