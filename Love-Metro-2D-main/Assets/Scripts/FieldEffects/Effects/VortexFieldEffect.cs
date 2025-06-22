using UnityEngine;

/// <summary>
/// Эффект вихря - закручивающая сила вокруг центра
/// </summary>
public class VortexFieldEffect : BaseFieldEffect
{
    [Header("Вихрь")]
    [SerializeField] private bool _clockwise = true;
    [SerializeField] private float _spiralStrength = 2f;
    [SerializeField] private float _inwardPull = 1f;
    [SerializeField] private bool _eyeOfStorm = true;
    [SerializeField] private float _eyeRadius = 1f;
    [SerializeField] private bool _variableSpeed = true;
    [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    protected override FieldEffectData CreateDefaultEffectData()
    {
        return new FieldEffectData(FieldEffectType.Vortex, 4f, 12f, transform.position);
    }
    
    protected override void InitializeEffectData()
    {
        base.InitializeEffectData();
        
        _gizmoColor = Color.blue;
        _effectData.effectColor = Color.blue;
        _effectData.forceMode = ForceMode2D.Force;
        _effectData.respectMass = false;
    }
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        if (!_isActive || target == null) return;
        
        Vector3 targetPos = target.GetPosition();
        Vector3 centerPos = transform.position;
        Vector3 toTarget = targetPos - centerPos;
        float distance = toTarget.magnitude;
        
        if (distance > _effectData.radius) return;
        if (!target.CanBeAffectedBy(_effectData.effectType)) return;
        
        // Проверяем глаз бури
        if (_eyeOfStorm && distance < _eyeRadius)
        {
            ApplyEyeEffect(target);
            return;
        }
        
        Vector3 vortexForce = CalculateVortexForce(toTarget, distance);
        target.ApplyFieldForce(vortexForce, _effectData.forceMode);
    }
    
    private Vector3 CalculateVortexForce(Vector3 toTarget, float distance)
    {
        float baseStrength = _effectData.GetEffectiveStrength(distance);
        
        // Тангенциальная (вращательная) составляющая
        Vector3 tangential = Vector3.Cross(toTarget.normalized, Vector3.forward);
        if (!_clockwise) tangential = -tangential;
        
        // Применяем кривую скорости
        if (_variableSpeed)
        {
            float normalizedDistance = distance / _effectData.radius;
            float speedMultiplier = _speedCurve.Evaluate(normalizedDistance);
            baseStrength *= speedMultiplier;
        }
        
        Vector3 tangentialForce = tangential * baseStrength * _spiralStrength;
        
        // Радиальная (втягивающая) составляющая
        Vector3 radialForce = Vector3.zero;
        if (_inwardPull > 0)
        {
            Vector3 toCenter = -toTarget.normalized;
            radialForce = toCenter * baseStrength * _inwardPull;
        }
        
        return tangentialForce + radialForce;
    }
    
    private void ApplyEyeEffect(IFieldEffectTarget target)
    {
        // В глазу бури объекты могут испытывать слабые случайные силы
        Vector3 randomForce = new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(-0.5f, 0.5f),
            0f
        );
        
