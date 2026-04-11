using UnityEngine;

public partial class ClickDirectionManager
{
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
            HandlePointerDown(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            HandlePointerUp(Input.mousePosition);

        if (_inputBlocked)
            return;

        if (Input.GetMouseButton(0))
            HandlePointerHeld(Input.mousePosition);
    }

    private void HandlePointerDown(Vector2 mousePosition)
    {
        if (TryConsumeManualPairingClick(mousePosition))
            return;

        _inputBlocked = false;
        _suppressReleaseUntilMouseUp = false;
        HasReleasePoint = false;

        _prevMouseX = mousePosition.x;
        _prevMouseY = mousePosition.y;
        CurrentPointerWorld = ScreenToWorld(mousePosition);

        Vector2 screenDirection = ClampScreenDirection(
            mousePosition - _screenCenter,
            ResolveMaxClickDistancePixels());
        Vector2 direction = BuildDirectionFromScreen(screenDirection, _normalizeDirection, _directionStrength);

        SetDirection(direction);
        Diagnostics.Log($"[ClickDirectionManager] MouseDown at {mousePosition}, direction={direction}");
    }

    private void HandlePointerUp(Vector2 mousePosition)
    {
        bool suppressRelease = _suppressReleaseUntilMouseUp;

        _inputBlocked = false;
        _suppressReleaseUntilMouseUp = false;
        IsMouseHeld = false;

        if (!suppressRelease)
        {
            LastReleaseWorld = ScreenToWorld(mousePosition);
            HasReleasePoint = true;
            LastReleaseTime = Time.time;
            Diagnostics.Log($"[ClickDirectionManager] MouseUp at world {LastReleaseWorld}");
        }

        ResetMotionState();
    }

    private void HandlePointerHeld(Vector2 mousePosition)
    {
        IsMouseHeld = true;

        Vector2 screenDirection = mousePosition - _screenCenter;
        UpdateAxes(mousePosition, screenDirection);

        if (screenDirection.sqrMagnitude > 0.0001f)
        {
            Vector2 direction = BuildDirectionFromScreen(screenDirection, _normalizeDirection, _directionStrength);
            SetDirection(direction);
        }

        CurrentPointerWorld = ScreenToWorld(mousePosition);
    }

    private bool TryConsumeManualPairingClick(Vector2 screenPosition)
    {
        if (ManualPairingManager.Instance == null)
            return false;

        if (!ManualPairingManager.Instance.HandleClick(screenPosition))
            return false;

        _inputBlocked = true;
        _suppressReleaseUntilMouseUp = true;
        IsMouseHeld = false;
        HasReleasePoint = false;
        CurrentPointerWorld = ScreenToWorld(screenPosition);
        ResetMotionState();
        Diagnostics.Log("[ClickDirectionManager] MouseDown consumed by manual pairing.");
        return true;
    }

    private bool RefreshCameraReference(bool logIfMissing = false)
    {
        if (_mainCamera != null)
            return true;

        _mainCamera = Camera.main;
        if (_mainCamera == null && logIfMissing)
            Diagnostics.Warn("[ClickDirectionManager] Main camera not found. Retrying next frame.");

        return _mainCamera != null;
    }

    private void RefreshScreenMetrics(bool force = false)
    {
        Vector2 currentSize = new Vector2(Screen.width, Screen.height);
        if (!force && currentSize == _lastScreenSize)
            return;

        _lastScreenSize = currentSize;
        _screenCenter = currentSize * 0.5f;
    }

    private void UpdateAxes(Vector2 mousePosition, Vector2 screenDirection)
    {
        float halfWidth = Mathf.Max(1f, Screen.width * 0.5f);
        float rawAxisX = ComputeNormalizedAxis(screenDirection.x, halfWidth, _axisDeadZone);
        _axisS = SmoothValue(_axisS, rawAxisX, _axisSmooth);
        HorizontalAxis = _axisS;

        float rawVelocityX = ComputeNormalizedVelocity(mousePosition.x, _prevMouseX, halfWidth, Time.deltaTime);
        _velS = SmoothValue(_velS, rawVelocityX, _velSmooth);
        HorizontalVelocity = _velS;
        _prevMouseX = mousePosition.x;

        float halfHeight = Mathf.Max(1f, Screen.height * 0.5f);
        float rawAxisY = ComputeNormalizedAxis(screenDirection.y, halfHeight, _axisDeadZone);
        _axisYS = SmoothValue(_axisYS, rawAxisY, _axisSmooth);
        VerticalAxis = _axisYS;

        float rawVelocityY = ComputeNormalizedVelocity(mousePosition.y, _prevMouseY, halfHeight, Time.deltaTime);
        _velYS = SmoothValue(_velYS, rawVelocityY, _velSmooth);
        VerticalVelocity = _velYS;
        _prevMouseY = mousePosition.y;
    }

    private void ResetMotionState()
    {
        HorizontalAxis = 0f;
        HorizontalVelocity = 0f;
        VerticalAxis = 0f;
        VerticalVelocity = 0f;
        _axisS = 0f;
        _velS = 0f;
        _axisYS = 0f;
        _velYS = 0f;
    }

    private Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return _mainCamera != null ? _mainCamera.ScreenToWorldPoint(screenPosition) : Vector2.zero;
    }

    private float SmoothValue(float current, float target, float smooth)
    {
        return Mathf.Lerp(current, target, Mathf.Max(0f, smooth) * Time.deltaTime);
    }

    private float ResolveMaxClickDistancePixels()
    {
        return Mathf.Max(0f, _maxClickDistance) * 100f;
    }

    private static Vector2 ClampScreenDirection(Vector2 screenDirection, float maxDistancePixels)
    {
        if (maxDistancePixels <= 0f || screenDirection.sqrMagnitude <= maxDistancePixels * maxDistancePixels)
            return screenDirection;

        return screenDirection.normalized * maxDistancePixels;
    }

    private static Vector2 BuildDirectionFromScreen(Vector2 screenDirection, bool normalizeDirection, float directionStrength)
    {
        if (screenDirection.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        if (!normalizeDirection)
            return screenDirection * directionStrength / 100f;

        return screenDirection.normalized * directionStrength;
    }

    private static float ComputeNormalizedAxis(float offsetFromCenter, float halfExtent, float deadZone)
    {
        float axis = Mathf.Clamp(offsetFromCenter / Mathf.Max(1f, halfExtent), -1f, 1f);
        return Mathf.Abs(axis) < Mathf.Max(0f, deadZone) ? 0f : axis;
    }

    private static float ComputeNormalizedVelocity(float currentPosition, float previousPosition, float halfExtent, float deltaTime)
    {
        float safeHalfExtent = Mathf.Max(1f, halfExtent);
        float safeDeltaTime = Mathf.Max(0.0001f, deltaTime);
        return ((currentPosition - previousPosition) / safeHalfExtent) / safeDeltaTime;
    }
}
