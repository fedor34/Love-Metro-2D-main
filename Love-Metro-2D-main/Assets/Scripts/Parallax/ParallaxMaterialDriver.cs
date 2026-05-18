using System.Collections.Generic;
using UnityEngine;

public class ParallaxMaterialDriver : MonoBehaviour
{
    private sealed class TargetBinding
    {
        public SpriteRenderer Renderer;
        public Material Material;
        public ParallaxLayer ParallaxLayer;
        public bool HasElapsedTime;
        public bool HasElapsedTimePlain;
        public bool HasSpeed;
        public bool HasSpeedPlain;
        public bool HasSpeedModificator;
        public bool HasSpeedModificatorPlain;
    }

    private static readonly int ElapsedTimeId = Shader.PropertyToID("_elapsedTime");
    private static readonly int ElapsedTimePlainId = Shader.PropertyToID("elapsedTime");
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");
    private static readonly int SpeedPlainId = Shader.PropertyToID("Speed");
    private static readonly int SpeedModificatorId = Shader.PropertyToID("_SpeedModificator");
    private static readonly int SpeedModificatorPlainId = Shader.PropertyToID("SpeedModificator");

    [Header("Speed Coupling")]
    [SerializeField] private float _maxTrainSpeed = 480f;
    [SerializeField] private float _elapsedTimeScale = 1.0f;
    [SerializeField] private float _speedResponseScale = 0.8f;
    [SerializeField] private float _targetRefreshRetryInterval = 1.0f;
    [SerializeField] private bool _logOnce = true;
    [SerializeField] private float _baseSpeedMod = 1.4f;

    [Header("Dependencies")]
    [SerializeField] private TrainManager _train;

    private readonly List<TargetBinding> _targets = new List<TargetBinding>();
    private bool _logged;
    private float _parallaxOffset;
    private float _nextTargetRefreshTime;
    private LoveMetro.Train.ITrainMotionEvents _trainEvents;

    private void Awake()
    {
        ResolveTrainManager();
    }

    private void Update()
    {
        if (!ResolveTrainManager())
            return;

        if (_targets.Count == 0)
        {
            TryRefreshTargets();
            if (_targets.Count == 0)
                return;
        }

        RemoveMissingTargets();
        if (_targets.Count == 0)
        {
            ScheduleNextTargetRefresh();
            return;
        }

        LoveMetro.Train.TrainMotionState motionState = _trainEvents.CurrentMotionState;
        float absoluteSpeed = Mathf.Abs(motionState.CurrentSpeed);
        float normalizedSpeed = CalculateNormalizedTrainSpeed(
            absoluteSpeed,
            _maxTrainSpeed,
            _speedResponseScale,
            !motionState.IsStopped && !motionState.IsBraking && absoluteSpeed > 0f);

        _parallaxOffset = AdvanceParallaxTime(_parallaxOffset, _elapsedTimeScale, Time.deltaTime);
        ApplyParallaxProperties(_parallaxOffset, normalizedSpeed, _baseSpeedMod);
        LogTickOnce(absoluteSpeed, normalizedSpeed);
    }

    public void Configure(LoveMetro.Train.ITrainMotionEvents train, IEnumerable<SpriteRenderer> renderers)
    {
        if (train != null)
        {
            _trainEvents = train;
            if (train is TrainManager trainManager)
                _train = trainManager;
        }

        RefreshTargets(renderers);
    }

    public void RefreshTargets()
    {
        RefreshTargets(null);
    }