        target.ApplyFieldForce(randomForce, ForceMode2D.Impulse);
    }
    
    protected override void DrawEffectRadius()
    {
        base.DrawEffectRadius();
        
        if (_effectData == null) return;
        
        // Рисуем глаз бури
        if (_eyeOfStorm)
        {
            Gizmos.color = Color.white;
            DrawCircle(transform.position, _eyeRadius);
            Gizmos.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.1f);
            Gizmos.DrawSphere(transform.position, _eyeRadius);
        }
        
        // Рисуем спираль вихря
        DrawVortexSpiral();
        
        // Рисуем векторы силы для целей
        DrawForceVectors();
    }
    
    private void DrawVortexSpiral()
    {
        Gizmos.color = _gizmoColor;
        Vector3 center = transform.position;
        
        // Рисуем спиральные линии
        int spiralSegments = 64;
        float maxRadius = _effectData.radius;
        
        for (int spiral = 0; spiral < 3; spiral++)
        {
            Vector3 lastPoint = Vector3.zero;
            
            for (int i = 0; i <= spiralSegments; i++)
            {
                float t = (float)i / spiralSegments;
                float angle = t * 4f * Mathf.PI + spiral * 2f * Mathf.PI / 3f;
                if (!_clockwise) angle = -angle;
                
                float radius = Mathf.Lerp(_eyeRadius, maxRadius, t);
                
                Vector3 point = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f
                );
                
                if (i > 0)
                {
                    Gizmos.DrawLine(lastPoint, point);
                }
                
                lastPoint = point;
            }
        }
    }
    
    private void DrawForceVectors()
    {
        if (_currentTargets.Count == 0) return;
        
        Gizmos.color = Color.red;
        foreach (var target in _currentTargets)
        {
            if (target != null)
            {
                Vector3 targetPos = target.GetPosition();
                Vector3 toTarget = targetPos - transform.position;
                float distance = toTarget.magnitude;
                
                if (distance > _eyeRadius) // Не рисуем в глазу бури
                {
                    Vector3 force = CalculateVortexForce(toTarget, distance);
                    Vector3 forceEnd = targetPos + force.normalized * Mathf.Min(force.magnitude * 0.1f, 2f);
                    Gizmos.DrawLine(targetPos, forceEnd);
                    
                    // Рисуем стрелку
                    Vector3 arrowDir = (forceEnd - targetPos).normalized;
                    Vector3 arrowSide = Vector3.Cross(arrowDir, Vector3.forward) * 0.2f;
                    Gizmos.DrawLine(forceEnd, forceEnd - arrowDir * 0.3f + arrowSide);
                    Gizmos.DrawLine(forceEnd, forceEnd - arrowDir * 0.3f - arrowSide);
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
            string info = $"Vortex Effect\n";
            info += $"Direction: {(_clockwise ? "Clockwise" : "Counter-clockwise")}\n";
            info += $"Spiral: {_spiralStrength:F1} | Pull: {_inwardPull:F1}\n";
            
            if (_eyeOfStorm)
            {
                info += $"Eye Radius: {_eyeRadius:F1}\n";
            }
            
            info += $"Targets: {_currentTargets.Count}";
            
            UnityEditor.Handles.Label(labelPos, info);
        }
#endif
    }
    
    #region Public Interface
    
    /// <summary>
    /// Изменить направление вращения
    /// </summary>
    public void SetClockwise(bool clockwise)
    {
        _clockwise = clockwise;
    }
    
    /// <summary>
    /// Настроить параметры вихря
    /// </summary>
    public void SetVortexParameters(float spiralStrength, float inwardPull)
    {
        _spiralStrength = spiralStrength;
        _inwardPull = inwardPull;
    }
    
    /// <summary>
    /// Настроить глаз бури
    /// </summary>
    public void SetEyeOfStorm(bool enable, float eyeRadius = 1f)
    {
        _eyeOfStorm = enable;
        _eyeRadius = eyeRadius;
    }
    
    /// <summary>
    /// Установить кривую скорости вращения
    /// </summary>
    public void SetSpeedCurve(AnimationCurve curve, bool variableSpeed = true)
    {
        _speedCurve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
        _variableSpeed = variableSpeed;
    }
    
    /// <summary>
    /// Получить вращательную скорость в точке
    /// </summary>
    public float GetRotationalSpeedAtPoint(Vector3 point)
    {
        Vector3 toPoint = point - transform.position;
        float distance = toPoint.magnitude;
        
        if (distance > _effectData.radius || distance < _eyeRadius) return 0f;
        
        float baseStrength = _effectData.GetEffectiveStrength(distance);
        
        if (_variableSpeed)
        {
            float normalizedDistance = distance / _effectData.radius;
            baseStrength *= _speedCurve.Evaluate(normalizedDistance);
        }
        
        return baseStrength * _spiralStrength * (_clockwise ? 1f : -1f);
    }
    
    /// <summary>
    /// Получить полную силу вихря в точке
    /// </summary>
    public Vector3 GetVortexForceAtPoint(Vector3 point)
    {
        Vector3 toPoint = point - transform.position;
        float distance = toPoint.magnitude;
        
        if (distance > _effectData.radius) return Vector3.zero;
        if (_eyeOfStorm && distance < _eyeRadius) return Vector3.zero;
        
        return CalculateVortexForce(toPoint, distance);
    }
    
    #endregion
} 