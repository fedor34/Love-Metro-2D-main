using UnityEngine;

public class AutoCreateFieldEffects : MonoBehaviour
{
    [Header("Настройки автосоздания")]
    [SerializeField] private bool _createOnStart = true;
    [SerializeField] private Vector3 _blackHolePosition = Vector3.zero;
    
    private void Start()
    {
        if (!_createOnStart) return;
        
        Debug.Log("[AutoCreateFieldEffects] Начинаем автосоздание системы эффектов...");
        
        // Создаем FieldEffectSystem если его нет
        CreateFieldEffectSystem();
        
        // Создаем BlackHoleTest если его нет
        CreateBlackHoleTest();
        
        Debug.Log("[AutoCreateFieldEffects] Автосоздание завершено!");
        
        // Отключаем этот компонент после создания
        this.enabled = false;
    }
    
    private void CreateFieldEffectSystem()
    {
        // Проверяем есть ли уже FieldEffectSystem
        if (FieldEffectSystem.Instance != null)
        {
            Debug.Log("[AutoCreateFieldEffects] FieldEffectSystem уже существует");
            return;
        }
        
        // Создаем новый объект для FieldEffectSystem
        GameObject systemObj = new GameObject("FieldEffectSystem");
        systemObj.AddComponent<FieldEffectSystem>();
        
        Debug.Log("[AutoCreateFieldEffects] FieldEffectSystem создан");
    }
    
    private void CreateBlackHoleTest()
    {
        // Проверяем есть ли уже BlackHoleTest
        var existingBlackHole = FindObjectOfType<BlackHoleTest>();
        if (existingBlackHole != null)
        {
            Debug.Log("[AutoCreateFieldEffects] BlackHoleTest уже существует");
            return;
        }
        
        // Создаем новый объект для BlackHoleTest
        GameObject blackHoleObj = new GameObject("BlackHoleTest");
        blackHoleObj.transform.position = _blackHolePosition;
        
        var blackHoleTest = blackHoleObj.AddComponent<BlackHoleTest>();
        
        Debug.Log($"[AutoCreateFieldEffects] BlackHoleTest создан в позиции {_blackHolePosition}");
    }
    
    // Метод для ручного создания через Inspector
    [ContextMenu("Create Field Effects")]
    public void CreateFieldEffectsManually()
    {
        CreateFieldEffectSystem();
        CreateBlackHoleTest();
    }
} 