using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Находит ВСЕ объекты в сцене, которые могут быть границами поля.
/// Запускается автоматически при старте игры.
/// </summary>
public class FindAllBoundaries : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var go = new GameObject("FindAllBoundaries");
        go.AddComponent<FindAllBoundaries>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        Invoke(nameof(FindBoundaries), 1f);
    }

    private void FindBoundaries()
    {
        Debug.Log("=== SEARCHING FOR ALL POSSIBLE BOUNDARIES ===");
        
        // Найдем ВСЕ коллайдеры в сцене
        var allColliders = FindObjectsOfType<Collider2D>(true); // включая неактивные
        
        Debug.Log($"Total colliders in scene: {allColliders.Length}");
        
        var possibleBoundaries = new List<Collider2D>();
        var staticColliders = new List<Collider2D>();
        
        foreach (var col in allColliders)
        {
            if (col == null) continue;
            
            // Проверяем, является ли этот объект пассажиром
            var passenger = col.GetComponent<Passenger>();
            if (passenger != null) continue; // пропускаем пассажиров
            
            // Статические коллайдеры - потенциальные границы
            var rb = col.GetComponent<Rigidbody2D>();
            if (rb == null || rb.bodyType == RigidbodyType2D.Static)
            {
                staticColliders.Add(col);
                
                // Проверяем имя на ключевые слова
                string name = col.gameObject.name.ToLower();
                if (name.Contains("wall") || name.Contains("border") || 
                    name.Contains("boundary") || name.Contains("bound") ||
                    name.Contains("edge") || name.Contains("limit"))
                {
                    possibleBoundaries.Add(col);
                }
            }
        }
        
        Debug.Log($"\n=== STATIC COLLIDERS (potential boundaries): {staticColliders.Count} ===");
        foreach (var col in staticColliders)
        {
            var info = GetColliderInfo(col);
            Debug.Log($"  {col.gameObject.name}: {info}");
        }
        
        Debug.Log($"\n=== IDENTIFIED BOUNDARIES by name: {possibleBoundaries.Count} ===");
        foreach (var col in possibleBoundaries)
        {
            var info = GetColliderInfo(col);
            Debug.LogWarning($"  ★ {col.gameObject.name}: {info}");
        }
        
        // Найдем объекты с большими коллайдерами (вероятно границы)
        Debug.Log("\n=== LARGE COLLIDERS (likely boundaries) ===");
        foreach (var col in staticColliders)
        {
            if (GetColliderSize(col) > 10f) // большие коллайдеры
            {
                var info = GetColliderInfo(col);
                Debug.LogWarning($"  ★ LARGE: {col.gameObject.name}: {info}");
            }
        }
        
        Debug.Log("\n=== END BOUNDARY SEARCH ===");
    }
    
    private string GetColliderInfo(Collider2D col)
    {
        string type = col.GetType().Name;
        string layer = LayerMask.LayerToName(col.gameObject.layer);
        bool isTrigger = col.isTrigger;
        Vector2 size = GetColliderSize2D(col);
        Vector3 pos = col.transform.position;
        
        return $"Type={type}, Layer={layer}({col.gameObject.layer}), IsTrigger={isTrigger}, Size≈{size}, Pos={pos}";
    }
    
    private float GetColliderSize(Collider2D col)
    {
        if (col is BoxCollider2D box) return Mathf.Max(box.size.x, box.size.y);
        if (col is CircleCollider2D circle) return circle.radius * 2;
        if (col is CapsuleCollider2D capsule) return Mathf.Max(capsule.size.x, capsule.size.y);
        if (col is EdgeCollider2D edge) 
        {
            if (edge.points.Length < 2) return 0;
            return Vector2.Distance(edge.points[0], edge.points[edge.points.Length - 1]);
        }
        return 0;
    }
    
    private Vector2 GetColliderSize2D(Collider2D col)
    {
        if (col is BoxCollider2D box) return box.size;
        if (col is CircleCollider2D circle) return new Vector2(circle.radius * 2, circle.radius * 2);
        if (col is CapsuleCollider2D capsule) return capsule.size;
        return Vector2.zero;
    }
}



