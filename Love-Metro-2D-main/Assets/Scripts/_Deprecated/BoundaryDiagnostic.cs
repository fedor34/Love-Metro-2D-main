using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Диагностический скрипт для исследования проблемы с VIP персонажами, проходящими через границы.
/// </summary>
public class BoundaryDiagnostic : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var go = new GameObject("BoundaryDiagnostic");
        go.AddComponent<BoundaryDiagnostic>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        Invoke(nameof(RunDiagnostics), 2f); // Запускаем через 2 секунды после старта сцены
    }

    private void RunDiagnostics()
    {
        Debug.Log("=== BOUNDARY DIAGNOSTIC START ===");
        
        // Проверяем все границы в сцене
        var allColliders = FindObjectsOfType<Collider2D>();
        var boundaryColliders = new List<Collider2D>();
        
        foreach (var col in allColliders)
        {
            if (col.gameObject.name.ToLower().Contains("border") ||
                col.gameObject.name.ToLower().Contains("wall") ||
                col.gameObject.name.ToLower().Contains("boundary"))
            {
                boundaryColliders.Add(col);
                Debug.Log($"[Boundary] Found: {col.gameObject.name}, Layer: {LayerMask.LayerToName(col.gameObject.layer)}, " +
                         $"Type: {col.GetType().Name}, IsTrigger: {col.isTrigger}, UsedByEffector: {col.usedByEffector}");
            }
        }
        
        // Проверяем всех пассажиров
        var passengers = FindObjectsOfType<Passenger>();
        Debug.Log($"[Passengers] Total found: {passengers.Length}");
        
        var vipCount = 0;
        var normalCount = 0;
        
        foreach (var p in passengers)
        {
            bool isVIP = p.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var anim = p.GetComponent<Animator>();
            if (!isVIP && anim != null && anim.runtimeAnimatorController != null)
            {
                isVIP = anim.runtimeAnimatorController.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
            
            if (isVIP) vipCount++; else normalCount++;
            
            var rb = p.GetComponent<Rigidbody2D>();
            var col = p.GetComponent<Collider2D>();
            
            Debug.Log($"[Passenger] {p.name}, IsVIP: {isVIP}, Layer: {LayerMask.LayerToName(p.gameObject.layer)}, " +
                     $"RB: CollisionDetection={rb.collisionDetectionMode}, " +
                     $"Collider: Type={col.GetType().Name}, IsTrigger={col.isTrigger}, " +
                     $"IncludeLayers: {rb.includeLayers.value}, ExcludeLayers: {rb.excludeLayers.value}");
        }
        
        Debug.Log($"[Summary] VIP: {vipCount}, Normal: {normalCount}, Boundaries: {boundaryColliders.Count}");
        
        // Проверяем Layer Collision Matrix
        CheckLayerCollisions();
        
        Debug.Log("=== BOUNDARY DIAGNOSTIC END ===");
    }
    
    private void CheckLayerCollisions()
    {
        int defaultLayer = LayerMask.NameToLayer("Default");
        int wallLayer = LayerMask.NameToLayer("Wall");
        int softWallLayer = LayerMask.NameToLayer("SoftWall");
        int fallingLayer = LayerMask.NameToLayer("Falling");
        
        Debug.Log($"[LayerCheck] Default: {defaultLayer}, Wall: {wallLayer}, SoftWall: {softWallLayer}, Falling: {fallingLayer}");
        Debug.Log($"[LayerCheck] Default <-> Wall: {!Physics2D.GetIgnoreLayerCollision(defaultLayer, wallLayer)}");
        Debug.Log($"[LayerCheck] Default <-> SoftWall: {!Physics2D.GetIgnoreLayerCollision(defaultLayer, softWallLayer)}");
        Debug.Log($"[LayerCheck] Falling <-> Wall: {!Physics2D.GetIgnoreLayerCollision(fallingLayer, wallLayer)}");
        Debug.Log($"[LayerCheck] Falling <-> SoftWall: {!Physics2D.GetIgnoreLayerCollision(fallingLayer, softWallLayer)}");
    }
    
    private void Update()
    {
        // Проверяем VIP персонажей на выход за границы в реальном времени
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("=== REAL-TIME CHECK ===");
            var passengers = FindObjectsOfType<Passenger>();
            foreach (var p in passengers)
            {
                bool isVIP = p.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
                var anim = p.GetComponent<Animator>();
                if (!isVIP && anim != null && anim.runtimeAnimatorController != null)
                {
                    isVIP = anim.runtimeAnimatorController.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
                }
                
                if (isVIP)
                {
                    Debug.Log($"[VIP Position] {p.name} at {p.transform.position}, State: {p.GetCurrentStateName()}, Layer: {LayerMask.LayerToName(p.gameObject.layer)}");
                }
            }
        }
    }
}



