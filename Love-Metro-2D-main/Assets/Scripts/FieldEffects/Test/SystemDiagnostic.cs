using UnityEngine;

public class SystemDiagnostic : MonoBehaviour
{
    private void Start()
    {
        InvokeRepeating("QuickCheck", 2f, 3f);
    }
    
    private void QuickCheck()
    {
        Debug.Log("=== SYSTEM DIAGNOSTIC ===");
        
        // Проверяем FieldEffectSystem
        if (FieldEffectSystem.Instance == null)
        {
            Debug.LogError("FieldEffectSystem отсутствует!");
            CreateFieldEffectSystem();
        }
        else
        {
            Debug.Log($"FieldEffectSystem: OK ({FieldEffectSystem.Instance.GetTotalEffectsCount()} эффектов, {FieldEffectSystem.Instance.GetTargetsCount()} целей)");
        }
        
        // Проверяем WindFieldEffect
        var winds = FindObjectsOfType<WindFieldEffect>();
        Debug.Log($"WindFieldEffect найдено: {winds.Length}");
        
        if (winds.Length > 0)
        {
            var wind = winds[0];
            var data = wind.GetEffectData();
            Debug.Log($"Ветер: позиция={wind.transform.position}, радиус={data.radius}, сила={data.strength}");
        }
        
        // Проверяем персонажей
        var wanderers = FindObjectsOfType<WandererNew>();
        Debug.Log($"Персонажей: {wanderers.Length}");
        
        if (wanderers.Length > 0 && winds.Length > 0)
        {
            var wind = winds[0];
            var wanderer = wanderers[0];
            float distance = Vector3.Distance(wind.transform.position, wanderer.transform.position);
            Debug.Log($"Расстояние ветер-персонаж: {distance:F2}");
        }
        
        Debug.Log("=== END DIAGNOSTIC ===");
    }
    
    private void CreateFieldEffectSystem()
    {
        var systemObj = new GameObject("FieldEffectSystem");
        systemObj.AddComponent<FieldEffectSystem>();
        Debug.Log("FieldEffectSystem создан автоматически");
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("=== MANUAL RESTART ===");
            QuickCheck();
        }
    }
} 