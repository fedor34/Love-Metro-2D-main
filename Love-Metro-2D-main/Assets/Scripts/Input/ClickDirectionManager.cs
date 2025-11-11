using UnityEngine;

/// <summary>
/// Управляет направлением полета человечков через клики мыши
/// </summary>
public class ClickDirectionManager : MonoBehaviour
{
    [Header("Click Direction Settings")]
    [SerializeField] private float _maxClickDistance = 5f; // Максимальное расстояние от центра экрана для клика
    [SerializeField] private bool _normalizeDirection = true; // Нормализовать направление
    [SerializeField] private float _directionStrength = 1f; // Сила направления
    
    [Header("Visual Feedback")]
    [SerializeField] private LineRenderer _directionLine; // Линия показывающая направление
    [SerializeField] private bool _showDirectionLine = true;
    [SerializeField] private float _lineLength = 3f;
    [SerializeField] private Color _lineColor = Color.yellow;
    
    // Текущее направление клика
    public static Vector2 CurrentClickDirection { get; private set; } = Vector2.zero;
    public static bool HasClickDirection { get; private set; } = false;
    public static bool IsMouseHeld { get; private set; } = false;
    // Нормализованный горизонтальный ввод [-1..1] (от центра экрана)
    public static float HorizontalAxis { get; private set; } = 0f;
    // Нормализованная горизонтальная скорость перетаскивания [-inf..inf]
    public static float HorizontalVelocity { get; private set; } = 0f;
    // Вертикальный ввод и скорость (для 2D-импульса)
    public static float VerticalAxis { get; private set; } = 0f;
    public static float VerticalVelocity { get; private set; } = 0f;

    // Текущая позиция курсора (мир) при удержании и позиция при отпускании
    public static Vector2 CurrentPointerWorld { get; private set; } = Vector2.zero;
    public static Vector2 LastReleaseWorld { get; private set; } = Vector2.zero;
    public static bool HasReleasePoint { get; private set; } = false;
    public static float LastReleaseTime { get; private set; } = -999f;
    
    // События
    public static System.Action<Vector2> OnDirectionSet;
    
    private Camera _mainCamera;
    private Vector2 _screenCenter;
    private float _prevMouseX;
    private float _prevMouseY;
    private float _axisS; // сглаженный осевой ввод
    private float _velS;  // сглаженная скорость
    private float _axisYS;
    private float _velYS;
    [SerializeField] private float _axisDeadZone = 0.05f;   // зона нечувствительности (5% ширины)
    [SerializeField] private float _axisSmooth = 12f;       // сглаживание оси
    [SerializeField] private float _velSmooth = 20f;        // сглаживание скорости
    
