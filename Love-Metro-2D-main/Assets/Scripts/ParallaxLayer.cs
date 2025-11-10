using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Настройки параллакса")]
    [SerializeField] private float _parallaxSpeed = 0.25f; // базовый коэффициент перевода скорости поезда в смещение
    [SerializeField, Tooltip("Нелинейное усиление визуальной скорости")] private float _speedGamma = 1.0f; // 1 = линейная зависимость
    [SerializeField] private bool _useTrainSpeed = true;
    [SerializeField] private Vector2 _scrollDirection = Vector2.left;

    [Header("Режим прокрутки")]
    [SerializeField] private bool _scrollByTransform = true; // если материал/шейдер не тянут offset — двигаем трансформ напрямую
    [SerializeField] private float _transformScrollScale = 1.0f; // линейная, без усилений
    [SerializeField] private bool _enableLooping = false;      // отключаем зацикливание по умолчанию (для простоты)
    [SerializeField] private bool _tileHorizontally = false;   // тайлинг выключен по умолчанию
    [SerializeField] private int _tilesCount = 3;              // количество тайлов (>=2) если включено

    [Header("Автонастройка скорости по имени слоя")] 
    [SerializeField] private string[] _fastKeys = new[] { "город", "city", "фон", "background" };
    [SerializeField] private float _nameBoost = 2f;

    private Material _material;
    private Vector2 _offset;
    private float _nameMultiplier = 1f;
    private Vector3 _startWorldPos;
    private float _layerWidth;
    private SpriteRenderer _renderer;
    private Transform[] _tiles;

    public float Speed; // Оставляем для совместимости
    
    private void Start()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null && _renderer.material != null)
        {
            _material = _renderer.material;
        }

        string lower = gameObject.name.ToLower();
        foreach (var k in _fastKeys)
        {
            if (lower.Contains(k)) { _nameMultiplier = _nameBoost; break; }
        }

        // Кэшируем стартовую позицию и ширину слоя для зацикливания
        _startWorldPos = transform.position;
        if (_renderer != null)
        {
            _layerWidth = _renderer.bounds.size.x;
        }

        // Создаём горизонтальные тайлы для бесшовной прокрутки
        if (_tileHorizontally && _renderer != null && _tilesCount >= 2 && _layerWidth > 0.001f)
        {
            _tiles = new Transform[_tilesCount];
            _tiles[0] = transform;
            for (int i = 1; i < _tilesCount; i++)
            {
                var go = new GameObject(name + "_tile_" + i, typeof(SpriteRenderer));
                go.transform.SetParent(transform.parent, worldPositionStays: true);
                go.transform.position = _startWorldPos + new Vector3(_layerWidth * i, 0f, 0f);
                var r = go.GetComponent<SpriteRenderer>();
                r.sprite = _renderer.sprite;
                r.sharedMaterial = _renderer.sharedMaterial;
                r.sortingLayerID = _renderer.sortingLayerID;
                r.sortingOrder = _renderer.sortingOrder;
                go.isStatic = false;
                _tiles[i] = go.transform;
            }
        }
    }
    
    /// <summary>
    /// Обновляет слой параллакса с заданной скоростью
    /// </summary>
    /// <param name="trainSpeed">Скорость поезда</param>
    public void UpdateLayer(float trainSpeed)
    {
        if (!_useTrainSpeed) return;
        
        float effectiveTrainSpeed = Mathf.Abs(trainSpeed);

        // Линейная привязка скорости фона к скорости поезда
        float effectiveSpeed = Mathf.Clamp(effectiveTrainSpeed * _parallaxSpeed * _nameMultiplier, 0f, 1000f);

        if (_scrollByTransform)
        {
            Vector2 dir = _scrollDirection.sqrMagnitude > 0.0001f ? _scrollDirection.normalized : Vector2.left;
            float rawStep = (effectiveSpeed * _transformScrollScale) * Time.deltaTime;
            float maxStep = (_layerWidth > 0.001f) ? _layerWidth * 0.9f : Mathf.Abs(rawStep); // допускаем до 0.9 ширины за кадр
            float step = Mathf.Clamp(rawStep, -maxStep, maxStep);

            if (_tiles != null && _tiles.Length >= 2)
            {
                // Двигаем все тайлы
                foreach (var t in _tiles)
                {
                    if (t != null) t.position += (Vector3)(dir * step);
                }
                if (_enableLooping) LoopTiles(dir, step);
            }
            else
            {
                transform.position += (Vector3)(dir * step);
                if (_enableLooping && _layerWidth > 0.001f) TryLoopHorizontally();
            }
        }
        else if (_material != null)
        {
            _offset += _scrollDirection * effectiveSpeed * Time.deltaTime;
            _material.SetVector("_Offset", _offset);
            _material.mainTextureOffset = _offset;
        }
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

    // Зацикливание по ширине: если слой «уехал» дальше своей ширины, возвращаем в окрестность старта
    private void TryLoopHorizontally()
    {
        float dx = transform.position.x - _startWorldPos.x;
        float w = _layerWidth * 0.5f; // половина ширины для безопасного порога
        if (dx <= -w)
        {
            transform.position += new Vector3(_layerWidth, 0f, 0f);
        }
        else if (dx >= w)
        {
            transform.position -= new Vector3(_layerWidth, 0f, 0f);
        }
    }

    private void LoopTiles(Vector2 dir, float step)
    {
        if (_tiles == null || _tiles.Length < 2) return;
        // Находим самый левый и самый правый тайл
        int left = 0, right = 0;
        for (int i = 1; i < _tiles.Length; i++)
        {
            if (_tiles[i] == null) continue;
            if (_tiles[i].position.x < _tiles[left].position.x) left = i;
            if (_tiles[i].position.x > _tiles[right].position.x) right = i;
        }

        // Если движемся влево и левый тайл ушёл слишком далеко — перенесём его вправо за правый тайл
        if (dir.x < 0f && _tiles[left] != null && _tiles[right] != null)
        {
            if (_tiles[right].position.x - _tiles[left].position.x > _layerWidth * (_tiles.Length - 1) + _layerWidth * 0.5f)
            {
                _tiles[left].position = _tiles[right].position + new Vector3(_layerWidth, 0f, 0f);
            }
        }
        // Если движемся вправо — переносим правый тайл влево
        else if (dir.x > 0f && _tiles[left] != null && _tiles[right] != null)
        {
            if (_tiles[right].position.x - _tiles[left].position.x > _layerWidth * (_tiles.Length - 1) + _layerWidth * 0.5f)
            {
                _tiles[right].position = _tiles[left].position - new Vector3(_layerWidth, 0f, 0f);
            }
        }
    }
}
