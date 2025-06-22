using UnityEngine;

/// <summary>
/// Простой тест для проверки статуса системы эффектов поля
/// Не создает автоматически объекты, только проверяет состояние системы
/// </summary>
public class SystemStatusTest : MonoBehaviour
{
    [Header("Отладочная информация")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private bool _logSystemStatus = false;
    
    private void Start()
    {
        if (_logSystemStatus)
        {
            LogSystemStatus();
        }
    }
    
    private void Update()
    {
        // Проверка статуса системы по нажатию клавиши
        if (Input.GetKeyDown(KeyCode.I))
        {
            LogSystemStatus();
        }
        
        // Создание простого эффекта гравитации по нажатию G
        if (Input.GetKeyDown(KeyCode.G))
        {
            CreateManualGravityEffect();
        }
        
        // Очистка всех эффектов по нажатию C
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearAllEffects();
        }
    }
    
    private void LogSystemStatus()
    {
        Debug.Log("=== СТАТУС СИСТЕМЫ ЭФФЕКТОВ ПОЛЯ ===");
        
        if (FieldEffectSystem.Instance == null)
        {
            Debug.LogError("[SystemStatusTest] FieldEffectSystem НЕ НАЙДЕНА!");
            return;
        }
        
        int effectsCount = FieldEffectSystem.Instance.GetTotalEffectsCount();
        int targetsCount = FieldEffectSystem.Instance.GetTargetsCount();
        
        Debug.Log($"[SystemStatusTest] Система активна");
        Debug.Log($"[SystemStatusTest] Эффектов: {effectsCount}");
        Debug.Log($"[SystemStatusTest] Целей: {targetsCount}");
        
        // Список активных эффектов
        var activeEffects = FieldEffectSystem.Instance.GetActiveEffects();
        if (activeEffects.Count > 0)
        {
            Debug.Log("[SystemStatusTest] Активные эффекты:");
            foreach (var effect in activeEffects)
            {
                if (effect != null)
                {
                    var data = effect.GetEffectData();
                    Debug.Log($"  - {data.effectType} (сила: {data.strength}, радиус: {data.radius})");
                }
            }
        }
        else
        {
            Debug.Log("[SystemStatusTest] Активных эффектов нет");
        }
    }
    
    private void CreateManualGravityEffect()
    {
        Debug.Log("[SystemStatusTest] Создание эффекта гравитации вручную...");
        
        try
        {
            // Создаем эффект в позиции (0, -2, 0)
            Vector3 position = new Vector3(0, -2, 0);
            var gravityData = new FieldEffectData(FieldEffectType.Gravity, 3f, 5f, position);
            
            var gravity = FieldEffectFactory.CreateEffect<GravityFieldEffectNew>(position, gravityData);
            gravity.name = "ManualGravity";
            
            Debug.Log($"[SystemStatusTest] Эффект гравитации создан успешно в позиции {position}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SystemStatusTest] Ошибка создания эффекта: {e.Message}");
        }
    }
    
    private void ClearAllEffects()
    {
        Debug.Log("[SystemStatusTest] Очистка всех эффектов...");
        
        if (FieldEffectSystem.Instance != null)
        {
            FieldEffectSystem.Instance.ClearAllEffects();
            Debug.Log("[SystemStatusTest] Все эффекты очищены");
        }
        else
        {
            Debug.LogError("[SystemStatusTest] Система эффектов не найдена для очистки");
        }
    }
    
    #if UNITY_EDITOR
    private void OnGUI()
    {
        if (!_showDebugInfo) return;
        
        GUI.Box(new Rect(10, 10, 200, 120), "Field Effects Debug");
        
        if (GUI.Button(new Rect(20, 35, 180, 20), "Log System Status"))
        {
            LogSystemStatus();
        }
        
        if (GUI.Button(new Rect(20, 60, 180, 20), "Create Gravity Effect"))
        {
            CreateManualGravityEffect();
        }
        
        if (GUI.Button(new Rect(20, 85, 180, 20), "Clear All Effects"))
        {
            ClearAllEffects();
        }
        
        // Показываем статус системы
        string statusText = "System Status: ";
        if (FieldEffectSystem.Instance != null)
        {
            int effects = FieldEffectSystem.Instance.GetTotalEffectsCount();
            int targets = FieldEffectSystem.Instance.GetTargetsCount();
            statusText += $"OK ({effects}E, {targets}T)";
        }
        else
        {
            statusText += "NOT FOUND!";
        }
        
        GUI.Label(new Rect(20, 105, 180, 20), statusText);
    }
    #endif
} 