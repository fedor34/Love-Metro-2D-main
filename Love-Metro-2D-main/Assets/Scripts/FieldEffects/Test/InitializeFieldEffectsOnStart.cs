using UnityEngine;

/// <summary>
/// Простой скрипт для инициализации FieldEffects системы.
/// Добавьте его к любому объекту в сцене и он создаст все необходимые компоненты.
/// </summary>
public class InitializeFieldEffectsOnStart : MonoBehaviour
{
    [Header("Настройки инициализации")]
    [Tooltip("Создавать ли FieldEffectSystem при старте")]
    public bool createFieldEffectSystem = true;
    
    [Tooltip("Создавать ли BlackHoleTest при старте")]
    public bool createBlackHoleTest = true;
    
    [Tooltip("Позиция для создания черной дыры")]
    public Vector3 blackHolePosition = Vector3.zero;
    
    [Tooltip("Удалить этот компонент после инициализации")]
    public bool destroyAfterInit = true;
    
    private void Start()
    {
        InitializeFieldEffects();
        
        if (destroyAfterInit)
        {
            // Удаляем этот компонент после выполнения
            Destroy(this);
        }
    }
    
    private void InitializeFieldEffects()
    {
        Debug.Log("[InitializeFieldEffects] Начинаем инициализацию...");
        
        if (createFieldEffectSystem)
        {
            CreateFieldEffectSystemIfNeeded();
        }
        
        if (createBlackHoleTest)
        {
            CreateBlackHoleTestIfNeeded();
        }
        
        Debug.Log("[InitializeFieldEffects] Инициализация завершена!");
    }
    
    private void CreateFieldEffectSystemIfNeeded()
    {
        if (FieldEffectSystem.Instance != null)
        {
            Debug.Log("[InitializeFieldEffects] FieldEffectSystem уже существует");
            return;
        }
        
        GameObject systemObj = new GameObject("FieldEffectSystem");
        systemObj.AddComponent<FieldEffectSystem>();
        
        Debug.Log("[InitializeFieldEffects] FieldEffectSystem создан");
    }
    
    private void CreateBlackHoleTestIfNeeded()
    {
        var existingBlackHole = FindObjectOfType<BlackHoleTest>();
        if (existingBlackHole != null)
        {
            Debug.Log("[InitializeFieldEffects] BlackHoleTest уже существует");
            return;
        }
        
        GameObject blackHoleObj = new GameObject("BlackHoleTest");
        blackHoleObj.transform.position = blackHolePosition;
        blackHoleObj.AddComponent<BlackHoleTest>();
        
        Debug.Log($"[InitializeFieldEffects] BlackHoleTest создан в позиции {blackHolePosition}");
    }
    
    // Метод для ручного вызова из Inspector
    [ContextMenu("Initialize Field Effects")]
    public void ManualInitialize()
    {
        InitializeFieldEffects();
    }
} 