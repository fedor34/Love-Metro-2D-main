using System.Collections.Generic;
using UnityEngine;

public class ParallaxMaterialDriver : MonoBehaviour
{
    private sealed class TargetBinding
    {
        public SpriteRenderer Renderer;
        public Material Material;
        public ParallaxLayer ParallaxLayer;
    }

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

    private void Awake()
    {
        ResolveTrainManager();
        RefreshTargets();
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

        float absoluteSpeed = Mathf.Abs(_train.GetCurrentSpeed());
        float normalizedSpeed = CalculateNormalizedTrainSpeed(
            absoluteSpeed,
            _maxTrainSpeed,
            _speedResponseScale,
            ClickDirectionManager.IsMouseHeld);

        _parallaxOffset = AdvanceParallaxTime(_parallaxOffset, _elapsedTimeScale, Time.deltaTime);
        ApplyParallaxProperties(_parallaxOffset, normalizedSpeed, _baseSpeedMod);
        LogTickOnce(absoluteSpeed, normalizedSpeed);
    }

    public void RefreshTargets()
    {
        _targets.Clear();

        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (!ShouldTrackRenderer(renderer))
                continue;

            Material runtimeMaterial = renderer.material;
            if (runtimeMaterial == null)
                continue;

            _targets.Add(new TargetBinding
            {
                Renderer = renderer,
                Material = runtimeMaterial,
                ParallaxLayer = renderer.GetComponent<ParallaxLayer>()
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
        if (_train == null)
            _train = FindObjectOfType<TrainManager>();

        return _train != null;
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
            Material material = _targets[i].Material;
            if (material == null)
                continue;

            SetFloatIfPresent(material, "_elapsedTime", parallaxTime);
            SetFloatIfPresent(material, "elapsedTime", parallaxTime);
            SetFloatIfPresent(material, "_Speed", normalizedSpeed);
            SetFloatIfPresent(material, "Speed", normalizedSpeed);
            SetFloatIfPresent(material, "_SpeedModificator", baseSpeedMod);
            SetFloatIfPresent(material, "SpeedModificator", baseSpeedMod);
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

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material != null && material.HasFloat(propertyName))
            material.SetFloat(propertyName, value);
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
