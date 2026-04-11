using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private float _parallaxSpeed = 0.25f;
    [SerializeField, Tooltip("Non-linear amplification of visual speed. 1 means linear response.")]
    private float _speedGamma = 1.0f;
    [SerializeField] private bool _useTrainSpeed = true;
    [SerializeField] private Vector2 _scrollDirection = Vector2.left;

    [Header("Scroll Mode")]
    [SerializeField] private bool _scrollByTransform = true;
    [SerializeField] private float _transformScrollScale = 1.0f;
    [SerializeField] private bool _enableLooping;
    [SerializeField] private bool _tileHorizontally;
    [SerializeField] private int _tilesCount = 3;

    [Header("Name-Based Boost")]
    [SerializeField] private string[] _fastKeys = new[] { "\u0433\u043e\u0440\u043e\u0434", "city", "\u0444\u043e\u043d", "background" };
    [SerializeField] private float _nameBoost = 2f;

    private Material _material;
    private Vector2 _offset;
    private float _nameMultiplier = 1f;
    private Vector3 _startWorldPosition;
    private float _layerWidth;
    private SpriteRenderer _renderer;
    private Transform[] _tiles;

    public float Speed;

    private void Start()
    {
        CacheRendererState();
        CacheNameMultiplier();
        CacheGeometry();
        TryCreateTiles();
    }

    public void UpdateLayer(float trainSpeed)
    {
        if (!_useTrainSpeed)
            return;

        float effectiveSpeed = CalculateEffectiveSpeed(trainSpeed, _parallaxSpeed, _nameMultiplier, _speedGamma);
        Speed = effectiveSpeed;

        if (_scrollByTransform)
        {
            ApplyTransformScroll(effectiveSpeed);
            return;
        }

        ApplyMaterialScroll(effectiveSpeed);
    }

    public void SetParallaxSpeed(float speed)
    {
        _parallaxSpeed = speed;
    }

    public void SetUseTrainSpeed(bool use)
    {
        _useTrainSpeed = use;
    }

    public void SetScrollByTransform(bool enabled)
    {
        _scrollByTransform = enabled;
    }

    public void SetTransformScrollScale(float scale)
    {
        _transformScrollScale = scale;
    }

    private void CacheRendererState()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _material = _renderer != null ? _renderer.material : null;
    }

    private void CacheNameMultiplier()
    {
        _nameMultiplier = ResolveNameMultiplier(gameObject.name, _fastKeys, _nameBoost);
    }

    private void CacheGeometry()
    {
        _startWorldPosition = transform.position;
        _layerWidth = _renderer != null ? _renderer.bounds.size.x : 0f;
    }

    private void TryCreateTiles()
    {
        if (!_tileHorizontally || _renderer == null || _tilesCount < 2 || _layerWidth <= 0.001f)
            return;

        _tiles = new Transform[_tilesCount];
        _tiles[0] = transform;

        for (int i = 1; i < _tilesCount; i++)
        {
            GameObject tileObject = new GameObject(name + "_tile_" + i, typeof(SpriteRenderer));
            tileObject.transform.SetParent(transform.parent, worldPositionStays: true);
            tileObject.transform.position = _startWorldPosition + new Vector3(_layerWidth * i, 0f, 0f);

            SpriteRenderer tileRenderer = tileObject.GetComponent<SpriteRenderer>();
            tileRenderer.sprite = _renderer.sprite;
            tileRenderer.sharedMaterial = _renderer.sharedMaterial;
            tileRenderer.sortingLayerID = _renderer.sortingLayerID;
            tileRenderer.sortingOrder = _renderer.sortingOrder;
            tileObject.isStatic = false;

            _tiles[i] = tileObject.transform;
        }
    }

    private void ApplyTransformScroll(float effectiveSpeed)
    {
        Vector2 direction = ResolveScrollDirection(_scrollDirection);
        float step = CalculateClampedTransformStep(
            effectiveSpeed,
            _transformScrollScale,
            _layerWidth,
            Time.deltaTime);

        if (Mathf.Abs(step) <= 0f)
            return;

        if (_tiles != null && _tiles.Length >= 2)
        {
            MoveTiles(direction, step);
            if (_enableLooping)
                LoopTiles(direction);

            return;
        }

        transform.position += (Vector3)(direction * step);
        if (_enableLooping && _layerWidth > 0.001f)
            TryLoopHorizontally();
    }

    private void ApplyMaterialScroll(float effectiveSpeed)
    {
        if (_material == null)
            return;

        _offset += ResolveScrollDirection(_scrollDirection) * effectiveSpeed * Time.deltaTime;
        _material.SetVector("_Offset", _offset);
        _material.mainTextureOffset = _offset;
    }

    private void MoveTiles(Vector2 direction, float step)
    {
        for (int i = 0; i < _tiles.Length; i++)
        {
            Transform tile = _tiles[i];
            if (tile != null)
                tile.position += (Vector3)(direction * step);
        }
    }

    private void TryLoopHorizontally()
    {
        float distanceFromStart = transform.position.x - _startWorldPosition.x;
        float halfWidth = _layerWidth * 0.5f;

        if (distanceFromStart <= -halfWidth)
        {
            transform.position += new Vector3(_layerWidth, 0f, 0f);
        }
        else if (distanceFromStart >= halfWidth)
        {
            transform.position -= new Vector3(_layerWidth, 0f, 0f);
        }
    }

    private void LoopTiles(Vector2 direction)
    {
        if (_tiles == null || _tiles.Length < 2)
            return;

        int leftIndex = 0;
        int rightIndex = 0;
        for (int i = 1; i < _tiles.Length; i++)
        {
            if (_tiles[i] == null)
                continue;

            if (_tiles[i].position.x < _tiles[leftIndex].position.x)
                leftIndex = i;

            if (_tiles[i].position.x > _tiles[rightIndex].position.x)
                rightIndex = i;
        }

        Transform leftTile = _tiles[leftIndex];
        Transform rightTile = _tiles[rightIndex];
        if (leftTile == null || rightTile == null)
            return;

        float allowedSpan = _layerWidth * (_tiles.Length - 1) + _layerWidth * 0.5f;
        float currentSpan = rightTile.position.x - leftTile.position.x;
        if (currentSpan <= allowedSpan)
            return;

        if (direction.x < 0f)
        {
            leftTile.position = rightTile.position + new Vector3(_layerWidth, 0f, 0f);
        }
        else if (direction.x > 0f)
        {
            rightTile.position = leftTile.position - new Vector3(_layerWidth, 0f, 0f);
        }
    }

    private static float ResolveNameMultiplier(string objectName, string[] fastKeys, float nameBoost)
    {
        if (ParallaxRendererClassifier.MatchesAny(objectName, fastKeys))
            return Mathf.Max(1f, nameBoost);

        return 1f;
    }

    private static float CalculateEffectiveSpeed(float trainSpeed, float parallaxSpeed, float nameMultiplier, float speedGamma)
    {
        float absoluteSpeed = Mathf.Max(0f, Mathf.Abs(trainSpeed));
        float gamma = Mathf.Max(0.01f, speedGamma);
        float curvedSpeed = Mathf.Pow(absoluteSpeed, gamma);
        return Mathf.Clamp(curvedSpeed * Mathf.Max(0f, parallaxSpeed) * Mathf.Max(0f, nameMultiplier), 0f, 1000f);
    }

    private static Vector2 ResolveScrollDirection(Vector2 scrollDirection)
    {
        return scrollDirection.sqrMagnitude > 0.0001f ? scrollDirection.normalized : Vector2.left;
    }

    private static float CalculateClampedTransformStep(
        float effectiveSpeed,
        float transformScrollScale,
        float layerWidth,
        float deltaTime)
    {
        float rawStep = Mathf.Max(0f, effectiveSpeed) * Mathf.Max(0f, transformScrollScale) * Mathf.Max(0f, deltaTime);
        if (rawStep <= 0f)
            return 0f;

        float maxStep = layerWidth > 0.001f ? layerWidth * 0.9f : rawStep;
        return Mathf.Clamp(rawStep, -maxStep, maxStep);
    }
}
