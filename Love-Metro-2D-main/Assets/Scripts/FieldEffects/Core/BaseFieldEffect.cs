using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Базовый класс для всех эффектов поля
/// Содержит общую функциональность и упрощает создание новых эффектов
/// </summary>
public abstract class BaseFieldEffect : MonoBehaviour, IFieldEffect
{
    [Header("Основные настройки")]
    [SerializeField] protected FieldEffectData _effectData;
    [SerializeField] protected bool _isActive = true;
    [SerializeField] protected int _priority = 0;
    
    [Header("Визуализация")]
    [SerializeField] protected bool _showGizmos = true;
    [SerializeField] protected Color _gizmoColor = Color.yellow;
    [SerializeField] protected bool _showEffectRadius = true;
    
    [Header("Производительность")]
    [SerializeField] protected bool _useDistanceOptimization = true;
    [SerializeField] protected float _maxDistance = 20f;
    
    // Компоненты
    protected Collider2D _collider;
    protected HashSet<IFieldEffectTarget> _currentTargets = new HashSet<IFieldEffectTarget>();
    
    // Кэширование
    protected Vector3 _lastPosition;
    protected bool _positionChanged;
    
    // События
    public System.Action<IFieldEffectTarget> OnTargetEntered;
    public System.Action<IFieldEffectTarget> OnTargetExited;
    
    public virtual FieldEffectData GetEffectData() => _effectData;
    public virtual bool IsActive => _isActive;
    public virtual int Priority => _priority;
    
    #region Unity Lifecycle
    
    protected virtual void Awake()
    {
        InitializeComponents();
        InitializeEffectData();
    }
    
    protected virtual void Start()
    {
        RegisterInSystem();
    }
    
    protected virtual void Update()
    {
        CheckPositionChange();
        if (_isActive)
        {
            UpdateEffect();
        }
    }
    
    protected virtual void OnDestroy()
    {
        UnregisterFromSystem();
    }
    
    #endregion
    
    #region Initialization
    
    protected virtual void InitializeComponents()
    {
        // Получаем или создаем коллайдер
        _collider = GetComponent<Collider2D>();
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<CircleCollider2D>();
        }
        _collider.isTrigger = true;
        
