using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Утилита для декодирования Layer Collision Matrix и выявления проблем с коллизиями.
/// </summary>
public class LayerCollisionMatrixDecoder : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var go = new GameObject("LayerCollisionMatrixDecoder");
        go.AddComponent<LayerCollisionMatrixDecoder>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        Invoke(nameof(DecodeAndCheck), 1f);
    }

    private void DecodeAndCheck()
    {
        Debug.Log("=== LAYER COLLISION MATRIX ANALYSIS ===");
        
        // Основные слои, которые нас интересуют
        int defaultLayer = LayerMask.NameToLayer("Default");
        int wallLayer = LayerMask.NameToLayer("Wall");
        int softWallLayer = LayerMask.NameToLayer("SoftWall");
        int fallingLayer = LayerMask.NameToLayer("Falling");
        
        Debug.Log($"[Layers] Default={defaultLayer}, Wall={wallLayer}, SoftWall={softWallLayer}, Falling={fallingLayer}");
        
        // Проверяем коллизии между ключевыми слоями
        bool defaultVsWall = !Physics2D.GetIgnoreLayerCollision(defaultLayer, wallLayer);
        bool defaultVsSoftWall = !Physics2D.GetIgnoreLayerCollision(defaultLayer, softWallLayer);
        bool fallingVsWall = !Physics2D.GetIgnoreLayerCollision(fallingLayer, wallLayer);
        bool fallingVsSoftWall = !Physics2D.GetIgnoreLayerCollision(fallingLayer, softWallLayer);
        bool defaultVsDefault = !Physics2D.GetIgnoreLayerCollision(defaultLayer, defaultLayer);
        bool fallingVsFalling = !Physics2D.GetIgnoreLayerCollision(fallingLayer, fallingLayer);
        
        Debug.Log($"[Collisions]");
        Debug.Log($"  Default <-> Default: {(defaultVsDefault ? "✓ ENABLED" : "✗ DISABLED")}");
        Debug.Log($"  Default <-> Wall: {(defaultVsWall ? "✓ ENABLED" : "✗ DISABLED")}");
        Debug.Log($"  Default <-> SoftWall: {(defaultVsSoftWall ? "✓ ENABLED" : "✗ DISABLED")}");
        Debug.Log($"  Falling <-> Falling: {(fallingVsFalling ? "✓ ENABLED" : "✗ DISABLED")}");
        Debug.Log($"  Falling <-> Wall: {(fallingVsWall ? "✓ ENABLED" : "✗ DISABLED")}");
        Debug.Log($"  Falling <-> SoftWall: {(fallingVsSoftWall ? "✓ ENABLED" : "✗ DISABLED")}");
        
        // Находим все границы
        var boundaries = FindBoundaries();
        Debug.Log($"\n[Boundaries Found] Total: {boundaries.Count}");
        foreach (var b in boundaries)
        {
            Debug.Log($"  - {b.name}: Layer={LayerMask.LayerToName(b.gameObject.layer)} ({b.gameObject.layer}), " +
                     $"Type={b.GetType().Name}, IsTrigger={b.isTrigger}");
        }
        
        // Находим всех пассажиров и проверяем их слои
        var passengers = FindObjectsOfType<Passenger>();
        Debug.Log($"\n[Passengers Check] Total: {passengers.Length}");
        
        var layerCounts = new Dictionary<int, int>();
        foreach (var p in passengers)
        {
            int layer = p.gameObject.layer;
            if (!layerCounts.ContainsKey(layer))
                layerCounts[layer] = 0;
            layerCounts[layer]++;
        }
        
        foreach (var kvp in layerCounts.OrderBy(x => x.Key))
        {
            string layerName = LayerMask.LayerToName(kvp.Key);
            Debug.Log($"  Layer {kvp.Key} ({layerName}): {kvp.Value} passengers");
            
            // Проверяем, может ли этот слой коллизировать с границами
            if (boundaries.Count > 0)
            {
                var firstBoundary = boundaries[0];
                int boundaryLayer = firstBoundary.gameObject.layer;
                bool canCollide = !Physics2D.GetIgnoreLayerCollision(kvp.Key, boundaryLayer);
                Debug.Log($"    Can collide with boundaries (Layer {boundaryLayer}): {(canCollide ? "✓ YES" : "✗ NO")}");
            }
        }
        
        // Проверяем конкретно VIP персонажей
        Debug.Log("\n[VIP Passengers Detail]");
        foreach (var p in passengers)
        {
            bool isVIP = IsVIP(p);
            if (isVIP)
            {
                var rb = p.GetComponent<Rigidbody2D>();
                var col = p.GetComponent<Collider2D>();
                Debug.Log($"  {p.name}:");
                Debug.Log($"    Layer: {LayerMask.LayerToName(p.gameObject.layer)} ({p.gameObject.layer})");
                Debug.Log($"    CollisionDetectionMode: {rb?.collisionDetectionMode}");
                Debug.Log($"    Collider: Type={col?.GetType().Name}, IsTrigger={col?.isTrigger}, Offset={((BoxCollider2D)col)?.offset}, Size={((BoxCollider2D)col)?.size}");
                Debug.Log($"    IncludeLayers: {rb?.includeLayers.value}, ExcludeLayers: {rb?.excludeLayers.value}");
            }
        }
        
        Debug.Log("\n=== END ANALYSIS ===");
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
    
    private List<Collider2D> FindBoundaries()
    {
        var result = new List<Collider2D>();
        var allColliders = FindObjectsOfType<Collider2D>();
        
        foreach (var col in allColliders)
        {
            string name = col.gameObject.name.ToLower();
            if (name.Contains("border") || name.Contains("wall") || name.Contains("boundary") || name.Contains("bound"))
            {
                result.Add(col);
            }
        }
        
        return result;
    }
    
    private void Update()
    {
        // Hotkey для повторного анализа
        if (Input.GetKeyDown(KeyCode.F10))
        {
            DecodeAndCheck();
        }
    }
}



