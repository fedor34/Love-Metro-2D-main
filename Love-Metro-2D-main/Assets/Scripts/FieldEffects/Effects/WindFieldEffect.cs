using UnityEngine;

/// <summary>
/// Эффект ветра - постоянная сила в определенном направлении
/// </summary>
public class WindFieldEffect : BaseFieldEffect
{
    [Header("Ветер")]
    [SerializeField] private Vector2 _windDirection = Vector2.right;
    [SerializeField] private bool _turbulence = false;
    [SerializeField] private float _turbulenceStrength = 0.5f;
    [SerializeField] private float _turbulenceFrequency = 2f;
    [SerializeField] private bool _affectBasedOnSize = true;
    [SerializeField] private AnimationCurve _gustPattern = AnimationCurve.Constant(0, 1, 1);
    [SerializeField] private bool _rotateWithWind = false;
    
    private float _turbulenceTime;
    
    protected override FieldEffectData CreateDefaultEffectData()
    {
        var data = new FieldEffectData(FieldEffectType.Wind, 3f, 8f, transform.position);
        data.direction = _windDirection;
        return data;
    }
    
    protected override void InitializeEffectData()
    {
        base.InitializeEffectData();
        
        _gizmoColor = Color.cyan;
        _effectData.effectColor = Color.cyan;
        _effectData.forceMode = ForceMode2D.Force;
        _effectData.respectMass = false;
        _effectData.direction = _windDirection;
    }
    
    protected override void OnUpdateEffect()
    {
        base.OnUpdateEffect();
        
        _turbulenceTime += Time.deltaTime * _turbulenceFrequency;
        _effectData.direction = _windDirection.normalized;
    }
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        if (!_isActive || target == null) return;
        
        Vector3 targetPos = target.GetPosition();
        float distance = Vector3.Distance(transform.position, targetPos);
        
        if (distance > _effectData.radius) return;
        if (!target.CanBeAffectedBy(_effectData.effectType)) return;
        
        Vector3 windForce = CalculateWindForce(target, distance);
        target.ApplyFieldForce(windForce, _effectData.forceMode);
        
