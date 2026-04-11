using UnityEngine;

/// <summary>
/// Captures pointer intent for train movement and passenger launching.
/// Keeps the legacy static API used by gameplay code while isolating input and visuals.
/// </summary>
public partial class ClickDirectionManager : MonoBehaviour
{
    [Header("Click Direction Settings")]
    [SerializeField] private float _maxClickDistance = 5f;
    [SerializeField] private bool _normalizeDirection = true;
    [SerializeField] private float _directionStrength = 1f;

    [Header("Visual Feedback")]
    [SerializeField] private LineRenderer _directionLine;
    [SerializeField] private bool _showDirectionLine = true;
    [SerializeField] private float _lineLength = 3f;
    [SerializeField] private Color _lineColor = Color.yellow;

    [Header("Input Filtering")]
    [SerializeField] private float _axisDeadZone = 0.05f;
    [SerializeField] private float _axisSmooth = 12f;
    [SerializeField] private float _velSmooth = 20f;

    public static Vector2 CurrentClickDirection { get; private set; } = Vector2.zero;
    public static bool HasClickDirection { get; private set; }
    public static bool IsMouseHeld { get; private set; }
    public static float HorizontalAxis { get; private set; }
    public static float HorizontalVelocity { get; private set; }
    public static float VerticalAxis { get; private set; }
    public static float VerticalVelocity { get; private set; }
    public static Vector2 CurrentPointerWorld { get; private set; } = Vector2.zero;
    public static Vector2 LastReleaseWorld { get; private set; } = Vector2.zero;
    public static bool HasReleasePoint { get; private set; }
    public static float LastReleaseTime { get; private set; } = -999f;

    public static System.Action<Vector2> OnDirectionSet;

    private Camera _mainCamera;
    private Vector2 _screenCenter;
    private Vector2 _lastScreenSize;
    private float _prevMouseX;
    private float _prevMouseY;
    private float _axisS;
    private float _velS;
    private float _axisYS;
    private float _velYS;
    private bool _inputBlocked;
    private bool _suppressReleaseUntilMouseUp;

    private void Start()
    {
        RefreshScreenMetrics(force: true);
        RefreshCameraReference(logIfMissing: true);
        EnsureDirectionLine();
        SetDirection(Vector2.right);
    }

    private void Update()
    {
        if (!RefreshCameraReference())
            return;

        RefreshScreenMetrics();
        HandleInput();
        UpdateVisualFeedback();
    }

    private void OnDisable()
    {
        ResetHoldState();
        HideDirectionLine();
    }

    private void OnDestroy()
    {
        ResetStaticState();
        OnDirectionSet = null;
    }

    private void SetDirection(Vector2 direction)
    {
        CurrentClickDirection = direction;
        HasClickDirection = true;

        OnDirectionSet?.Invoke(direction);
        Diagnostics.Log($"[ClickDirectionManager] Direction set to {direction}");
    }

    private void ResetHoldState()
    {
        _inputBlocked = false;
        _suppressReleaseUntilMouseUp = false;
        IsMouseHeld = false;
        ResetMotionState();
    }

    private static void ResetStaticState()
    {
        ResetDirection();
        IsMouseHeld = false;
        HorizontalAxis = 0f;
        HorizontalVelocity = 0f;
        VerticalAxis = 0f;
        VerticalVelocity = 0f;
        CurrentPointerWorld = Vector2.zero;
        LastReleaseWorld = Vector2.zero;
        HasReleasePoint = false;
        LastReleaseTime = -999f;
    }

    public static Vector2 GetCurrentDirection()
    {
        return HasClickDirection ? CurrentClickDirection : Vector2.right;
    }

    public static void ResetDirection()
    {
        HasClickDirection = false;
        CurrentClickDirection = Vector2.zero;
    }
}
