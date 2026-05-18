using System;
using System.Collections.Generic;
using UnityEngine;

public class FieldEffectSystem : MonoBehaviour, LoveMetro.FieldEffects.IFieldEffectSystem
{
    private static FieldEffectSystem _instance;
    public static FieldEffectSystem Instance => _instance;

    [Header("System")]
    [SerializeField] private bool _enableDebugMode = true;
    [SerializeField] private bool _enableGizmos = true;
    [SerializeField] private int _maxEffectsPerTarget = 5;

    [Header("Performance")]
    [SerializeField] private bool _useFixedUpdate = true;

    private Dictionary<FieldEffectCategory, List<IFieldEffect>> _effectsByCategory;
    private Dictionary<FieldEffectType, List<IFieldEffect>> _effectsByType;
    private Dictionary<IFieldEffectTarget, List<ActiveEffectData>> _activeEffectsPerTarget;
    private List<IFieldEffectTarget> _allTargets;
    private FieldEffectSpatialQuery _spatialQuery;
    private readonly List<IFieldEffect> _nearbyEffectsBuffer = new List<IFieldEffect>(8);
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
            InitializeCollections();
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

    private void InitializeCollections()
    {
        _effectsByCategory = new Dictionary<FieldEffectCategory, List<IFieldEffect>>();
        _effectsByType = new Dictionary<FieldEffectType, List<IFieldEffect>>();
        _activeEffectsPerTarget = new Dictionary<IFieldEffectTarget, List<ActiveEffectData>>();
        _allTargets = new List<IFieldEffectTarget>();
        _spatialQuery = new FieldEffectSpatialQuery(CacheUpdateInterval);

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
        if (!TryGetEffectMetadata(effect, out FieldEffectData data, out FieldEffectCategory category))
            return;

        List<IFieldEffect> categoryEffects = _effectsByCategory[category];
        if (categoryEffects.Contains(effect))
            return;

        categoryEffects.Add(effect);
        _effectsByType[data.effectType].Add(effect);
        _spatialQuery?.MarkDirty();

        OnEffectRegistered?.Invoke(effect);

        if (_enableDebugMode)
        {
            Debug.Log($"[FieldEffectSystem] Registered {data.effectType} in {category}");
        }
    }

    public void UnregisterEffect(IFieldEffect effect)
    {
        if (!TryGetEffectMetadata(effect, out FieldEffectData data, out FieldEffectCategory category))
            return;

        _effectsByCategory[category].Remove(effect);
        _effectsByType[data.effectType].Remove(effect);
        _spatialQuery?.MarkDirty();
        RemoveEffectFromAllTargets(effect);

        OnEffectUnregistered?.Invoke(effect);

        if (_enableDebugMode)
        {
            Debug.Log($"[FieldEffectSystem] Unregistered {data.effectType}");
        }
    }

    #endregion

    #region Target Management

    public void RegisterTarget(IFieldEffectTarget target)
    {
        if (target == null || _allTargets.Contains(target))
            return;

        _allTargets.Add(target);
        GetOrCreateActiveEffects(target);
    }

    public void UnregisterTarget(IFieldEffectTarget target)
    {
        if (target == null)
            return;

        RemoveAllEffectsFromTarget(target);
        RemoveTargetReference(target);
    }

    #endregion

    #region Update System

