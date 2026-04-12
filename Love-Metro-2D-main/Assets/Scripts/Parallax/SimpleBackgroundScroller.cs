using UnityEngine;

/// <summary>
/// Бесшовный скроллинг фоновых слоёв.
///
/// Принцип работы (детерминированный wrap):
///   • При инициализации каждому тайлу запоминается начальная X-позиция (originX).
///   • В Update накапливается scrollOffset (отрицательный = движение влево).
///   • Позиция каждого тайла = ((originX + scrollOffset − anchor) mod ring) + anchor,
///     где ring = tileStep × N, anchor = camLeft − tileStep.
///   • Поскольку originX значения равномерно разнесены на tileStep,
///     а ring кратен tileStep, тайлы всегда покрывают окно без зазоров и без дрейфа.
/// </summary>
public class SimpleBackgroundScroller : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public SpriteRenderer renderer;
        public float speedFactor = 1f;

        // Runtime — не сериализуется
        [System.NonSerialized] public Transform[] tiles;
        [System.NonSerialized] public float[] originX;    // начальные X-позиции тайлов (мир)
        [System.NonSerialized] public float scrollOffset; // накопленный сдвиг (< 0 = влево)
        [System.NonSerialized] public float tileStep;     // ширина одного тайла
        [System.NonSerialized] public float ringWidth;    // tileStep × tiles.Length
        [System.NonSerialized] public float windowAnchor; // фиксированный левый край окна кольца
        [System.NonSerialized] public float tileY;        // Y-позиция (константа)
        [System.NonSerialized] public float tileZ;        // Z-позиция (константа)
        [System.NonSerialized] public bool initialized;
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
    private Camera _mainCamera;

    private void Awake()
    {
        ResolveTrainManager(force: true);
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!ResolveTrainManager())
            return;

        if (_layers == null || _layers.Length == 0)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        float speed = Mathf.Abs(_train.GetCurrentSpeed());
        float delta = ComputeScrollDelta(speed, _direction, _baseMultiplier, Time.deltaTime);

        for (int i = 0; i < _layers.Length; i++)
        {
            Layer layer = _layers[i];
            if (layer == null || layer.renderer == null)
                continue;

            if (!layer.initialized)
            {
                TryInitLayer(layer);
                continue;
            }

            if (delta != 0f)
                layer.scrollOffset += delta * layer.speedFactor;

            RepositionTiles(layer);
        }
    }

    // ── Инициализация ───────────────────────────────────────────────────────

    private void TryInitLayer(Layer layer)
    {
        if (layer == null || layer.renderer == null || layer.initialized)
            return;

        if (!layer.renderer.gameObject.activeInHierarchy)
            return;

        Sprite sprite = layer.renderer.sprite;
        if (sprite == null)
            return;

        // Ширина тайла в мировых единицах
        float w = sprite.rect.width / sprite.pixelsPerUnit
                  * Mathf.Abs(layer.renderer.transform.lossyScale.x);

        if (w <= 0.01f)
            return;

        layer.tileStep = w;

        Camera cam = _mainCamera != null ? _mainCamera : Camera.main;
        float cameraHalfWidth = cam != null
            ? cam.orthographicSize * cam.aspect
            : 15f;

        // Тайлов: покрыть ширину камеры + 3 запасных (левый буфер + правый буфер + запас)
        int tilesNeeded = Mathf.CeilToInt(cameraHalfWidth * 2f / w) + 3;
        tilesNeeded = Mathf.Max(tilesNeeded, 3);

        layer.ringWidth = w * tilesNeeded;

        float camLeft = cam != null
            ? cam.transform.position.x - cameraHalfWidth
            : -15f;

        // Якорь кольца фиксируется один раз при инициализации.
        // Окно = [windowAnchor, windowAnchor + ring).
        // Первый тайл — на одну ширину левее камеры (буфер).
        float startX = camLeft - w;
        layer.windowAnchor = startX;

        Vector3 origPos = layer.renderer.transform.position;
        layer.tileY = origPos.y;
        layer.tileZ = origPos.z;
        layer.scrollOffset = 0f;

        layer.tiles   = new Transform[tilesNeeded];
        layer.originX = new float[tilesNeeded];

        // Тайл 0 — оригинальный рендерер
        layer.renderer.transform.position = new Vector3(startX, layer.tileY, layer.tileZ);
        layer.tiles[0]   = layer.renderer.transform;
        layer.originX[0] = startX;

        for (int i = 1; i < tilesNeeded; i++)
        {
            GameObject copy = CreateTile(layer.renderer, layer.renderer.name + "_t" + i);
            float x = startX + w * i;
            copy.transform.position = new Vector3(x, layer.tileY, layer.tileZ);
            layer.tiles[i]   = copy.transform;
            layer.originX[i] = x;
        }

        layer.initialized = true;

        Diagnostics.Log(
            $"[BGScroller] '{layer.renderer.name}': w={w:F2} tiles={tilesNeeded} ring={layer.ringWidth:F1} anchor={startX:F1}");
    }

    private static GameObject CreateTile(SpriteRenderer source, string tileName)
    {
        GameObject go = new GameObject(tileName);
        go.transform.SetParent(source.transform.parent, worldPositionStays: true);
        go.transform.localScale = source.transform.localScale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite          = source.sprite;
        sr.sharedMaterial  = source.sharedMaterial;
        sr.sortingLayerID  = source.sortingLayerID;
        sr.sortingOrder    = source.sortingOrder;
        sr.color           = source.color;
        sr.flipX           = source.flipX;
        sr.flipY           = source.flipY;
        return go;
    }

    // ── Детерминированное позиционирование ──────────────────────────────────

    /// <summary>
    /// Вычисляет позицию каждого тайла как (originX + scrollOffset) mod ringWidth,
    /// с привязкой окна к windowAnchor.
    /// Математически гарантирует равномерное заполнение без зазоров и без дрейфа.
    /// </summary>
    private void RepositionTiles(Layer layer)
    {
        float ring   = layer.ringWidth;
        float anchor = layer.windowAnchor;

        if (ring <= 0f) return;

        for (int i = 0; i < layer.tiles.Length; i++)
        {
            Transform tile = layer.tiles[i];
            if (tile == null) continue;

            float x = layer.originX[i] + layer.scrollOffset;

            // Привести x в окно [anchor, anchor + ring)
            float t       = (x - anchor) / ring;
            float wrapped = x - Mathf.Floor(t) * ring;

            tile.position = new Vector3(wrapped, layer.tileY, layer.tileZ);
        }
    }

    // ── Вспомогательные ─────────────────────────────────────────────────────

    private bool ResolveTrainManager(bool force = false)
    {
        if (_train != null) return true;

        if (!force && Time.time < _nextTrainResolveTime)
            return false;

        _train = FindObjectOfType<TrainManager>();
        _nextTrainResolveTime = Time.time + Mathf.Max(0.1f, _trainResolveRetryInterval);
        return _train != null;
    }

    /// <summary>
    /// Возвращает знаковый скалярный сдвиг по X за один кадр.
    /// Отрицательный — движение влево (direction = Vector2.left).
    /// </summary>
    private static float ComputeScrollDelta(float speed, Vector2 direction,
                                            float baseMultiplier, float deltaTime)
    {
        if (direction.sqrMagnitude <= 0.0001f || deltaTime <= 0f || speed <= 0f)
            return 0f;

        return direction.normalized.x * baseMultiplier * speed * deltaTime;
    }
}
