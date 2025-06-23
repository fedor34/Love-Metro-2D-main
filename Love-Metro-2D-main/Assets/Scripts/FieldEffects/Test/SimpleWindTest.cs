using UnityEngine;

public class SimpleWindTest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== SIMPLE WIND TEST START ===");
        
        // Проверяем базовые компоненты
        var windEffects = FindObjectsOfType<WindFieldEffect>();
        Debug.Log($"Найдено WindFieldEffect: {windEffects.Length}");
        
        var wanderers = FindObjectsOfType<WandererNew>();
        Debug.Log($"Найдено WandererNew: {wanderers.Length}");
        
        // Проверяем FieldEffectSystem
        if (FieldEffectSystem.Instance == null)
        {
            Debug.LogError("FieldEffectSystem.Instance == NULL! Создаю вручную...");
            var systemObj = new GameObject("FieldEffectSystem");
            systemObj.AddComponent<FieldEffectSystem>();
        }
        else
        {
            Debug.Log("FieldEffectSystem найден: " + FieldEffectSystem.Instance.gameObject.name);
        }
        
        Debug.Log("=== SIMPLE WIND TEST END ===");
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("=== MANUAL TEST (пробел нажат) ===");
            TestWindEffect();
        }
    }
    
    private void TestWindEffect()
    {
        var windEffects = FindObjectsOfType<WindFieldEffect>();
        var wanderers = FindObjectsOfType<WandererNew>();
        
        Debug.Log($"WindEffects: {windEffects.Length}, Wanderers: {wanderers.Length}");
        
        if (windEffects.Length > 0 && wanderers.Length > 0)
        {
            var wind = windEffects[0];
            var wanderer = wanderers[0];
            
            float distance = Vector3.Distance(wind.transform.position, wanderer.transform.position);
            var windData = wind.GetEffectData();
            
            Debug.Log($"Расстояние от ветра до персонажа: {distance:F2}");
            Debug.Log($"Радиус ветра: {windData.radius}");
            Debug.Log($"Персонаж в зоне ветра: {distance <= windData.radius}");
            
            // Принудительно применяем ветер
            if (wanderer.CanBeAffectedBy(FieldEffectType.Wind))
            {
                Vector2 testForce = Vector2.right * 5f;
                wanderer.ApplyFieldForce(testForce, FieldEffectType.Wind);
                Debug.Log("Принудительно применили ветер!");
            }
            else
            {
                Debug.LogWarning("Персонаж не может быть затронут ветром!");
            }
        }
    }
} 