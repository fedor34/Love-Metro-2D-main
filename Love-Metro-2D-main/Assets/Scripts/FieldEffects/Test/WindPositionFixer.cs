using UnityEngine;

public class WindPositionFixer : MonoBehaviour
{
    [ContextMenu("Fix Wind Position")]
    public void FixWindPosition()
    {
        Debug.Log("=== FIXING WIND POSITION ===");
        
        var windEffects = FindObjectsOfType<WindFieldEffect>();
        var wanderers = FindObjectsOfType<WandererNew>();
        
        if (windEffects.Length == 0)
        {
            Debug.LogError("WindFieldEffect не найден!");
            return;
        }
        
        if (wanderers.Length == 0)
        {
            Debug.LogError("WandererNew не найден!");
            return;
        }
        
        var wind = windEffects[0];
        var firstWanderer = wanderers[0];
        
        // Перемещаем ветер к первому персонажу
        wind.transform.position = firstWanderer.transform.position + Vector3.left * 2f;
        
        // Увеличиваем радиус
        var windData = wind.GetEffectData();
        windData.radius = 15f;
        windData.strength = 50f;
        wind.SetRadius(15f);
        wind.SetStrength(50f);
        
        Debug.Log($"Ветер перемещен к позиции: {wind.transform.position}");
        Debug.Log($"Радиус установлен: {windData.radius}");
        Debug.Log($"Сила установлена: {windData.strength}");
        
        Debug.Log("=== WIND POSITION FIXED ===");
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            FixWindPosition();
        }
    }
} 