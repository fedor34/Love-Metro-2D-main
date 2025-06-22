using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Центральная система управления эффектами поля
/// Инициализируется автоматически и управляет всеми эффектами
/// </summary>
public class FieldEffectSystem : MonoBehaviour
{
    private static FieldEffectSystem _instance;
    public static FieldEffectSystem Instance => _instance;

    [Header("Система")]
    [SerializeField] private bool _enableDebugMode = true;
    [SerializeField] private bool _enableGizmos = true;
    [SerializeField] private int _maxEffectsPerTarget = 5;
    
    [Header("Производительность")]
    [SerializeField] private bool _useFixedUpdate = true;
    
    // Хранение эффектов по категориям
    private Dictionary<FieldEffectCategory, List<IFieldEffect>> _effectsByCategory;
    private Dictionary<FieldEffectType, List<IFieldEffect>> _effectsByType;
    private Dictionary<IFieldEffectTarget, List<ActiveEffectData>> _activeEffectsPerTarget;
    
    // Кэширование для производительности
    private List<IFieldEffectTarget> _allTargets;
    private Dictionary<Vector3, List<IFieldEffect>> _spatialCache;
    private float _cacheUpdateTime;
    private const float CACHE_UPDATE_INTERVAL = 0.1f;
    
    // События системы
    public static event Action<IFieldEffect> OnEffectRegistered;
    public static event Action<IFieldEffect> OnEffectUnregistered;
    public static event Action<IFieldEffectTarget, IFieldEffect> OnEffectAppliedToTarget;
    public static event Action<IFieldEffectTarget, IFieldEffect> OnEffectRemovedFromTarget;

    #region Initialization
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        OnEffectRegistered = null;
        OnEffectUnregistered = null;
        OnEffectAppliedToTarget = null;
        OnEffectRemovedFromTarget = null;
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeSystem()
    {
        if (_instance == null)
        {
            CreateSystemInstance();
        }
    }
    