    private void Start()
    {
        if (_enableDebugMode)
        {
            Debug.Log($"[FieldEffectSystem] Found {GetTotalEffectsCount()} effects and {_allTargets.Count} targets");
        }
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
            {
                RegisterEffect(effect);
            }

            if (component is IFieldEffectTarget target)
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
        UpdateSpatialCache();

        if (_allTargets == null)
            return;

        for (int i = _allTargets.Count - 1; i >= 0; i--)
        {
            IFieldEffectTarget target = _allTargets[i];
            if (target == null)
            {
                RemoveTargetReferenceAt(i, target);
                continue;
            }

            try
            {
                UpdateEffectsForTarget(target, deltaTime);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FieldEffectSystem] Failed to update target: {exception.Message}");
                RemoveTargetReferenceAt(i, target);
            }
        }
    }

    private void UpdateEffectsForTarget(IFieldEffectTarget target, float deltaTime)
    {
        if (target == null || _activeEffectsPerTarget == null)
            return;

        if (!TryGetTargetPosition(target, out Vector3 targetPosition))
        {
            RemoveTargetReference(target);
            return;
        }

        List<IFieldEffect> nearbyEffects = _nearbyEffectsBuffer;
        FillEffectsAtPosition(targetPosition, nearbyEffects);
        _nearbyEffectSet.Clear();
        for (int i = 0; i < nearbyEffects.Count; i++)
        {
            IFieldEffect nearbyEffect = nearbyEffects[i];
            if (nearbyEffect != null)
                _nearbyEffectSet.Add(nearbyEffect);
        }

        List<ActiveEffectData> activeEffects = GetOrCreateActiveEffects(target);

        foreach (IFieldEffect effect in nearbyEffects)
        {
            if (CanApplyEffect(target, activeEffects, effect))
            {
                ApplyEffectToTarget(target, effect, activeEffects, deltaTime);
            }
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
            {
                RemoveEffectFromTarget(target, effect, activeEffects);
            }
        }
    }

    #endregion

    #region Effect Application

    private bool CanApplyEffect(IFieldEffectTarget target, List<ActiveEffectData> activeEffects, IFieldEffect effect)
    {
        if (target == null || effect == null)
            return false;

        FieldEffectData data = effect.GetEffectData();
        if (data == null || !target.CanBeAffectedBy(data.effectType))
            return false;

        return HasActiveEffect(activeEffects, effect) || activeEffects.Count < _maxEffectsPerTarget;
    }

    private void ApplyEffectToTarget(IFieldEffectTarget target, IFieldEffect effect, List<ActiveEffectData> activeEffects, float deltaTime)
    {
        ActiveEffectData existingEffect = activeEffects.Find(activeEffect => activeEffect.Effect == effect);
        if (existingEffect == null)
        {
            activeEffects.Add(new ActiveEffectData(effect, Time.time));
            target.OnEnterFieldEffect(effect);
            OnEffectAppliedToTarget?.Invoke(target, effect);
        }

        if (_enableDebugMode && effect.GetEffectData().effectType == FieldEffectType.Wind)
        {
            Debug.Log($"[FieldEffectSystem] Applying wind to {target.GetPosition()} via {effect.GetType().Name}");
        }

        effect.ApplyEffect(target, deltaTime);
    }

    private bool RemoveEffectFromTarget(IFieldEffectTarget target, IFieldEffect effect, List<ActiveEffectData> activeEffects = null)
    {
        if (target == null || effect == null)
            return false;

        activeEffects ??= GetOrCreateActiveEffects(target);

        bool removed = false;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Effect != effect)
                continue;

            activeEffects.RemoveAt(i);
            removed = true;
        }

        if (!removed)
            return false;

        effect.RemoveEffect(target);
        target.OnExitFieldEffect(effect);
        OnEffectRemovedFromTarget?.Invoke(target, effect);
        return true;
    }

    private void RemoveEffectFromAllTargets(IFieldEffect effect)
    {
        List<IFieldEffectTarget> targets = new List<IFieldEffectTarget>(_activeEffectsPerTarget.Keys);
        foreach (IFieldEffectTarget target in targets)
        {
            if (target == null)
                continue;

            RemoveEffectFromTarget(target, effect);
        }
    }

    #endregion

    #region Spatial Cache And Queries

    private void UpdateSpatialCache()
    {
        _spatialQuery?.Update(Time.time, _effectsByCategory);
    }

    public List<IFieldEffect> GetEffectsAtPosition(Vector3 position)
    {
        if (_spatialQuery == null || _effectsByCategory == null)
            return new List<IFieldEffect>();

        List<IFieldEffect> effects = new List<IFieldEffect>();
        _spatialQuery.FillEffectsAtPosition(position, _effectsByCategory, effects, Time.time);
        return effects;
    }

    private void FillEffectsAtPosition(Vector3 position, List<IFieldEffect> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (_spatialQuery == null || _effectsByCategory == null)
            return;

        _spatialQuery.FillEffectsAtPosition(position, _effectsByCategory, results, Time.time);
    }

    public List<IFieldEffect> GetEffectsByCategory(FieldEffectCategory category)
    {
        return _effectsByCategory.TryGetValue(category, out List<IFieldEffect> effects)
            ? new List<IFieldEffect>(effects)
            : new List<IFieldEffect>();
    }

    public List<IFieldEffect> GetEffectsByType(FieldEffectType type)
    {
        return _effectsByType.TryGetValue(type, out List<IFieldEffect> effects)
            ? new List<IFieldEffect>(effects)
            : new List<IFieldEffect>();
    }

    #endregion

    #region Utility

    private bool TryGetEffectMetadata(IFieldEffect effect, out FieldEffectData data, out FieldEffectCategory category)
    {
        data = effect?.GetEffectData();
        if (effect == null || data == null)
        {
            category = FieldEffectCategory.Other;
            return false;
        }

        category = GetEffectCategory(data.effectType);
        return true;
    }

    private List<ActiveEffectData> GetOrCreateActiveEffects(IFieldEffectTarget target)
    {
        if (!_activeEffectsPerTarget.TryGetValue(target, out List<ActiveEffectData> activeEffects))
        {
            activeEffects = new List<ActiveEffectData>();
            _activeEffectsPerTarget[target] = activeEffects;
        }

        return activeEffects;
    }

    private bool HasActiveEffect(List<ActiveEffectData> activeEffects, IFieldEffect effect)
    {
        return activeEffects.Exists(activeEffect => activeEffect.Effect == effect);
    }

    private void RemoveAllEffectsFromTarget(IFieldEffectTarget target)
    {
        if (target == null || !_activeEffectsPerTarget.TryGetValue(target, out List<ActiveEffectData> activeEffects))
            return;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            RemoveEffectFromTarget(target, activeEffects[i].Effect, activeEffects);
        }
    }

    private void RemoveTargetReference(IFieldEffectTarget target)
    {
        _allTargets?.Remove(target);
        _activeEffectsPerTarget?.Remove(target);
    }

    private void RemoveTargetReferenceAt(int index, IFieldEffectTarget target)
    {
        if (_allTargets != null && index >= 0 && index < _allTargets.Count)
        {
            _allTargets.RemoveAt(index);
        }

        _activeEffectsPerTarget?.Remove(target);
    }

    private bool TryGetTargetPosition(IFieldEffectTarget target, out Vector3 targetPosition)
    {
        try
        {
            targetPosition = target.GetPosition();
            return true;
        }
        catch (Exception)
        {
            targetPosition = Vector3.zero;
            return false;
        }
    }

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
        foreach (List<IFieldEffect> effects in _effectsByCategory.Values)
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
        List<IFieldEffect> allActiveEffects = new List<IFieldEffect>();
        foreach (List<IFieldEffect> effects in _effectsByCategory.Values)
        {
            allActiveEffects.AddRange(effects);
        }

        return allActiveEffects;
    }

    public void ClearAllEffects()
    {
        List<IFieldEffect> effectsToRemove = new List<IFieldEffect>();
        foreach (List<IFieldEffect> effects in _effectsByCategory.Values)
        {
            effectsToRemove.AddRange(effects);
        }

        foreach (IFieldEffect effect in effectsToRemove)
        {
            if (effect == null)
                continue;

            UnregisterEffect(effect);

            if (effect is MonoBehaviour behaviour && behaviour != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(behaviour.gameObject);
                }
                else
                {
                    DestroyImmediate(behaviour.gameObject);
                }
            }
        }

        Debug.Log($"[FieldEffectSystem] Cleared all effects ({effectsToRemove.Count} removed)");
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!_enableGizmos || _instance != this)
            return;

        Gizmos.color = Color.white;
        Vector3 cameraPosition = Camera.current?.transform.position ?? Vector3.zero;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            cameraPosition + Vector3.up * 3f,
            $"Field Effects: {GetTotalEffectsCount()}\nTargets: {_allTargets?.Count ?? 0}");
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

