using UnityEngine;

public class QuickDebug : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("QuickDebug: START вызван!");
        
        InvokeRepeating("DebugInfo", 1f, 2f);
    }
    
    private void DebugInfo()
    {
        Debug.Log("=== QUICK DEBUG ===");
        Debug.Log("Время: " + Time.time);
        
        // Поиск всех компонентов
        var winds = FindObjectsOfType<WindFieldEffect>();
        var wanderers = FindObjectsOfType<WandererNew>();
        var systems = FindObjectsOfType<FieldEffectSystem>();
        
        Debug.Log($"WindFieldEffect найдено: {winds.Length}");
        Debug.Log($"WandererNew найдено: {wanderers.Length}");
        Debug.Log($"FieldEffectSystem найдено: {systems.Length}");
        
        if (winds.Length > 0)
        {
            Debug.Log($"Первый ветер: {winds[0].name}, активен: {winds[0].gameObject.activeInHierarchy}");
        }
        
        Debug.Log("=== END QUICK DEBUG ===");
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("T нажата - тестовое сообщение!");
        }
    }
} 