        // Устанавливаем размер коллайдера
        UpdateColliderSize();
    }
    
    protected virtual void InitializeEffectData()
    {
        if (_effectData == null)
        {
            _effectData = CreateDefaultEffectData();
        }
        
        // Обновляем центр эффекта
        _effectData.center = transform.position;
        _lastPosition = transform.position;
    }
    
    protected abstract FieldEffectData CreateDefaultEffectData();
    
    protected virtual void UpdateColliderSize()
    {
        if (_collider != null && _effectData != null)
        {
            if (_collider is CircleCollider2D circle)
            {
                circle.radius = _effectData.radius;
            }
            else if (_collider is BoxCollider2D box)
            {
                box.size = Vector2.one * _effectData.radius * 2f;
            }
        }
    }
    
    #endregion
    
    #region System Registration
    
    protected virtual void RegisterInSystem()
    {
        if (FieldEffectSystem.Instance != null)
        {
            FieldEffectSystem.Instance.RegisterEffect(this);
        }
        else
        {
            Debug.LogWarning($"[BaseFieldEffect] FieldEffectSystem не найден при регистрации {gameObject.name}");
        }
    }
    
    protected virtual void UnregisterFromSystem()
    {
        if (FieldEffectSystem.Instance != null)
        {
            FieldEffectSystem.Instance.UnregisterEffect(this);
        }
    }
    
    #endregion
    
    #region Effect Implementation
    
    public abstract void ApplyEffect(IFieldEffectTarget target, float deltaTime);
    
    public virtual void RemoveEffect(IFieldEffectTarget target)
    {
        // Базовая реализация - ничего не делаем
        // Переопределяется в конкретных эффектах при необходимости
    }
    
    public virtual bool IsInEffectZone(Vector3 targetPosition)
    {
        if (!_isActive || _effectData == null) return false;
        
        float distance = Vector3.Distance(transform.position, targetPosition);
        return distance <= _effectData.radius;
    }
    
    protected virtual void UpdateEffect()
    {
        // Обновляем позицию эффекта
        if (_positionChanged)
        {
            _effectData.center = transform.position;
            _positionChanged = false;
        }
        
        // Дополнительная логика обновления в наследниках
        OnUpdateEffect();
    }
    
    protected virtual void OnUpdateEffect()
    {
        // Переопределяется в наследниках
    }
    
    #endregion
    
    #region Target Management
    
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        var target = other.GetComponent<IFieldEffectTarget>();
        if (target != null && target.CanBeAffectedBy(_effectData.effectType))
        {
            AddTarget(target);
        }
    }
    
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        var target = other.GetComponent<IFieldEffectTarget>();
        if (target != null)
        {
            RemoveTarget(target);
        }
    }
    
    protected virtual void AddTarget(IFieldEffectTarget target)
    {
        if (_currentTargets.Add(target))
        {
            OnTargetEntered?.Invoke(target);
        }
    }
    
    protected virtual void RemoveTarget(IFieldEffectTarget target)
    {
        if (_currentTargets.Remove(target))
        {
            RemoveEffect(target);
            OnTargetExited?.Invoke(target);
        }
    }
    
    public HashSet<IFieldEffectTarget> GetCurrentTargets()
    {
        return new HashSet<IFieldEffectTarget>(_currentTargets);
    }
    
    #endregion
    
    #region Utility
    
    protected virtual void CheckPositionChange()
    {
        if (transform.position != _lastPosition)
        {
            _lastPosition = transform.position;
            _positionChanged = true;
        }
    }
    
    protected virtual bool IsTargetInRange(IFieldEffectTarget target)
    {
        if (!_useDistanceOptimization) return true;
        
        float distance = Vector3.Distance(transform.position, target.GetPosition());
        return distance <= _maxDistance;
    }
    
    protected virtual float GetDistanceToTarget(IFieldEffectTarget target)
    {
        return Vector3.Distance(transform.position, target.GetPosition());
    }
    
    protected virtual Vector3 GetDirectionToTarget(IFieldEffectTarget target)
    {
        return (target.GetPosition() - transform.position).normalized;
    }
    
    protected virtual float GetEffectStrengthAtDistance(float distance)
    {
        if (distance <= 0) return _effectData.strength;
        
        // Простая линейная интерполяция
        float normalizedDistance = Mathf.Clamp01(distance / _effectData.radius);
        return _effectData.strength * (1f - normalizedDistance);
    }
    
    #endregion
    
    #region Public Interface
    
    public virtual void SetActive(bool active)
    {
        _isActive = active;
        
        if (!active)
        {
            // Удаляем эффект со всех текущих целей
            foreach (var target in _currentTargets)
            {
                RemoveEffect(target);
            }
        }
    }
    
    public virtual void SetStrength(float strength)
    {
        if (_effectData != null)
        {
            _effectData.strength = strength;
        }
    }
    
    public virtual void SetRadius(float radius)
    {
        if (_effectData != null)
        {
            _effectData.radius = radius;
            UpdateColliderSize();
        }
    }
    
    public virtual void SetPriority(int priority)
    {
        _priority = priority;
    }
    
    public virtual void SetEffectData(FieldEffectData data)
    {
        _effectData = data;
        if (_effectData != null)
        {
            UpdateColliderSize();
        }
    }
    
    #endregion
    
    #region Debug & Visualization
    
    protected virtual void OnDrawGizmos()
    {
        if (!_showGizmos) return;
        
        DrawEffectRadius();
        DrawEffectInfo();
        DrawTargetConnections();
    }
    
    protected virtual void DrawEffectRadius()
    {
        if (!_showEffectRadius || _effectData == null) return;
        
        Gizmos.color = _gizmoColor;
        DrawCircle(transform.position, _effectData.radius);
        
        // Внутренний круг для активной зоны
        Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, _effectData.radius * 0.1f);
    }
    
    protected virtual void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    protected virtual void DrawEffectInfo()
    {
#if UNITY_EDITOR
        if (_effectData != null)
        {
            var labelPos = transform.position + Vector3.up * (_effectData.radius + 1f);
            UnityEditor.Handles.Label(labelPos, 
                $"{_effectData.effectType}\nStr: {_effectData.strength:F1}\nTargets: {_currentTargets.Count}");
        }
#endif
    }
    
    protected virtual void DrawTargetConnections()
    {
        if (_currentTargets.Count == 0) return;
        
        Gizmos.color = Color.white;
        foreach (var target in _currentTargets)
        {
            if (target != null)
            {
                Gizmos.DrawLine(transform.position, target.GetPosition());
            }
        }
    }
    
    #endregion
}

/// <summary>
/// Помощник для создания эффектов через код
/// </summary>
public static class FieldEffectFactory
{
    public static T CreateEffect<T>(Vector3 position, FieldEffectData data = null) where T : BaseFieldEffect
    {
        GameObject obj = new GameObject($"FieldEffect_{typeof(T).Name}");
        obj.transform.position = position;
        
        T effect = obj.AddComponent<T>();
        
        if (data != null)
        {
            effect.SetEffectData(data);
        }
        
        return effect;
    }
    
    public static BaseFieldEffect CreateEffect(FieldEffectType type, Vector3 position, float strength = 1f, float radius = 5f)
    {
        var data = new FieldEffectData(type, strength, radius, position);
        
        switch (type)
        {
            case FieldEffectType.Gravity:
                return CreateEffect<GravityFieldEffectNew>(position, data);
            case FieldEffectType.Wind:
                return CreateEffect<WindFieldEffect>(position, data);
            case FieldEffectType.Vortex:
                return CreateEffect<VortexFieldEffect>(position, data);
            default:
                throw new System.NotImplementedException($"Тип эффекта {type} не реализован");
        }
    }
} 