using UnityEngine;
using System.Linq;

/// <summary>
/// Диагностический скрипт для проверки системы ветра
/// </summary>
public class WindDiagnostic : MonoBehaviour
{
    [Header("Диагностика")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private bool _autoCreateWindForTest = true;
    
    [Header("Тестовый ветер")]
    [SerializeField] private Vector2 _testWindDirection = Vector2.right;
    [SerializeField] private float _testWindStrength = 12f;
    [SerializeField] private float _testWindRadius = 8f;
    
    private WindFieldEffect _testWind;
    private FieldEffectSystem _system;

    void Start()
    {
        _system = FieldEffectSystem.Instance;
        
        if (_autoCreateWindForTest)
        {
            CreateTestWind();
        }
        
        // Диагностика системы
        DiagnoseSystem();
    }

    void Update()
    {
        if (_showDebugInfo)
        {
            ShowRuntimeInfo();
        }
        
        // Тестовые клавиши
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DiagnoseSystem();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            CreateTestWind();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            DiagnosePassengers();
        }
    }

    private void CreateTestWind()
    {
        if (_testWind != null)
        {
            Debug.Log("[WindDiagnostic] Удаляю существующий тестовый ветер");
            DestroyImmediate(_testWind.gameObject);
        }
        
        GameObject windObj = new GameObject("TestWind_Diagnostic");
        windObj.transform.position = transform.position;
        
        _testWind = windObj.AddComponent<WindFieldEffect>();
        _testWind.SetWindDirection(_testWindDirection);
        _testWind.SetWindStrength(_testWindStrength);
        
        var effectData = _testWind.GetEffectData();
        effectData.radius = _testWindRadius;
        
        Debug.Log($"[WindDiagnostic] Создан тестовый ветер: сила={_testWindStrength}, радиус={_testWindRadius}, активен={_testWind.IsActive}");
    }

    private void DiagnoseSystem()
    {
        Debug.Log("=== ДИАГНОСТИКА ВЕТРОВОЙ СИСТЕМЫ ===");
        
        // 1. Проверка FieldEffectSystem
        if (_system == null)
        {
            Debug.LogError("[WindDiagnostic] ❌ FieldEffectSystem.Instance == null!");
            return;
        }
        else
        {
            Debug.Log($"[WindDiagnostic] ✅ FieldEffectSystem найден. Эффектов: {_system.GetTotalEffectsCount()}, Целей: {_system.GetTargetsCount()}");
        }
        
        // 2. Проверка ветровых эффектов
        var windEffects = _system.GetEffectsByType(FieldEffectType.Wind);
        Debug.Log($"[WindDiagnostic] Найдено ветровых эффектов: {windEffects.Count}");
        
        foreach (var effect in windEffects)
        {
            if (effect is WindFieldEffect wind)
            {
                var data = wind.GetEffectData();
                Debug.Log($"[WindDiagnostic] - Ветер '{wind.name}': активен={wind.IsActive}, сила={data.strength}, радиус={data.radius}");
            }
        }
        
        // 3. Проверка пассажиров
        var passengers = FindObjectsOfType<Passenger>();
        Debug.Log($"[WindDiagnostic] Найдено пассажиров: {passengers.Length}");
        
        int flyingCount = 0;
        foreach (var passenger in passengers)
        {
            if (passenger.name.Contains("Flying") || passenger.transform.position.y > 1f) // Примерная проверка полета
            {
                flyingCount++;
            }
        }
        Debug.Log($"[WindDiagnostic] Пассажиров в полете: {flyingCount}");
        
        Debug.Log("=== КОНЕЦ ДИАГНОСТИКИ ===");
    }

    private void DiagnosePassengers()
    {
        Debug.Log("=== ДИАГНОСТИКА ПАССАЖИРОВ ===");
        
        var passengers = FindObjectsOfType<Passenger>();
        foreach (var passenger in passengers)
        {
            string state = "Unknown";
            bool canBeAffected = passenger.CanBeAffectedBy(FieldEffectType.Wind);
            Vector3 pos = passenger.GetPosition();
            
            Debug.Log($"[WindDiagnostic] Пассажир '{passenger.name}': позиция={pos}, может_быть_затронут_ветром={canBeAffected}");
        }
        
        Debug.Log("=== КОНЕЦ ДИАГНОСТИКИ ПАССАЖИРОВ ===");
    }

    private void ShowRuntimeInfo()
    {
        // Показываем информацию в правом верхнем углу экрана
        if (_system != null)
        {
            string info = $"FieldEffectSystem: {_system.GetTotalEffectsCount()} эффектов, {_system.GetTargetsCount()} целей\n";
            
            var windEffects = _system.GetEffectsByType(FieldEffectType.Wind);
            info += $"Ветровых эффектов: {windEffects.Count}\n";
            
            if (_testWind != null)
            {
                info += $"Тестовый ветер: активен={_testWind.IsActive}, цели={_testWind.GetCurrentTargets().Count}";
            }
            
            // Рисуем текст на экране
            var style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            
            GUI.Label(new Rect(Screen.width - 300, 10, 290, 100), info, style);
        }
    }

    private void OnDrawGizmos()
    {
        if (_testWind != null)
        {
            // Рисуем зону тестового ветра
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_testWind.transform.position, _testWindRadius);
            
            // Рисуем направление ветра
            Gizmos.color = Color.white;
            Vector3 direction = new Vector3(_testWindDirection.x, _testWindDirection.y, 0).normalized;
            Gizmos.DrawRay(_testWind.transform.position, direction * 3f);
            
            // Рисуем стрелку
            Vector3 arrowPos = _testWind.transform.position + direction * 3f;
            Vector3 arrowLeft = arrowPos + Quaternion.Euler(0, 0, 135) * direction * 0.5f;
            Vector3 arrowRight = arrowPos + Quaternion.Euler(0, 0, -135) * direction * 0.5f;
            Gizmos.DrawLine(arrowPos, arrowLeft);
            Gizmos.DrawLine(arrowPos, arrowRight);
        }
        
        // Показываем всех пассажиров
        var passengers = FindObjectsOfType<Passenger>();
        foreach (var passenger in passengers)
        {
            bool canBeAffected = passenger.CanBeAffectedBy(FieldEffectType.Wind);
            Gizmos.color = canBeAffected ? Color.green : Color.red;
            Gizmos.DrawWireCube(passenger.transform.position, Vector3.one * 0.5f);
        }
    }

    private void OnGUI()
    {
        if (!_showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));
        GUILayout.Label("=== УПРАВЛЕНИЕ ДИАГНОСТИКОЙ ===");
        GUILayout.Label("F1 - Полная диагностика системы");
        GUILayout.Label("F2 - Создать тестовый ветер");
        GUILayout.Label("F3 - Диагностика пассажиров");
        
        if (_testWind != null)
        {
            GUILayout.Label($"Тестовый ветер: {(_testWind.IsActive ? "АКТИВЕН" : "НЕАКТИВЕН")}");
            if (GUILayout.Button("Переключить ветер"))
            {
                _testWind.SetActive(!_testWind.IsActive);
            }
        }
        GUILayout.EndArea();
    }
} 