internal sealed class FieldEffectSpatialQuery
{
    private readonly Dictionary<Vector2Int, List<IFieldEffect>> _cells = new Dictionary<Vector2Int, List<IFieldEffect>>();
    private readonly float _cacheUpdateInterval;
    private float _lastRebuildTime = float.NegativeInfinity;
    private bool _dirty = true;

    private const float CellSize = 2f;

    public FieldEffectSpatialQuery(float cacheUpdateInterval)
    {
        _cacheUpdateInterval = Mathf.Max(0.01f, cacheUpdateInterval);
    }

    public void MarkDirty()
    {
        _dirty = true;
    }

    public void Update(float time, Dictionary<FieldEffectCategory, List<IFieldEffect>> effectsByCategory)
    {
        EnsureCurrent(time, effectsByCategory);
    }

    public void FillEffectsAtPosition(
        Vector3 position,
        Dictionary<FieldEffectCategory, List<IFieldEffect>> effectsByCategory,
        List<IFieldEffect> results,
        float time)
    {
        if (results == null)
            return;

        results.Clear();
        if (effectsByCategory == null)
            return;

        EnsureCurrent(time, effectsByCategory);

        Vector2Int cell = PositionToCell(position);
        if (!_cells.TryGetValue(cell, out List<IFieldEffect> candidates))
            return;

        for (int i = 0; i < candidates.Count; i++)
        {
            IFieldEffect effect = candidates[i];
            if (effect != null && effect.IsInEffectZone(position))
                results.Add(effect);
        }
    }

