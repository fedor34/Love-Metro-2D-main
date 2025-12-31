using UnityEngine;

/// <summary>
/// Тестовый скрипт для демонстрации режима полета пассажиров
/// </summary>
public class FlyingTest : MonoBehaviour
{
    [Header("Параметры тестирования")]
    [SerializeField] private bool _createWindOnStart = true;
    [SerializeField] private Vector2 _windDirection = Vector2.right;
    [SerializeField] private float _windStrength = 10f;
    [SerializeField] private float _windRadius = 15f;
    
    [Header("Тестирование в рантайме")]
    [SerializeField] private KeyCode _toggleWindKey = KeyCode.F;
    [SerializeField] private KeyCode _increaseWindKey = KeyCode.R;
    [SerializeField] private KeyCode _decreaseWindKey = KeyCode.T;
    
    private WindFieldEffect _windEffect;

    void Start()
    {
        if (_createWindOnStart)
        {
            CreateWindEffect();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(_toggleWindKey))
        {
            ToggleWind();
        }
        
        if (Input.GetKeyDown(_increaseWindKey))
        {
            IncreaseWindStrength();
        }
        
        if (Input.GetKeyDown(_decreaseWindKey))
        {
            DecreaseWindStrength();
        }
    }

    private void CreateWindEffect()
    {
        if (_windEffect == null)
        {
            GameObject windObj = new GameObject("Flying Test Wind");
            windObj.transform.position = transform.position;
            
            _windEffect = windObj.AddComponent<WindFieldEffect>();
            _windEffect.SetWindDirection(_windDirection);
            _windEffect.SetWindStrength(_windStrength);
            _windEffect.SetFluctuations(true, 0.2f, 1.5f);
            
            var effectData = _windEffect.GetEffectData();
            effectData.radius = _windRadius;
            
            Debug.Log($"Создан ветровой эффект силой {_windStrength}. Пассажиры будут переходить в режим полета при силе ветра ≥ 8!");
        }
    }

    private void ToggleWind()
    {
        if (_windEffect == null)
        {
            CreateWindEffect();
        }
        else
        {
            _windEffect.SetActive(!_windEffect.IsActive);
            Debug.Log($"Ветер {(_windEffect.IsActive ? "включен" : "выключен")}");
        }
    }

    private void IncreaseWindStrength()
    {
        if (_windEffect != null)
        {
            _windStrength = Mathf.Min(_windStrength + 2f, 30f);
            _windEffect.SetWindStrength(_windStrength);
            Debug.Log($"Сила ветра увеличена до {_windStrength}");
        }
    }

    private void DecreaseWindStrength()
    {
        if (_windEffect != null)
        {
            _windStrength = Mathf.Max(_windStrength - 2f, 0f);
            _windEffect.SetWindStrength(_windStrength);
            Debug.Log($"Сила ветра уменьшена до {_windStrength}");
        }
    }

    private void OnDrawGizmos()
    {
        if (_windEffect != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _windRadius);
            
            // Показываем направление ветра
            Gizmos.color = Color.white;
            Vector3 direction = new Vector3(_windDirection.x, _windDirection.y, 0).normalized;
            Gizmos.DrawRay(transform.position, direction * 3f);
        }
    }
} 