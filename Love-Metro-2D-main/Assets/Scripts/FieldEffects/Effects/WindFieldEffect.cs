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
        
        Debug.Log($"[WindFieldEffect] Создан ветер: направление={_windDirection}, сила={_windStrength}");
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Обновляем время для флуктуаций
        _fluctuationTime += Time.deltaTime * _fluctuationSpeed;
    }
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        if (target == null) return;
        
        // Получаем расстояние до цели
        float distance = GetDistanceToTarget(target);
        
        // Вычисляем силу ветра на основе расстояния
        float effectiveStrength = _windStrength;
        
        // Уменьшаем силу с расстоянием (опционально)
        if (_effectData.radius > 0 && distance > 0)
        {
            float distanceFactor = 1f - (distance / _effectData.radius);
            effectiveStrength *= distanceFactor;
        }
        
        // Базовое направление ветра
        Vector2 windForce = _normalizedDirection * effectiveStrength;
        
        // Добавляем флуктуации если включены
        if (_useRandomFluctuations)
        {
            float fluctuation = Mathf.Sin(_fluctuationTime + distance) * _fluctuationIntensity;
            Vector2 perpendicular = new Vector2(-_normalizedDirection.y, _normalizedDirection.x);
            windForce += perpendicular * fluctuation * effectiveStrength;
        }
        
        // Применяем силу ветра с учетом deltaTime
        target.ApplyFieldForce(windForce * deltaTime, FieldEffectType.Wind);
        
        if (_showGizmos)
        {
            Debug.Log($"[WindFieldEffect] Применен ветер к {target}: сила={windForce.magnitude:F2}, направление={windForce.normalized}");
        }
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
        Debug.Log($"[WindFieldEffect] Направление ветра изменено на {direction}");
    }
    
    /// <summary>
    /// Устанавливает силу ветра
    /// </summary>
    public void SetWindStrength(float strength)
    {
        _windStrength = strength;
        _effectData.strength = strength;
        Debug.Log($"[WindFieldEffect] Сила ветра изменена на {strength}");
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