    void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogWarning("[ClickDirectionManager] Main camera not found at Start – will retry.");
        }
        
        _screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        // Создаем LineRenderer если его нет
        if (_directionLine == null && _showDirectionLine)
        {
            CreateDirectionLine();
        }
        
        // Устанавливаем начальное направление вправо
        SetDirection(Vector2.right);
    }
    
    void Update()
    {
        // Камера могла измениться/пересоздаться при смене сцены – проверяем каждый кадр
        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null)
            return; // нет камеры – пропускаем кадр без ввода

        HandleInput();
        UpdateVisualFeedback();
    }
    
    void HandleInput()
    {
        // Удержание ЛКМ — обновляем направление в реальном времени
        if (Input.GetMouseButton(0))
        {
            IsMouseHeld = true;
            Vector2 mousePosition = Input.mousePosition;
            Vector2 screenDirection = mousePosition - _screenCenter;
            // Ось X от центра экрана, нормированная к половине ширины
            float halfW = Mathf.Max(1f, Screen.width * 0.5f);
            float rawAxis = Mathf.Clamp(screenDirection.x / halfW, -1f, 1f);
            if (Mathf.Abs(rawAxis) < _axisDeadZone) rawAxis = 0f;
            _axisS = Mathf.Lerp(_axisS, rawAxis, _axisSmooth * Time.deltaTime);
            HorizontalAxis = _axisS;
            // Горизонтальная скорость перетаскивания (нормированная)
            float rawVel = ((mousePosition.x - _prevMouseX) / halfW) / Mathf.Max(0.0001f, Time.deltaTime);
            _velS = Mathf.Lerp(_velS, rawVel, _velSmooth * Time.deltaTime);
            HorizontalVelocity = _velS;
            _prevMouseX = mousePosition.x;

            // Вертикальная ось и скорость
            float halfH = Mathf.Max(1f, Screen.height * 0.5f);
            float rawAxisY = Mathf.Clamp(screenDirection.y / halfH, -1f, 1f);
            if (Mathf.Abs(rawAxisY) < _axisDeadZone) rawAxisY = 0f;
            _axisYS = Mathf.Lerp(_axisYS, rawAxisY, _axisSmooth * Time.deltaTime);
            VerticalAxis = _axisYS;
            float rawVelY = ((mousePosition.y - _prevMouseY) / halfH) / Mathf.Max(0.0001f, Time.deltaTime);
            _velYS = Mathf.Lerp(_velYS, rawVelY, _velSmooth * Time.deltaTime);
            VerticalVelocity = _velYS;
            _prevMouseY = mousePosition.y;
            if (screenDirection.sqrMagnitude > 0.0001f)
            {
                Vector2 direction = screenDirection.normalized;
                if (!_normalizeDirection)
                {
                    direction = screenDirection * _directionStrength / 100f;
                }
                else
                {
                    direction *= _directionStrength;
                }
                SetDirection(direction);
            }
            CurrentPointerWorld = _mainCamera != null ? _mainCamera.ScreenToWorldPoint(mousePosition) : Vector2.zero;
        }

        // Начало удержания — сбрасываем релиз
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            _prevMouseX = mousePosition.x;
            _prevMouseY = mousePosition.y;
            if (_mainCamera != null)
                CurrentPointerWorld = _mainCamera.ScreenToWorldPoint(mousePosition);

            // Вычисляем направление от центра экрана к позиции клика
            Vector2 screenDirection = mousePosition - _screenCenter;
            
            // Ограничиваем максимальное расстояние
            if (screenDirection.magnitude > _maxClickDistance * 100f) // 100f для перевода в пиксели
            {
                screenDirection = screenDirection.normalized * _maxClickDistance * 100f;
            }
            
            // Переводим в мировые координаты и нормализуем
            Vector2 direction = screenDirection.normalized;
            
            if (!_normalizeDirection)
            {
                direction = screenDirection * _directionStrength / 100f;
            }
            else
            {
                direction *= _directionStrength;
            }
            
            SetDirection(direction);
            HasReleasePoint = false; // новый цикл удержания
            Debug.Log($"[ClickDirectionManager] MouseDown at {mousePosition}, direction: {direction}");
        }

        // Отпускание — фиксируем мировую точку и помечаем релиз
        if (Input.GetMouseButtonUp(0))
        {
            IsMouseHeld = false;
            Vector2 mousePosition = Input.mousePosition;
            if (_mainCamera != null)
                LastReleaseWorld = _mainCamera.ScreenToWorldPoint(mousePosition);
            HasReleasePoint = true;
            LastReleaseTime = Time.time;
            Debug.Log($"[ClickDirectionManager] MouseUp at world {LastReleaseWorld}");
            // Сбрасываем ввод
            HorizontalAxis = 0f; _axisS = 0f; HorizontalVelocity = 0f; _velS = 0f;
            VerticalAxis = 0f; _axisYS = 0f; VerticalVelocity = 0f; _velYS = 0f;
        }
    }
    
    void SetDirection(Vector2 direction)
    {
        CurrentClickDirection = direction;
        HasClickDirection = true;
        
        OnDirectionSet?.Invoke(direction);
        
        Debug.Log($"[ClickDirectionManager] Direction set to: {direction}");
    }
    
    void UpdateVisualFeedback()
    {
        if (_directionLine != null && _showDirectionLine && HasClickDirection)
        {
            Vector3 startPos = Vector3.zero;
            Vector3 endPos = new Vector3(CurrentClickDirection.x, CurrentClickDirection.y, 0) * _lineLength;
            
            _directionLine.SetPosition(0, startPos);
            _directionLine.SetPosition(1, endPos);
            _directionLine.enabled = true;
        }
        else if (_directionLine != null)
        {
            _directionLine.enabled = false;
        }
    }
    
    void CreateDirectionLine()
    {
        GameObject lineObj = new GameObject("DirectionLine");
        lineObj.transform.SetParent(transform);
        
        _directionLine = lineObj.AddComponent<LineRenderer>();
        _directionLine.material = new Material(Shader.Find("Sprites/Default"));
        _directionLine.startColor = _lineColor;
        _directionLine.endColor = _lineColor;
        _directionLine.startWidth = 0.1f;
        _directionLine.endWidth = 0.05f;
        _directionLine.positionCount = 2;
        _directionLine.useWorldSpace = true;
        _directionLine.sortingOrder = 10;
        
        Debug.Log("[ClickDirectionManager] Created direction line");
    }
    
    // Публичный метод для получения текущего направления
    public static Vector2 GetCurrentDirection()
    {
        return HasClickDirection ? CurrentClickDirection : Vector2.right;
    }
    
    // Метод для сброса направления
    public static void ResetDirection()
    {
        HasClickDirection = false;
        CurrentClickDirection = Vector2.zero;
    }
}
