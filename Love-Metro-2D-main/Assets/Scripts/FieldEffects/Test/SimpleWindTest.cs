using UnityEngine;

/// <summary>
/// Простой тест ветра - принудительно применяет ветер к ближайшему пассажиру
/// </summary>
public class SimpleWindTest : MonoBehaviour
{
    [Header("Прямое тестирование ветра")]
    [SerializeField] private KeyCode _testKey = KeyCode.T;
    [SerializeField] private Vector2 _windForce = new Vector2(15f, 0f);
    [SerializeField] private float _testRadius = 10f;

    void Update()
    {
        if (Input.GetKeyDown(_testKey))
        {
            TestWindDirectly();
        }
    }

    private void TestWindDirectly()
    {
        Debug.Log("=== ПРЯМОЙ ТЕСТ ВЕТРА ===");
        
        // Находим всех пассажиров рядом
        var passengers = FindObjectsOfType<Passenger>();
        Passenger closestPassenger = null;
        float minDistance = float.MaxValue;
        
        foreach (var passenger in passengers)
        {
            float distance = Vector3.Distance(transform.position, passenger.transform.position);
            if (distance < _testRadius && distance < minDistance)
            {
                minDistance = distance;
                closestPassenger = passenger;
            }
        }
        
        if (closestPassenger == null)
        {
            Debug.LogWarning($"[SimpleWindTest] Нет пассажиров в радиусе {_testRadius}!");
            return;
        }
        
        Debug.Log($"[SimpleWindTest] Тестирую ветер на {closestPassenger.name} (расстояние: {minDistance:F1})");
        Debug.Log($"[SimpleWindTest] Применяю силу: {_windForce}");
        
        // Проверяем, может ли пассажир быть затронут ветром
        bool canBeAffected = closestPassenger.CanBeAffectedBy(FieldEffectType.Wind);
        Debug.Log($"[SimpleWindTest] CanBeAffectedBy(Wind): {canBeAffected}");
        
        if (!canBeAffected)
        {
            Debug.LogError($"[SimpleWindTest] Пассажир {closestPassenger.name} НЕ МОЖЕТ быть затронут ветром!");
            return;
        }
        
        // Прямое применение ветра
        try
        {
            closestPassenger.ApplyFieldForce(_windForce, FieldEffectType.Wind);
            Debug.Log($"[SimpleWindTest] ✅ Ветер применен к {closestPassenger.name}!");
            
            // Проверяем velocity через секунду
            StartCoroutine(CheckVelocityAfterDelay(closestPassenger));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SimpleWindTest] ❌ Ошибка применения ветра: {ex.Message}");
        }
    }
    
    private System.Collections.IEnumerator CheckVelocityAfterDelay(Passenger passenger)
    {
        yield return new WaitForSeconds(0.1f);
        
        if (passenger != null)
        {
            var rb = passenger.GetRigidbody();
            if (rb != null)
            {
                Debug.Log($"[SimpleWindTest] Velocity через 0.1с: {rb.velocity} (magnitude: {rb.velocity.magnitude:F1})");
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Рисуем зону тестирования
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _testRadius);
        
        // Рисуем направление ветра
        Gizmos.color = Color.cyan;
        Vector3 windDirection = new Vector3(_windForce.x, _windForce.y, 0).normalized;
        Gizmos.DrawRay(transform.position, windDirection * 3f);
        
        // Стрелка
        Vector3 arrowPos = transform.position + windDirection * 3f;
        Vector3 arrowLeft = arrowPos + Quaternion.Euler(0, 0, 135) * windDirection * 0.5f;
        Vector3 arrowRight = arrowPos + Quaternion.Euler(0, 0, -135) * windDirection * 0.5f;
        Gizmos.DrawLine(arrowPos, arrowLeft);
        Gizmos.DrawLine(arrowPos, arrowRight);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 60), 
            $"Простой тест ветра:\n" +
            $"Нажмите '{_testKey}' для применения ветра {_windForce}\n" +
            $"Радиус тестирования: {_testRadius}");
    }
} 