    public void RefreshTargets(IEnumerable<SpriteRenderer> renderers)
    {
        _targets.Clear();

        if (renderers == null)
        {
            ScheduleNextTargetRefresh();
            return;
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            if (!ShouldTrackRenderer(renderer))
                continue;

            Material runtimeMaterial = renderer.material;
            if (runtimeMaterial == null)
                continue;

            _targets.Add(new TargetBinding
            {
                Renderer = renderer,
                Material = runtimeMaterial,
                ParallaxLayer = renderer.GetComponent<ParallaxLayer>(),
                HasElapsedTime = runtimeMaterial.HasProperty(ElapsedTimeId),
                HasElapsedTimePlain = runtimeMaterial.HasProperty(ElapsedTimePlainId),
                HasSpeed = runtimeMaterial.HasProperty(SpeedId),
                HasSpeedPlain = runtimeMaterial.HasProperty(SpeedPlainId),
                HasSpeedModificator = runtimeMaterial.HasProperty(SpeedModificatorId),
                HasSpeedModificatorPlain = runtimeMaterial.HasProperty(SpeedModificatorPlainId)
            });

            renderer.gameObject.isStatic = false;
        }

        DisableParallaxLayers();
        if (_targets.Count > 0)
            Diagnostics.Log($"[ParallaxMaterialDriver] Found {_targets.Count} parallax targets.");

        ScheduleNextTargetRefresh();
    }

    private bool ResolveTrainManager()
    {
        if (_trainEvents == null && _train != null)
            _trainEvents = _train;

        return _trainEvents != null;
    }

    private void RemoveMissingTargets()
    {
        _targets.RemoveAll(IsMissingTarget);
    }

    private void TryRefreshTargets()
    {
        if (Time.time < _nextTargetRefreshTime)
            return;

        RefreshTargets();
    }

    private void DisableParallaxLayers()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            ParallaxLayer layer = _targets[i].ParallaxLayer;
            if (layer != null)
                layer.enabled = false;
        }
    }

    private void ApplyParallaxProperties(float parallaxTime, float normalizedSpeed, float baseSpeedMod)
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            TargetBinding target = _targets[i];
            Material material = target.Material;
            if (material == null)
                continue;

            SetFloatIfPresent(material, ElapsedTimeId, target.HasElapsedTime, parallaxTime);
            SetFloatIfPresent(material, ElapsedTimePlainId, target.HasElapsedTimePlain, parallaxTime);
            SetFloatIfPresent(material, SpeedId, target.HasSpeed, normalizedSpeed);
            SetFloatIfPresent(material, SpeedPlainId, target.HasSpeedPlain, normalizedSpeed);
            SetFloatIfPresent(material, SpeedModificatorId, target.HasSpeedModificator, baseSpeedMod);
            SetFloatIfPresent(material, SpeedModificatorPlainId, target.HasSpeedModificatorPlain, baseSpeedMod);
        }
    }

    private void LogTickOnce(float absoluteSpeed, float normalizedSpeed)
    {
        if (!_logOnce || _logged)
            return;

        _logged = true;
        Diagnostics.Log(
            $"[ParallaxMaterialDriver] speed={absoluteSpeed:F2} normalized={normalizedSpeed:F2} " +
            $"time={_parallaxOffset:F2} targets={_targets.Count}");
    }

    private static bool ShouldTrackRenderer(SpriteRenderer renderer)
    {
        if (renderer == null)
            return false;

        return ParallaxRendererClassifier.IsParallaxDrivenMaterial(renderer.sharedMaterial);
    }

    private static float CalculateNormalizedTrainSpeed(
        float currentSpeed,
        float maxTrainSpeed,
        float speedResponseScale,
        bool isPointerHeld)
    {
        if (!isPointerHeld)
            return 0f;

        float safeMaxSpeed = Mathf.Max(0.01f, maxTrainSpeed);
        return Mathf.Clamp01(Mathf.Abs(currentSpeed) / safeMaxSpeed) * Mathf.Max(0f, speedResponseScale);
    }

    private static float AdvanceParallaxTime(float currentOffset, float elapsedTimeScale, float deltaTime)
    {
        return currentOffset + Mathf.Max(0f, elapsedTimeScale) * Mathf.Max(0f, deltaTime);
    }

    private static void SetFloatIfPresent(Material material, int propertyId, bool hasProperty, float value)
    {
        if (material != null && hasProperty)
            material.SetFloat(propertyId, value);
    }

    private static bool IsMissingTarget(TargetBinding target)
    {
        return target == null ||
               target.Renderer == null ||
               !target.Renderer ||
               target.Material == null;
    }

    private void ScheduleNextTargetRefresh()
    {
        _nextTargetRefreshTime = Time.time + Mathf.Max(0.1f, _targetRefreshRetryInterval);
    }
}
