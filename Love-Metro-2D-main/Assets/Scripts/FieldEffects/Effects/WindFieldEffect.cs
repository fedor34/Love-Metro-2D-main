using UnityEngine;

/// <summary>
/// Эффект ветра - создает постоянную силу в определенном направлении
/// </summary>
public class WindFieldEffect : BaseFieldEffect
{
    [Header("Параметры ветра")]
    [SerializeField] private Vector2 _windDirection = Vector2.right;
    [SerializeField] private float _windStrength = 10f;
    [SerializeField] private bool _useRandomFluctuations = true;
    [SerializeField] private float _fluctuationIntensity = 0.3f;
    [SerializeField] private float _fluctuationSpeed = 2f;
    
    [Header("Визуальные эффекты")]
    [SerializeField] private bool _showWindDirection = true;
    [SerializeField] private Color _windColor = Color.cyan;
    
    [Header("Турбулентность")]
    [SerializeField] private bool _useTurbulence = true;
    [SerializeField] private float _turbulenceFrequency = 1f;
    [SerializeField] private float _turbulenceStrength = 1f;
    
    private float _fluctuationTime = 0f;
    private Vector2 _normalizedDirection;
    
    protected override FieldEffectData CreateDefaultEffectData()
    {
        return new FieldEffectData
        {
            effectType = FieldEffectType.Wind,
            strength = _windStrength,
            radius = 10f,
            center = transform.position,
            affectedLayers = LayerMask.GetMask("Default")
        };
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Нормализуем направление ветра
        _normalizedDirection = _windDirection.normalized;
        
        // Настройка данных эффекта
        if (_effectData.effectType != FieldEffectType.Wind)
        {
            _effectData.effectType = FieldEffectType.Wind;
            _effectData.strength = _windStrength;
            _effectData.affectedLayers = LayerMask.GetMask("Default");
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Обновляем время для флуктуаций
        _fluctuationTime += Time.deltaTime * _fluctuationSpeed;
    }
    
    protected override void OnUpdateEffect()
    {
        // Обновляем данные эффекта
        _effectData.direction = _windDirection;
        _effectData.strength = _windStrength;
    }
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        if (!IsActive || target == null) return;
        
        Vector3 targetPosition = target.GetPosition();
        if (!IsInEffectZone(targetPosition)) return;
        
        // Вычисляем силу ветра с учетом расстояния
        float distance = Vector3.Distance(transform.position, targetPosition);
        float effectiveStrength = GetEffectStrengthAtDistance(distance);
        
        // Ограничиваем максимальную силу ветра для предотвращения багов
        float clampedWindStrength = Mathf.Clamp(_windStrength, 0f, 100f);
        Vector2 windForce = _windDirection * effectiveStrength * clampedWindStrength;
        
        // Применяем турбулентность если включена
        if (_useTurbulence)
        {
            float turbulenceX = Mathf.PerlinNoise(Time.time * _turbulenceFrequency, targetPosition.y * 0.1f) - 0.5f;
            float turbulenceY = Mathf.PerlinNoise(targetPosition.x * 0.1f, Time.time * _turbulenceFrequency) - 0.5f;
            
            Vector2 turbulence = new Vector2(turbulenceX, turbulenceY) * _turbulenceStrength;
            windForce += turbulence;
        }
        
        target.ApplyFieldForce(windForce, _effectData.effectType);
    }
    
    public override void RemoveEffect(IFieldEffectTarget target)
    {
        // Ветер не требует специального удаления эффекта
        // Просто перестаем применять силу
    }
    
    /// <summary>
    /// Устанавливает направление ветра
    /// </summary>
    public void SetWindDirection(Vector2 direction)
    {
        _windDirection = direction;
        _normalizedDirection = direction.normalized;
    }
    
    /// <summary>
    /// Устанавливает силу ветра
    /// </summary>
    public void SetWindStrength(float strength)
    {
        // Ограничиваем силу ветра разумными пределами
        _windStrength = Mathf.Clamp(strength, 0f, 100f);
        _effectData.strength = _windStrength;
        
        if (strength > 100f)
        {
            Debug.LogWarning($"[WindFieldEffect] Сила ветра {strength} слишком большая! Ограничена до {_windStrength}");
        }
    }
    
    /// <summary>
    /// Включает/выключает флуктуации ветра
    /// </summary>
    public void SetFluctuations(bool enable, float intensity = 0.3f, float speed = 2f)
    {
        _useRandomFluctuations = enable;
        _fluctuationIntensity = intensity;
        _fluctuationSpeed = speed;
    }
    
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        if (!_showWindDirection) return;
        
        // Рисуем направление ветра
        Gizmos.color = _windColor;
        Vector3 center = transform.position;
        Vector3 direction = new Vector3(_normalizedDirection.x, _normalizedDirection.y, 0);
        
        // Основная стрелка
        Gizmos.DrawRay(center, direction * 2f);
        
        // Наконечник стрелки
        Vector3 arrowHead1 = direction * 1.5f + Vector3.Cross(direction, Vector3.forward) * 0.3f;
        Vector3 arrowHead2 = direction * 1.5f + Vector3.Cross(direction, Vector3.back) * 0.3f;
        
        Gizmos.DrawLine(center + direction * 2f, center + arrowHead1);
        Gizmos.DrawLine(center + direction * 2f, center + arrowHead2);
        
        // Показываем зону действия
        if (_effectData.radius > 0)
        {
            Gizmos.color = new Color(_windColor.r, _windColor.g, _windColor.b, 0.2f);
            Gizmos.DrawSphere(center, _effectData.radius);
        }
    }
    
    protected virtual string GetEffectInfo()
    {
        string info = $"Wind Effect - {gameObject.name}";
        info += $"\nWind Direction: {_windDirection}";
        info += $"\nWind Strength: {_windStrength:F1}";
        
        if (_useRandomFluctuations)
        {
            info += $"\nFluctuations: {_fluctuationIntensity:F1}x at {_fluctuationSpeed:F1}Hz";
        }
        
        return info;
    }
} 