using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Отладчик для ветрового эффекта - показывает детальную информацию о работе системы
/// </summary>
public class WindEffectDebugger : MonoBehaviour
{
    [Header("Отладка")]
    [SerializeField] private bool _enableDebug = true;
    [SerializeField] private float _debugUpdateInterval = 1f;
    
    private float _lastDebugTime;
    
    private void Update()
    {
        if (!_enableDebug) return;
        
        if (Time.time - _lastDebugTime > _debugUpdateInterval)
        {
            _lastDebugTime = Time.time;
            DebugFieldEffectSystem();
        }
    }
    
    private void DebugFieldEffectSystem()
    {
        Debug.Log("=== WIND EFFECT DEBUG ===");
        
        // Проверяем наличие FieldEffectSystem
        if (FieldEffectSystem.Instance == null)
        {
            Debug.LogError("[WindDebugger] FieldEffectSystem.Instance == null! Система не инициализирована!");
            return;
        }
        
        Debug.Log($"[WindDebugger] FieldEffectSystem найдена: {FieldEffectSystem.Instance.gameObject.name}");
        
        // Находим все ветровые эффекты
        var windEffects = FindObjectsOfType<WindFieldEffect>();
        Debug.Log($"[WindDebugger] Найдено ветровых эффектов: {windEffects.Length}");
        
        foreach (var wind in windEffects)
        {
            var data = wind.GetEffectData();
            Debug.Log($"[WindDebugger] Ветер {wind.name}: активен={wind.IsActive}, сила={data.strength}, радиус={data.radius}, центр={data.center}");
        }
        
        // Находим все цели
        var targets = FindObjectsOfType<WandererNew>();
        Debug.Log($"[WindDebugger] Найдено целей (WandererNew): {targets.Length}");
        
        int initiatedTargets = 0;
        foreach (var target in targets)
        {
            if (target.GetComponent<WandererNew>() != null)
            {
                // Проверяем инициализацию через публичные свойства
                bool canBeAffected = target.CanBeAffectedBy(FieldEffectType.Wind);
                Debug.Log($"[WindDebugger] Цель {target.name}: может_быть_затронута_ветром={canBeAffected}");
                if (canBeAffected) initiatedTargets++;
            }
        }
        
        Debug.Log($"[WindDebugger] Инициализированных целей: {initiatedTargets}");
        
        // Проверяем перекрытие ветра и целей
        CheckWindTargetOverlap();
        
        Debug.Log("=== END WIND DEBUG ===");
    }
    
    private void CheckWindTargetOverlap()
    {
        var windEffects = FindObjectsOfType<WindFieldEffect>();
        var targets = FindObjectsOfType<WandererNew>();
        
        foreach (var wind in windEffects)
        {
            var windData = wind.GetEffectData();
            int targetsInRange = 0;
            
            foreach (var target in targets)
            {
                float distance = Vector3.Distance(wind.transform.position, target.transform.position);
                bool inRange = distance <= windData.radius;
                
                if (inRange)
                {
                    targetsInRange++;
                    Debug.Log($"[WindDebugger] Цель {target.name} В ЗОНЕ ветра {wind.name}: расстояние={distance:F2}, радиус={windData.radius}");
                }
            }
            
            Debug.Log($"[WindDebugger] Ветер {wind.name} влияет на {targetsInRange} целей");
        }
    }
    
    [ContextMenu("Manual Debug")]
    public void ManualDebug()
    {
        DebugFieldEffectSystem();
    }
    
    [ContextMenu("Create FieldEffectSystem")]
    public void CreateFieldEffectSystem()
    {
        if (FieldEffectSystem.Instance != null)
        {
            Debug.Log("[WindDebugger] FieldEffectSystem уже существует");
            return;
        }
        
        var systemObj = new GameObject("FieldEffectSystem");
        systemObj.AddComponent<FieldEffectSystem>();
        Debug.Log("[WindDebugger] FieldEffectSystem создан вручную");
    }
} 