    private void EnsureCurrent(float time, Dictionary<FieldEffectCategory, List<IFieldEffect>> effectsByCategory)
    {
        if (effectsByCategory == null)
            return;

        if (Application.isPlaying && !_dirty && time - _lastRebuildTime < _cacheUpdateInterval)
            return;

        Rebuild(time, effectsByCategory);
    }

    private void Rebuild(float time, Dictionary<FieldEffectCategory, List<IFieldEffect>> effectsByCategory)
    {
        _cells.Clear();
        foreach (List<IFieldEffect> categoryEffects in effectsByCategory.Values)
        {
            if (categoryEffects == null)
                continue;

            for (int i = 0; i < categoryEffects.Count; i++)
            {
                IFieldEffect effect = categoryEffects[i];
                if (!TryGetEffectBounds(effect, out Vector3 center, out float radius))
                    continue;

                Vector2Int minCell = PositionToCell(center - new Vector3(radius, radius, 0f));
                Vector2Int maxCell = PositionToCell(center + new Vector3(radius, radius, 0f));
                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    for (int y = minCell.y; y <= maxCell.y; y++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (!_cells.TryGetValue(cell, out List<IFieldEffect> effects))
                        {
                            effects = new List<IFieldEffect>();
                            _cells[cell] = effects;
                        }

                        effects.Add(effect);
                    }
                }
            }
        }

        _dirty = false;
        _lastRebuildTime = time;
    }

    private static bool TryGetEffectBounds(IFieldEffect effect, out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 0f;

        FieldEffectData data = effect?.GetEffectData();
        if (effect == null || data == null)
            return false;

        center = data.center;
        if (effect is MonoBehaviour behaviour && behaviour != null)
            center = behaviour.transform.position;

        radius = Mathf.Max(0f, data.radius);
        return true;
    }

    private static Vector2Int PositionToCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / CellSize),
            Mathf.FloorToInt(position.y / CellSize));
    }
}