        // Применяем вращение если нужно
        if (_rotateWithWind)
        {
            ApplyWindRotation(target, windForce.magnitude);
        }
    }
    
    private Vector3 CalculateWindForce(IFieldEffectTarget target, float distance)
    {
        float baseStrength = _effectData.GetEffectiveStrength(distance);
        Vector3 windDirection = _effectData.direction;
        
        // Применяем паттерн порывов
        float gustMultiplier = _gustPattern.Evaluate(Time.time % 1f);
        baseStrength *= gustMultiplier;
        
        // Добавляем турбулентность
        if (_turbulence)
        {
            Vector3 turbulenceVector = GetTurbulenceVector();
            windDirection += turbulenceVector * _turbulenceStrength;
        }
        
        // Учитываем размер объекта
        if (_affectBasedOnSize)
        {
            float sizeMultiplier = GetTargetSizeMultiplier(target);
            baseStrength *= sizeMultiplier;
        }
        
        return windDirection.normalized * baseStrength;
    }
    
    private Vector3 GetTurbulenceVector()
    {
        float noiseX = Mathf.PerlinNoise(_turbulenceTime, 0f) * 2f - 1f;
        float noiseY = Mathf.PerlinNoise(0f, _turbulenceTime) * 2f - 1f;
        return new Vector3(noiseX, noiseY, 0f);
    }
    
    private float GetTargetSizeMultiplier(IFieldEffectTarget target)
    {
        if (target is MonoBehaviour mb)
        {
            var collider = mb.GetComponent<Collider2D>();
            if (collider != null)
            {
                float area = collider.bounds.size.x * collider.bounds.size.y;
                return Mathf.Sqrt(area); // Площадь влияет на силу ветра
            }
        }
        return 1f;
    }
    
    private void ApplyWindRotation(IFieldEffectTarget target, float windStrength)
    {
        if (target is MonoBehaviour mb)
        {
            var rb = mb.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Вращение пропорционально силе ветра
                float torque = windStrength * 0.1f * (Random.value - 0.5f);
                rb.AddTorque(torque);
            }
        }
    }
    
    protected override void DrawEffectRadius()
    {
        base.DrawEffectRadius();
        
        // Рисуем направление ветра
        if (_effectData != null)
        {
            Gizmos.color = Color.white;
            Vector3 center = transform.position;
            Vector3 direction = _effectData.direction.normalized;
            
            // Рисуем стрелку направления
            Vector3 arrowEnd = center + direction * _effectData.radius * 0.7f;
            Gizmos.DrawLine(center, arrowEnd);
            
            // Рисуем наконечник стрелки
            Vector3 arrowHead1 = arrowEnd - direction * 0.5f + Vector3.Cross(direction, Vector3.forward) * 0.3f;
            Vector3 arrowHead2 = arrowEnd - direction * 0.5f - Vector3.Cross(direction, Vector3.forward) * 0.3f;
            Gizmos.DrawLine(arrowEnd, arrowHead1);
            Gizmos.DrawLine(arrowEnd, arrowHead2);
            
            // Рисуем линии потока ветра
            DrawWindLines();
        }
    }
    
    private void DrawWindLines()
    {
        if (_effectData == null) return;
        
        Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.3f);
        Vector3 center = transform.position;
        Vector3 direction = _effectData.direction.normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward);
        
        // Рисуем несколько линий потока
        for (int i = -2; i <= 2; i++)
        {
            Vector3 lineStart = center + perpendicular * i * 0.5f - direction * _effectData.radius * 0.8f;
            Vector3 lineEnd = center + perpendicular * i * 0.5f + direction * _effectData.radius * 0.8f;
            Gizmos.DrawLine(lineStart, lineEnd);
        }
    }
    
    protected override void DrawEffectInfo()
    {
        base.DrawEffectInfo();
        
#if UNITY_EDITOR
        if (_effectData != null)
        {
            var labelPos = transform.position + Vector3.up * (_effectData.radius + 2f);
            string info = $"Wind Effect\nDirection: {_windDirection}\nStrength: {_effectData.strength:F1}\n";
            
            if (_turbulence)
            {
                info += $"Turbulence: {_turbulenceStrength:F1}\n";
            }
            
            info += $"Targets: {_currentTargets.Count}";
            
            UnityEditor.Handles.Label(labelPos, info);
        }
#endif
    }
    
    #region Public Interface
    
    /// <summary>
    /// Установить направление ветра
    /// </summary>
    public void SetWindDirection(Vector2 direction)
    {
        _windDirection = direction.normalized;
        if (_effectData != null)
        {
            _effectData.direction = _windDirection;
        }
    }
    
    /// <summary>
    /// Настроить турбулентность
    /// </summary>
    public void SetTurbulence(bool enable, float strength = 0.5f, float frequency = 2f)
    {
        _turbulence = enable;
        _turbulenceStrength = strength;
        _turbulenceFrequency = frequency;
    }
    
    /// <summary>
    /// Установить паттерн порывов
    /// </summary>
    public void SetGustPattern(AnimationCurve pattern)
    {
        _gustPattern = pattern ?? AnimationCurve.Constant(0, 1, 1);
    }
    
    /// <summary>
    /// Получить силу ветра в точке
    /// </summary>
    public Vector3 GetWindForceAtPoint(Vector3 point)
    {
        float distance = Vector3.Distance(transform.position, point);
        if (distance > _effectData.radius) return Vector3.zero;
        
        float strength = _effectData.GetEffectiveStrength(distance);
        Vector3 direction = _effectData.direction;
        
        if (_turbulence)
        {
            direction += GetTurbulenceVector() * _turbulenceStrength;
        }
        
        return direction.normalized * strength;
    }
    
    #endregion
} 