    private static void CreateSystemInstance()
    {
        GameObject systemObj = new GameObject("[FieldEffectSystem]");
        _instance = systemObj.AddComponent<FieldEffectSystem>();
        DontDestroyOnLoad(systemObj);
        
        Debug.Log("[FieldEffectSystem] Система инициализирована автоматически");
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCollections();
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[FieldEffectSystem] Дублированная система обнаружена, уничтожаю {name}");
            Destroy(gameObject);
        }
    }
    
    private void InitializeCollections()
    {
        _effectsByCategory = new Dictionary<FieldEffectCategory, List<IFieldEffect>>();
        _effectsByType = new Dictionary<FieldEffectType, List<IFieldEffect>>();
        _activeEffectsPerTarget = new Dictionary<IFieldEffectTarget, List<ActiveEffectData>>();
        _allTargets = new List<IFieldEffectTarget>();
        _spatialCache = new Dictionary<Vector3, List<IFieldEffect>>();
        
        // Инициализируем категории
        foreach (FieldEffectCategory category in Enum.GetValues(typeof(FieldEffectCategory)))
        {
            _effectsByCategory[category] = new List<IFieldEffect>();
        }
        
        foreach (FieldEffectType type in Enum.GetValues(typeof(FieldEffectType)))
        {
            _effectsByType[type] = new List<IFieldEffect>();
        }
    }
    
    #endregion
    
    #region Effect Registration
    
    public void RegisterEffect(IFieldEffect effect)
    {
        if (effect == null) return;
        
        var data = effect.GetEffectData();
        var category = GetEffectCategory(data.effectType);
        
        if (!_effectsByCategory[category].Contains(effect))
        {
            _effectsByCategory[category].Add(effect);
            _effectsByType[data.effectType].Add(effect);
            
            OnEffectRegistered?.Invoke(effect);
            
            if (_enableDebugMode)
            {
                Debug.Log($"[FieldEffectSystem] Зарегистрирован эффект {data.effectType} категории {category}");
            }
        }
    }
    
    public void UnregisterEffect(IFieldEffect effect)
    {
        if (effect == null) return;
        
        var data = effect.GetEffectData();
        var category = GetEffectCategory(data.effectType);
        
        _effectsByCategory[category].Remove(effect);
        _effectsByType[data.effectType].Remove(effect);
        
        // Удаляем эффект со всех целей
        RemoveEffectFromAllTargets(effect);
        
        OnEffectUnregistered?.Invoke(effect);
        
        if (_enableDebugMode)
        {
            Debug.Log($"[FieldEffectSystem] Удален эффект {data.effectType}");
        }
    }
    
    #endregion
    
    #region Target Management
    
    public void RegisterTarget(IFieldEffectTarget target)
    {
        if (target != null && !_allTargets.Contains(target))
        {
            _allTargets.Add(target);
            _activeEffectsPerTarget[target] = new List<ActiveEffectData>();
        }
    }
    
    public void UnregisterTarget(IFieldEffectTarget target)
    {
        if (target != null)
        {
            _allTargets.Remove(target);
            _activeEffectsPerTarget.Remove(target);
        }
    }
    
    #endregion
    
    #region Update System
    
    private void Start()
    {
        // Находим существующие эффекты и цели на сцене
        DiscoverExistingComponents();
        
        if (_enableDebugMode)
        {
            Debug.Log($"[FieldEffectSystem] Обнаружено {GetTotalEffectsCount()} эффектов и {_allTargets.Count} целей");
        }
    }
    
    private void DiscoverExistingComponents()
    {
        // Находим эффекты
        var effects = FindObjectsOfType<MonoBehaviour>();
        foreach (var obj in effects)
        {
            if (obj is IFieldEffect effect)
            {
                RegisterEffect(effect);
            }
        }
        
        // Находим цели
        var targets = FindObjectsOfType<MonoBehaviour>();
        foreach (var obj in targets)
        {
            if (obj is IFieldEffectTarget target)
            {
                RegisterTarget(target);
            }
        }
    }
    
    private void Update()
    {
        if (!_useFixedUpdate)
        {
            UpdateEffects();
        }
    }
    
    private void FixedUpdate()
    {
        if (_useFixedUpdate)
        {
            UpdateEffects();
        }
    }
    
    private void UpdateEffects()
    {
        float deltaTime = _useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;
        
        // Обновляем кэш если нужно
        UpdateSpatialCache();
        
        // Обновляем все активные эффекты
        foreach (var target in _allTargets)
        {
            if (target == null) continue;
            
            UpdateEffectsForTarget(target, deltaTime);
        }
    }
    
    private void UpdateEffectsForTarget(IFieldEffectTarget target, float deltaTime)
    {
        var targetPosition = target.GetPosition();
        var nearbyEffects = GetEffectsAtPosition(targetPosition);
        
        var activeEffects = _activeEffectsPerTarget[target];
        
        // Применяем новые эффекты
        foreach (var effect in nearbyEffects)
        {
            if (CanApplyEffect(target, effect))
            {
                ApplyEffectToTarget(target, effect, deltaTime);
            }
        }
        
        // Удаляем эффекты которые больше не действуют
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var activeEffect = activeEffects[i];
            if (!nearbyEffects.Contains(activeEffect.Effect))
            {
                RemoveEffectFromTarget(target, activeEffect.Effect);
            }
        }
    }
    
    #endregion
    
    #region Effect Application
    
    private bool CanApplyEffect(IFieldEffectTarget target, IFieldEffect effect)
    {
        var data = effect.GetEffectData();
        return target.CanBeAffectedBy(data.effectType) && 
               _activeEffectsPerTarget[target].Count < _maxEffectsPerTarget;
    }
    
    private void ApplyEffectToTarget(IFieldEffectTarget target, IFieldEffect effect, float deltaTime)
    {
        var activeEffects = _activeEffectsPerTarget[target];
        var existingEffect = activeEffects.Find(e => e.Effect == effect);
        
        if (existingEffect == null)
        {
            // Новый эффект
            var newActiveEffect = new ActiveEffectData(effect, Time.time);
            activeEffects.Add(newActiveEffect);
            target.OnEnterFieldEffect(effect);
            OnEffectAppliedToTarget?.Invoke(target, effect);
        }
        
        // Применяем эффект
        effect.ApplyEffect(target, deltaTime);
    }
    
    private void RemoveEffectFromTarget(IFieldEffectTarget target, IFieldEffect effect)
    {
        var activeEffects = _activeEffectsPerTarget[target];
        activeEffects.RemoveAll(e => e.Effect == effect);
        
        effect.RemoveEffect(target);
        target.OnExitFieldEffect(effect);
        OnEffectRemovedFromTarget?.Invoke(target, effect);
    }
    
    private void RemoveEffectFromAllTargets(IFieldEffect effect)
    {
        foreach (var target in _allTargets)
        {
            if (target != null)
            {
                RemoveEffectFromTarget(target, effect);
            }
        }
    }
    
    #endregion
    
    #region Spatial Cache & Queries
    
    private void UpdateSpatialCache()
    {
        if (Time.time - _cacheUpdateTime > CACHE_UPDATE_INTERVAL)
        {
            _spatialCache.Clear();
            _cacheUpdateTime = Time.time;
        }
    }
    
    public List<IFieldEffect> GetEffectsAtPosition(Vector3 position)
    {
        var gridPos = new Vector3(
            Mathf.Round(position.x),
            Mathf.Round(position.y),
            Mathf.Round(position.z)
        );
        
        if (_spatialCache.TryGetValue(gridPos, out var cachedEffects))
        {
            return cachedEffects;
        }
        
        var effects = new List<IFieldEffect>();
        
        foreach (var categoryEffects in _effectsByCategory.Values)
        {
            foreach (var effect in categoryEffects)
            {
                if (effect != null && effect.IsInEffectZone(position))
                {
                    effects.Add(effect);
                }
            }
        }
        
        _spatialCache[gridPos] = effects;
        return effects;
    }
    
    public List<IFieldEffect> GetEffectsByCategory(FieldEffectCategory category)
    {
        return _effectsByCategory.TryGetValue(category, out var effects) 
            ? new List<IFieldEffect>(effects) 
            : new List<IFieldEffect>();
    }
    
    public List<IFieldEffect> GetEffectsByType(FieldEffectType type)
    {
        return _effectsByType.TryGetValue(type, out var effects) 
            ? new List<IFieldEffect>(effects) 
            : new List<IFieldEffect>();
    }
    
    #endregion
    
    #region Utility
    
    private FieldEffectCategory GetEffectCategory(FieldEffectType type)
    {
        switch (type)
        {
            case FieldEffectType.Gravity:
            case FieldEffectType.Repulsion:
            case FieldEffectType.Wind:
            case FieldEffectType.Magnetic:
            case FieldEffectType.Vortex:
                return FieldEffectCategory.Movement;
            case FieldEffectType.Slowdown:
            case FieldEffectType.Speedup:
            case FieldEffectType.Friction:
            case FieldEffectType.Bounce:
                return FieldEffectCategory.Modifier;
            case FieldEffectType.Teleport:
            case FieldEffectType.Checkpoint:
            case FieldEffectType.Activator:
                return FieldEffectCategory.Trigger;
            case FieldEffectType.Visual:
                return FieldEffectCategory.Visual;
            case FieldEffectType.Audio:
                return FieldEffectCategory.Audio;
            default:
                return FieldEffectCategory.Other;
        }
    }
    
    public int GetTotalEffectsCount()
    {
        int count = 0;
        foreach (var effects in _effectsByCategory.Values)
        {
            count += effects.Count;
        }
        return count;
    }
    
    public int GetTargetsCount()
    {
        return _allTargets?.Count ?? 0;
    }
    
    public List<IFieldEffect> GetActiveEffects()
    {
        var allActiveEffects = new List<IFieldEffect>();
        foreach (var effects in _effectsByCategory.Values)
        {
            allActiveEffects.AddRange(effects);
        }
        return allActiveEffects;
    }
    
    public void ClearAllEffects()
    {
        // Создаем копию списка эффектов для безопасного удаления
        var effectsToRemove = new List<IFieldEffect>();
        foreach (var effects in _effectsByCategory.Values)
        {
            effectsToRemove.AddRange(effects);
        }
        
        // Удаляем каждый эффект
        foreach (var effect in effectsToRemove)
        {
            if (effect != null)
            {
                UnregisterEffect(effect);
                
                // Если это MonoBehaviour, уничтожаем объект
                if (effect is MonoBehaviour mb && mb != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(mb.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(mb.gameObject);
                    }
                }
            }
        }
        
        Debug.Log($"[FieldEffectSystem] Очищены все эффекты ({effectsToRemove.Count} эффектов удалено)");
    }
    
    #endregion
    
    #region Debug & Gizmos
    
    private void OnDrawGizmos()
    {
        if (!_enableGizmos || _instance != this) return;
        
        // Рисуем информацию о системе
        Gizmos.color = Color.white;
        var cameraPos = Camera.current?.transform.position ?? Vector3.zero;
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(cameraPos + Vector3.up * 3, 
            $"Field Effects: {GetTotalEffectsCount()}\nTargets: {_allTargets?.Count ?? 0}");
#endif
    }
    
    #endregion
}

/// <summary>
/// Категории эффектов для группировки и оптимизации
/// </summary>
public enum FieldEffectCategory
{
    Movement,   // Эффекты движения (гравитация, ветер, отталкивание)
    Modifier,   // Модификаторы (ускорение, замедление)
    Trigger,    // Триггерные эффекты (телепорт, активация)
    Visual,     // Визуальные эффекты
    Audio,      // Звуковые эффекты
    Other       // Прочие эффекты
}

/// <summary>
/// Данные активного эффекта на цели
/// </summary>
[System.Serializable]
public class ActiveEffectData
{
    public IFieldEffect Effect;
    public float StartTime;
    public float Duration;
    public int Priority;
    
    public ActiveEffectData(IFieldEffect effect, float startTime, int priority = 0)
    {
        Effect = effect;
        StartTime = startTime;
        Priority = priority;
    }
} 