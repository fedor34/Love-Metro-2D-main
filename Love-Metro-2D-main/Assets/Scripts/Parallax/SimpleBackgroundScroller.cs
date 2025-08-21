using UnityEngine;

public class SimpleBackgroundScroller : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public SpriteRenderer renderer;
        public float speedFactor = 1f; // множитель относительно скорости поезда
    }

    [Header("Layers to scroll (world-space SpriteRenderers)")]
    [SerializeField] private Layer[] _layers;

    [Header("Direction and scaling")]
    [SerializeField] private Vector2 _direction = Vector2.left;
    [SerializeField] private float _baseMultiplier = 20f; // базовый множитель визуальной скорости

    private TrainManager _train;

    private void Awake()
    {
        _train = FindObjectOfType<TrainManager>();
    }

    private void Update()
    {
        if (_train == null || _layers == null) return;
        float s = Mathf.Abs(_train.GetCurrentSpeed());
        Vector3 delta = (Vector3)(_direction.normalized * (_baseMultiplier * s) * Time.deltaTime);
        foreach (var l in _layers)
        {
            if (l == null || l.renderer == null) continue;
            var t = l.renderer.transform;
            t.position += delta * l.speedFactor;
        }
    }
}