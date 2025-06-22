using UnityEngine;

public class SpawnDebugger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== ОТЛАДЧИК СПАВНА ЗАПУЩЕН ===");
        
        // Проверяем FieldEffectSystem
        if (FieldEffectSystem.Instance == null)
        {
            Debug.LogError("[SpawnDebugger] FieldEffectSystem.Instance = null!");
        }
        else
        {
            Debug.Log($"[SpawnDebugger] FieldEffectSystem найден: {FieldEffectSystem.Instance.name}");
        }
        
        // Проверяем PassangerSpawner
        var spawner = FindObjectOfType<PassangerSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[SpawnDebugger] PassangerSpawner не найден!");
        }
        else
        {
            Debug.Log($"[SpawnDebugger] PassangerSpawner найден: {spawner.name}");
        }
        
        // Проверяем префабы персонажей
        var femalePrefs = Resources.FindObjectsOfTypeAll<WandererNew>();
        Debug.Log($"[SpawnDebugger] Найдено префабов WandererNew: {femalePrefs.Length}");
        
        // Проверяем активных персонажей в сцене
        var activePassengers = FindObjectsOfType<WandererNew>();
        Debug.Log($"[SpawnDebugger] Активных персонажей в сцене: {activePassengers.Length}");
        
        foreach (var passenger in activePassengers)
        {
            Debug.Log($"[SpawnDebugger] Персонаж: {passenger.name}, позиция: {passenger.transform.position}");
        }
    }
    
    private void Update()
    {
        // Каждые 5 секунд проверяем количество персонажей
        if (Time.time % 5f < Time.deltaTime)
        {
            var activePassengers = FindObjectsOfType<WandererNew>();
            Debug.Log($"[SpawnDebugger] Персонажей в сцене: {activePassengers.Length}");
        }
    }
} 