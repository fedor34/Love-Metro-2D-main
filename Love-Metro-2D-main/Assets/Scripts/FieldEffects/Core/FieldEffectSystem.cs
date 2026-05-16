using System;
using System.Collections.Generic;
using LoveMetro.FieldEffects;
using UnityEngine;

public class FieldEffectSystem : MonoBehaviour, IFieldEffectSystem
{
    private static FieldEffectSystem _instance;
    public static FieldEffectSystem Instance => _instance;

    [Header("System")]
    [SerializeField] private bool _enableDebugMode = true;
    [SerializeField] private bool _enableGizmos = true;
    [SerializeField] private int _maxEffectsPerTarget = 5;

    [Header("Performance")]
    [SerializeField] private bool _useFixedUpdate = true;

    private FieldEffectRegistry _registry;
    private FieldEffectTargetRegistry _targets;
    private FieldEffectApplicator _applicator;
    private FieldEffectSpatialQuery _spatialQuery;
    private readonly HashSet<IFieldEffect> _nearbyEffectSet = new HashSet<IFieldEffect>();

    private const float CacheUpdateInterval = 0.1f;

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

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
            InitializeRuntime();
            LoveMetro.Core.RuntimeServices.Instance.RegisterFieldEffectSystem(this);
            return;
        }

        if (_instance != this)
        {
            Debug.LogWarning($"[FieldEffectSystem] Duplicate system detected, destroying {name}");
            LoveMetro.Core.UnityLifecycle.SafeDestroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            LoveMetro.Core.RuntimeServices.Instance.UnregisterFieldEffectSystem(this);
            _instance = null;
        }
    }

    private void InitializeRuntime()
    {
        _registry = new FieldEffectRegistry();
        _targets = new FieldEffectTargetRegistry();
        _spatialQuery = new FieldEffectSpatialQuery(CacheUpdateInterval);
        _applicator = new FieldEffectApplicator(() => _maxEffectsPerTarget, () => Time.time);
        _applicator.EffectApplied += HandleEffectApplied;
        _applicator.EffectRemoved += HandleEffectRemoved;
    }

    private void HandleEffectApplied(IFieldEffectTarget target, IFieldEffect effect)
    {
        OnEffectAppliedToTarget?.Invoke(target, effect);

        if (_enableDebugMode && effect != null && effect.GetEffectData()?.effectType == FieldEffectType.Wind)
            Debug.Log($"[FieldEffectSystem] Applying wind to {target.GetPosition()} via {effect.GetType().Name}");
    }

    private void HandleEffectRemoved(IFieldEffectTarget target, IFieldEffect effect)
    {
        OnEffectRemovedFromTarget?.Invoke(target, effect);
    }

    #endregion

    #region Effect Registration

    public void RegisterEffect(IFieldEffect effect)
    {
        if (_registry == null)
            InitializeRuntime();

        if (!_registry.TryRegister(effect, out FieldEffectData data, out FieldEffectCategory category))
            return;

        OnEffectRegistered?.Invoke(effect);

        if (_enableDebugMode)
            Debug.Log($"[FieldEffectSystem] Registered {data.effectType} in {category}");
    }

    public void UnregisterEffect(IFieldEffect effect)
    {
        if (_registry == null)
            return;

        if (!_registry.TryUnregister(effect, out FieldEffectData data, out _))
            return;

        RemoveEffectFromAllTargets(effect);
        OnEffectUnregistered?.Invoke(effect);

        if (_enableDebugMode)
            Debug.Log($"[FieldEffectSystem] Unregistered {data.effectType}");
    }

    #endregion

    #region Target Management

    public void RegisterTarget(IFieldEffectTarget target)
    {
        if (_targets == null)
            InitializeRuntime();

        _targets.Register(target);
    }

    public void UnregisterTarget(IFieldEffectTarget target)
    {
        if (target == null || _targets == null)
            return;

        if (_targets.TryGetActiveEffects(target, out List<ActiveEffectData> activeEffects))
            _applicator.RemoveAllFromTarget(target, activeEffects);

        _targets.RemoveTarget(target);
    }

    #endregion

    #region Update System

    private void Start()
    {
        if (_enableDebugMode && _registry != null)
            Debug.Log($"[FieldEffectSystem] Found {_registry.GetTotalEffectsCount()} effects and {_targets.Count} targets");
    }

    public void RegisterSceneComponents(IEnumerable<MonoBehaviour> sceneComponents)
    {
        if (sceneComponents == null)
            return;

        foreach (MonoBehaviour component in sceneComponents)
        {
            if (component == null)
                continue;

            if (component is IFieldEffect effect)
                RegisterEffect(effect);

            if (component is IFieldEffectTarget target)
                RegisterTarget(target);
        }
    }

    private void Update()
    {
        if (!_useFixedUpdate)
            UpdateEffects();
    }

    private void FixedUpdate()
    {
        if (_useFixedUpdate)
            UpdateEffects();
    }

    private void UpdateEffects()
    {
        if (_targets == null || _registry == null)
            return;

        float deltaTime = _useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;
        _spatialQuery?.Update(Time.time);

        IReadOnlyList<IFieldEffectTarget> targets = _targets.AllTargets;
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            IFieldEffectTarget target = targets[i];
            if (target == null)
            {
                _targets.RemoveTargetAt(i, target);
                continue;
            }

            try
            {
                UpdateEffectsForTarget(target, deltaTime);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FieldEffectSystem] Failed to update target: {exception.Message}");
                _targets.RemoveTargetAt(i, target);
            }
        }
    }

    private void UpdateEffectsForTarget(IFieldEffectTarget target, float deltaTime)
    {
        if (!FieldEffectTargetRegistry.TryGetTargetPosition(target, out Vector3 targetPosition))
        {
            _targets.RemoveTarget(target);
            return;
        }

        List<IFieldEffect> nearbyEffects = GetEffectsAtPosition(targetPosition);
        _nearbyEffectSet.Clear();
        for (int i = 0; i < nearbyEffects.Count; i++)
        {
            if (nearbyEffects[i] != null)
                _nearbyEffectSet.Add(nearbyEffects[i]);
        }

        List<ActiveEffectData> activeEffects = _targets.GetOrCreateActiveEffects(target);

        foreach (IFieldEffect effect in nearbyEffects)
        {
            if (_applicator.CanApply(target, activeEffects, effect))
                _applicator.Apply(target, effect, activeEffects, deltaTime);
        }

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            IFieldEffect effect = activeEffects[i].Effect;
            if (effect == null)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            if (!_nearbyEffectSet.Contains(effect))
                _applicator.Remove(target, effect, activeEffects);
        }
    }

    #endregion

    #region Effect Application

    private void RemoveEffectFromAllTargets(IFieldEffect effect)
    {
        if (_targets == null)
            return;

        foreach (IFieldEffectTarget target in _targets.SnapshotTargets())
        {
            if (target == null)
                continue;

            if (_targets.TryGetActiveEffects(target, out List<ActiveEffectData> activeEffects))
                _applicator.Remove(target, effect, activeEffects);
        }
    }

    #endregion

    #region Query API

    public List<IFieldEffect> GetEffectsAtPosition(Vector3 position)
    {
        if (_spatialQuery == null || _registry == null)
            return new List<IFieldEffect>();

        return _spatialQuery.GetEffectsAtPosition(position, _registry.EffectsByCategory);
    }

    public List<IFieldEffect> GetEffectsByCategory(FieldEffectCategory category)
    {
        return _registry?.GetEffectsByCategory(category) ?? new List<IFieldEffect>();
    }

    public List<IFieldEffect> GetEffectsByType(FieldEffectType type)
    {
        return _registry?.GetEffectsByType(type) ?? new List<IFieldEffect>();
    }

    public int GetTotalEffectsCount()
    {
        return _registry?.GetTotalEffectsCount() ?? 0;
    }

    public int GetTargetsCount()
    {
        return _targets?.Count ?? 0;
    }

    public List<IFieldEffect> GetActiveEffects()
    {
        return _registry?.GetAllEffects() ?? new List<IFieldEffect>();
    }

    public void ClearAllEffects()
    {
        if (_registry == null)
            return;

        List<IFieldEffect> effectsToRemove = _registry.GetAllEffects();
        foreach (IFieldEffect effect in effectsToRemove)
        {
            if (effect == null)
                continue;

            UnregisterEffect(effect);

            if (effect is MonoBehaviour behaviour && behaviour != null)
                LoveMetro.Core.UnityLifecycle.SafeDestroy(behaviour.gameObject);
        }

        Debug.Log($"[FieldEffectSystem] Cleared all effects ({effectsToRemove.Count} removed)");
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!_enableGizmos || _instance != this || _registry == null)
            return;

        Gizmos.color = Color.white;
        Vector3 cameraPosition = Camera.current?.transform.position ?? Vector3.zero;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            cameraPosition + Vector3.up * 3f,
            $"Field Effects: {_registry.GetTotalEffectsCount()}\nTargets: {_targets?.Count ?? 0}");
#endif
    }

    #endregion
}

public enum FieldEffectCategory
{
    Movement,
    Modifier,
    Trigger,
    Visual,
    Audio,
    Other
}

[Serializable]
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
