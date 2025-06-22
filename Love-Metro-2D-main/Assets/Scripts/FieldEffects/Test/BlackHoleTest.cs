using UnityEngine;

/// <summary>
/// Тестовый скрипт для проверки работы черной дыры
/// </summary>
public class BlackHoleTest : MonoBehaviour
{
    [Header("Параметры черной дыры")]
    [SerializeField] private float _strength = 10f;
    [SerializeField] private float _radius = 15f;
    [SerializeField] private float _eventHorizonRadius = 3f;
    
    private GravityFieldEffectNew _blackHole;
    
    void Start()
    {
        // Убеждаемся, что система эффектов инициализирована
        if (FieldEffectSystem.Instance == null)
        {
            Debug.LogError("[BlackHoleTest] FieldEffectSystem не найдена!");
            return;
        }
        
        CreateBlackHole();
    }
    
    void CreateBlackHole()
    {
        Debug.Log("[BlackHoleTest] Создаем черную дыру для тестирования");
        
        // Создаем объект черной дыры
        GameObject blackHoleObj = new GameObject("Test Black Hole");
        blackHoleObj.transform.position = transform.position;
        
        // Добавляем компонент эффекта гравитации
        _blackHole = blackHoleObj.AddComponent<GravityFieldEffectNew>();
        
        // Настраиваем параметры черной дыры
        _blackHole.SetStrength(_strength);
        _blackHole.SetRadius(_radius);
        _blackHole.SetBlackHoleEffect(true, _eventHorizonRadius);
        _blackHole.SetRealisticGravity(true, 15f, 200f);
        
        Debug.Log($"[BlackHoleTest] Черная дыра создана в позиции {transform.position}");
        Debug.Log($"[BlackHoleTest] Параметры: сила={_strength}, радиус={_radius}, горизонт событий={_eventHorizonRadius}");
    }
    
    void Update()
    {
        // Отладочная информация
        if (_blackHole != null && Time.frameCount % 60 == 0) // Каждую секунду
        {
            var targets = FindObjectsOfType<WandererNew>();
            int absorbedCount = 0;
            int nearbyCount = 0;
            
            foreach (var target in targets)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < _radius)
                {
                    nearbyCount++;
                    if (distance < _eventHorizonRadius)
                    {
                        absorbedCount++;
                    }
                }
            }
            
            Debug.Log($"[BlackHoleTest] Статистика: {nearbyCount} персонажей в зоне, {absorbedCount} в горизонте событий");
        }
    }
    
    void OnDrawGizmos()
    {
        // Рисуем зону эффекта
        Gizmos.color = Color.yellow;
        DrawCircle(transform.position, _radius);
        
        // Рисуем горизонт событий
        Gizmos.color = Color.red;
        DrawCircle(transform.position, _eventHorizonRadius);
        
        // Рисуем центр
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
    
    private void DrawCircle(Vector3 center, float radius)
    {
        const int segments = 32;
        float angleStep = 2f * Mathf.PI / segments;
        
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
} 