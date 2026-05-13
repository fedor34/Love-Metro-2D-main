using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private ParallaxLayer[] _parallaxLayers;
    [SerializeField] private bool _updateViaReflection = true;
    [SerializeField] private TrainManager _trainManager;

    [Header("Speed Source")]
    [SerializeField] private bool _preferExternalSpeed = true;
    [SerializeField] private float _externalHoldTime = 0.25f;

    private float _lastSpeed;
    private float _lastExternalSetTime = -999f;

    private void Start()
    {
        EnsureLayersInitialized();
    }

    private void Update()
    {
        EnsureLayersInitialized();
        ResolveTrainManager();

        if (ShouldReadSpeedFromTrain())
            _lastSpeed = _trainManager.GetCurrentSpeed();

        ApplySpeedToLayers(_lastSpeed);
    }

    public void SetTrainSpeed(float speed)
    {
        _lastSpeed = speed;
        _lastExternalSetTime = Time.time;
        EnsureLayersInitialized();
        ApplySpeedToLayers(speed);
    }

    public void Configure(TrainManager trainManager, ParallaxLayer[] layers)
    {
        if (trainManager != null)
            _trainManager = trainManager;

        if (layers != null && layers.Length > 0)
            _parallaxLayers = layers;
    }

    private void EnsureLayersInitialized()
    {
        if (_parallaxLayers == null)
            _parallaxLayers = System.Array.Empty<ParallaxLayer>();
    }

    private void ResolveTrainManager()
    {
        if (_trainManager == null && LoveMetro.Core.RuntimeServices.Instance.TrainMotionEvents is TrainManager trainManager)
            _trainManager = trainManager;
    }

    private bool ShouldReadSpeedFromTrain()
    {
        if (!_updateViaReflection || _trainManager == null)
            return false;

        if (!_preferExternalSpeed)
            return true;

        return Time.time - _lastExternalSetTime > _externalHoldTime;
    }

    private void ApplySpeedToLayers(float speed)
    {
        if (_parallaxLayers == null)
            return;

        for (int i = 0; i < _parallaxLayers.Length; i++)
        {
            ParallaxLayer layer = _parallaxLayers[i];
            if (layer != null && layer.transform != null)
                layer.UpdateLayer(speed);
        }
    }
}
