using UnityEngine;

/// <summary>
/// Эффект гравитации - притягивает объекты к центру
/// Новая версия на основе BaseFieldEffect
/// </summary>
public class GravityFieldEffectNew : BaseFieldEffect
{
    [Header("Гравитация")]
    [SerializeField] private bool _useRealisticGravity = false;
    [SerializeField] private float _gravitationalConstant = 6.67f;
    [SerializeField] private float _centralMass = 100f;
    [SerializeField] private bool _affectRotation = false;
    [SerializeField] private float _rotationStrength = 1f;
    [SerializeField] private bool _createBlackHoleEffect = false;
    [SerializeField] private float _eventHorizonRadius = 1f;
    
    protected override FieldEffectData CreateDefaultEffectData()
    {
        return new FieldEffectData(FieldEffectType.Gravity, 5f, 10f, transform.position);
    }
    
    protected override void InitializeEffectData()
    {
        base.InitializeEffectData();
        
        // Настройки специфичные для гравитации
        _gizmoColor = Color.yellow;
        _effectData.effectColor = Color.yellow;
        _effectData.forceMode = ForceMode2D.Force;
        _effectData.respectMass = true;
    }
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        if (!_isActive || target == null) return;
        
        Vector3 targetPos = target.GetPosition();
        Vector3 effectPos = transform.position;
        Vector3 direction = (effectPos - targetPos).normalized;
        float distance = Vector3.Distance(effectPos, targetPos);
        
        if (distance > _effectData.radius) return;
        
        // Проверяем, может ли эффект действовать на цель
        if (!target.CanBeAffectedBy(_effectData.effectType)) return;
        
        float effectiveStrength = CalculateGravityStrength(distance, target);
        Vector3 force = direction * effectiveStrength;
        
        // Специальная обработка для черной дыры
        if (_createBlackHoleEffect && distance < _eventHorizonRadius)
        {
            ApplyBlackHoleEffect(target, force);
        }
        else
        {
            target.ApplyFieldForce(force, _effectData.forceMode);
        }
        
        // Применяем вращение если нужно
        if (_affectRotation)
        {
            ApplyRotationalEffect(target, distance);
        }
    }
    
    private float CalculateGravityStrength(float distance, IFieldEffectTarget target)
    {
        float baseStrength = _effectData.GetEffectiveStrength(distance);
        
        if (_useRealisticGravity)
        {
            // F = G * m1 * m2 / r^2
            float targetMass = GetTargetMass(target);
            float gravity = (_gravitationalConstant * _centralMass * targetMass) / Mathf.Max(distance * distance, 0.1f);
            return gravity * baseStrength;
        }
        
        return baseStrength;
    }
    
    private float GetTargetMass(IFieldEffectTarget target)
    {
        // Пытаемся получить массу из Rigidbody2D
        if (target is MonoBehaviour mb)
        {
            var rb = mb.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                return rb.mass;
            }
        }
        
        return 1f; // Масса по умолчанию
    }
    
    private void ApplyBlackHoleEffect(IFieldEffectTarget target, Vector3 force)
    {
        // Увеличиваем силу внутри горизонта событий
        Vector3 extremeForce = force * 10f;
        target.ApplyFieldForce(extremeForce, ForceMode2D.Impulse);
        
        // Можно добавить визуальные эффекты растяжения
        // TODO: Добавить эффект спагеттификации
    }
    
    private void ApplyRotationalEffect(IFieldEffectTarget target, float distance)
    {
        if (target is MonoBehaviour mb)
        {
            var rb = mb.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Применяем вращательный момент
                float torque = _rotationStrength * (1f - distance / _effectData.radius);
                rb.AddTorque(torque);
            }
        }
    }
    
    protected override void DrawEffectRadius()
    {
        base.DrawEffectRadius();
        
        // Рисуем горизонт событий для черной дыры
        if (_createBlackHoleEffect)
        {
            Gizmos.color = Color.red;
            DrawCircle(transform.position, _eventHorizonRadius);
        }
        
        // Рисуем направления силы
        if (_currentTargets.Count > 0)
        {
            Gizmos.color = Color.green;
            foreach (var target in _currentTargets)
            {
                if (target != null)
                {
                    Vector3 targetPos = target.GetPosition();
                    Vector3 direction = (transform.position - targetPos).normalized;
                    float distance = Vector3.Distance(transform.position, targetPos);
                    float strength = CalculateGravityStrength(distance, target);
                    
                    // Рисуем стрелку пропорциональную силе
                    Vector3 arrowEnd = targetPos + direction * Mathf.Min(strength * 0.1f, 2f);
                    Gizmos.DrawLine(targetPos, arrowEnd);
                }
            }
        }
    }
    
    protected override void DrawEffectInfo()
    {
        base.DrawEffectInfo();
        
#if UNITY_EDITOR
        if (_effectData != null)
        {
            var labelPos = transform.position + Vector3.up * (_effectData.radius + 2f);
            string info = $"Gravity Effect\nStrength: {_effectData.strength:F1}\n";
            
            if (_useRealisticGravity)
            {
                info += $"Mass: {_centralMass:F1}\nG: {_gravitationalConstant:F2}\n";
            }
            
            if (_createBlackHoleEffect)
            {
                info += $"Black Hole\nEvent Horizon: {_eventHorizonRadius:F1}\n";
            }
            
            info += $"Targets: {_currentTargets.Count}";
            
            UnityEditor.Handles.Label(labelPos, info);
        }
#endif
    }
    
    #region Public Interface
    
    /// <summary>
    /// Включить/выключить реалистичную гравитацию
    /// </summary>
    public void SetRealisticGravity(bool realistic, float gravitationalConstant = 6.67f, float centralMass = 100f)
    {
        _useRealisticGravity = realistic;
        _gravitationalConstant = gravitationalConstant;
        _centralMass = centralMass;
    }
    
    /// <summary>
    /// Настроить эффект черной дыры
    /// </summary>
    public void SetBlackHoleEffect(bool enable, float eventHorizonRadius = 1f)
    {
        _createBlackHoleEffect = enable;
        _eventHorizonRadius = eventHorizonRadius;
    }
    
    /// <summary>
    /// Настройки вращения
    /// </summary>
    public void SetRotationalEffect(bool enable, float rotationStrength = 1f)
    {
        _affectRotation = enable;
        _rotationStrength = rotationStrength;
    }
    
    /// <summary>
    /// Получить информацию о гравитационном поле в точке
    /// </summary>
    public Vector3 GetGravityAtPoint(Vector3 point)
    {
        Vector3 direction = (transform.position - point).normalized;
        float distance = Vector3.Distance(transform.position, point);
        
        if (distance > _effectData.radius) return Vector3.zero;
        
        float strength = _effectData.GetEffectiveStrength(distance);
        
        if (_useRealisticGravity)
        {
            float gravity = (_gravitationalConstant * _centralMass) / Mathf.Max(distance * distance, 0.1f);
            strength *= gravity;
        }
        
        return direction * strength;
    }
    
    #endregion
} 