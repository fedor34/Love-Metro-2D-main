using UnityEngine;

public class SimpleBackgroundScroller : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public SpriteRenderer renderer;
        public float speedFactor = 1f;
    }

    [Header("Layers To Scroll")]
    [SerializeField] private Layer[] _layers;

    [Header("Direction And Scaling")]
    [SerializeField] private Vector2 _direction = Vector2.left;
    [SerializeField] private float _baseMultiplier = 20f;

    [Header("Dependencies")]
    [SerializeField] private TrainManager _train;
    [SerializeField] private float _trainResolveRetryInterval = 1.0f;

    private float _nextTrainResolveTime;

    private void Awake()
    {
        ResolveTrainManager(force: true);
    }

    private void Update()
    {
        if (!ResolveTrainManager())
            return;

        if (_layers == null || _layers.Length == 0)
            return;

        Vector3 delta = BuildScrollDelta(Mathf.Abs(_train.GetCurrentSpeed()), _direction, _baseMultiplier, Time.deltaTime);
        if (delta.sqrMagnitude <= 0.000001f)
            return;

        for (int i = 0; i < _layers.Length; i++)
            ApplyScroll(_layers[i], delta);
    }

    private bool ResolveTrainManager(bool force = false)
    {
        if (_train != null)
            return true;

        if (!force && Time.time < _nextTrainResolveTime)
            return false;

        _train = FindObjectOfType<TrainManager>();
        _nextTrainResolveTime = Time.time + Mathf.Max(0.1f, _trainResolveRetryInterval);
        return _train != null;
    }

    private static Vector3 BuildScrollDelta(float speed, Vector2 direction, float baseMultiplier, float deltaTime)
    {
        if (direction.sqrMagnitude <= 0.0001f || deltaTime <= 0f || speed <= 0f)
            return Vector3.zero;

        return (Vector3)(direction.normalized * (baseMultiplier * speed) * deltaTime);
    }

    private static void ApplyScroll(Layer layer, Vector3 delta)
    {
        if (layer == null || layer.renderer == null)
            return;

        layer.renderer.transform.position += delta * layer.speedFactor;
    }
}
