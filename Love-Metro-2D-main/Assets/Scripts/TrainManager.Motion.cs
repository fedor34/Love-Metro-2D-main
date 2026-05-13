using UnityEngine;

public partial class TrainManager
{
    private LoveMetro.Input.PointerIntent ResolvePointerIntent()
    {
        LoveMetro.Input.IInputIntentProvider provider = LoveMetro.Core.RuntimeServices.Instance.InputIntentProvider;
        return provider != null
            ? provider.CurrentIntent
            : new LoveMetro.Input.PointerIntent(
                ClickDirectionManager.CurrentClickDirection,
                ClickDirectionManager.HasClickDirection,
                ClickDirectionManager.IsMouseHeld,
                ClickDirectionManager.HorizontalAxis,
                ClickDirectionManager.HorizontalVelocity,
                ClickDirectionManager.VerticalAxis,
                ClickDirectionManager.VerticalVelocity,
                ClickDirectionManager.CurrentPointerWorld,
                ClickDirectionManager.LastReleaseWorld,
                ClickDirectionManager.HasReleasePoint,
                ClickDirectionManager.LastReleaseTime);
    }

    private void EnsureSpawnerReference()
    {
        if (_spawner == null)
            Diagnostics.Warn("[Train] PassangerSpawner is not configured.");
    }

    private void EnsureParallaxReference()
    {
        if (_parallaxEffect == null)
            Diagnostics.Warn("[Train] ParallaxEffect is not configured.");
    }

    private void EnsurePassengersContainerReference()
    {
        if (_passangers == null)
            Diagnostics.Warn("[Train] PassangersContainer is not configured.");
    }

    private void UpdateAccelerationHoldTime(bool isAccelerating)
    {
        if (Input.GetMouseButtonDown(0))
            _accelHoldTime = 0f;

        if (isAccelerating)
        {
            _accelHoldTime += Time.deltaTime;
        }
        else if (_accelHoldTime > 0f)
        {
            _accelHoldTime = 0f;
        }
    }

    private void HandlePointerInputTransitions()
    {
        if (_isStopped)
            return;

        if (Input.GetMouseButtonDown(0))
            HandlePointerPress();

        if (Input.GetMouseButtonUp(0))
            HandlePointerRelease();
    }

    private void HandlePointerPress()
    {
        bool wasAtRest = _currentSpeed <= _startImpulseSpeedThreshold;
        float boostSpeed = Mathf.Max(_minSpeed, _startBoost);

        SetSpeed(boostSpeed);
        OnBrakeEnd?.Invoke();
        BrakeEnded?.Invoke();

        if (_accelImpulseGiven || !wasAtRest)
            return;

        float accelMagnitude = Mathf.Max(4f, boostSpeed * 1.6f + boostSpeed * boostSpeed * 0.18f);
        DispatchInertiaImpulse(Vector2.left * accelMagnitude, "START impulse", "(mouse down from rest)");
        _accelImpulseGiven = true;
    }

    private void HandlePointerRelease()
    {
        OnBrakeStart?.Invoke();
        BrakeStarted?.Invoke();
        _isBraking = true;
    }

    private void UpdateTrainMotion(bool isAccelerating)
    {
        if (_isStopped)
            return;

        float accelerationValue = isAccelerating
            ? ResolveHeldAcceleration()
            : ResolveCoastingDeceleration();

        _currentAcceleration = accelerationValue;
        SetSpeed(_motionController != null
            ? _motionController.ApplyAcceleration(_currentSpeed, accelerationValue, Time.deltaTime)
            : _currentSpeed + accelerationValue * Time.deltaTime);
        ResetStartImpulseWhenNeeded();
    }

    private float ResolveHeldAcceleration()
    {
        LoveMetro.Input.PointerIntent pointerIntent = ResolvePointerIntent();
        float horizontalAxis = pointerIntent.HorizontalAxis;
        float horizontalVelocity = pointerIntent.HorizontalVelocity;
        float accelerationValue = CalculateHeldAcceleration(horizontalAxis, horizontalVelocity);

        _isBraking = accelerationValue < 0f;
        if (!_isBraking)
            OnBrakeEnd?.Invoke();

        TryEmitDirectionChangeImpulse(horizontalAxis, horizontalVelocity, pointerIntent.VerticalAxis);
        TryEmitFlickImpulse(horizontalVelocity, pointerIntent.VerticalVelocity);
        _lastAxis = horizontalAxis;

        return accelerationValue;
    }

    private float ResolveCoastingDeceleration()
    {
        _lastAxis = 0f;
        return -_deceleration * 0.35f;
    }

    private float CalculateHeldAcceleration(float horizontalAxis, float horizontalVelocity)
    {
        float accelerationValue;
        if (horizontalAxis > 0f)
        {
            accelerationValue = horizontalAxis * _acceleration * 4f;
        }
        else if (horizontalAxis < 0f)
        {
            accelerationValue = horizontalAxis * _brakeDeceleration * 3f;
        }
        else
        {
            accelerationValue = 0f;
        }

        if (horizontalVelocity > 0.7f)
            accelerationValue += _acceleration * 3f * Mathf.Clamp01(horizontalVelocity - 0.7f);

        if (horizontalVelocity < -0.7f)
            accelerationValue += -_brakeDeceleration * 4f * Mathf.Clamp01(-0.7f - horizontalVelocity);

        return accelerationValue;
    }

    private void TryEmitDirectionChangeImpulse(float horizontalAxis, float horizontalVelocity, float verticalAxis)
    {
        float deadZone = 0.06f;
        bool validPrev = Mathf.Abs(_lastAxis) > deadZone;
        bool validNow = Mathf.Abs(horizontalAxis) > deadZone;
        if (!validPrev || !validNow || Mathf.Sign(horizontalAxis) == Mathf.Sign(_lastAxis) || !CanEmitDirectionalImpulse())
            return;

        float magnitude = CalculateDirectionalImpulseMagnitude(horizontalAxis, horizontalVelocity);
        Vector2 impulse = BuildDirectionalImpulse(horizontalAxis, verticalAxis, magnitude);
        DispatchDirectionalImpulse(
            impulse,
            "DIR-CHANGE impulse",
            $"(x:{_lastAxis:F2}->{horizontalAxis:F2}, |vx|={Mathf.Abs(horizontalVelocity):F2}, mag={magnitude:F1})");
    }

    private void TryEmitFlickImpulse(float horizontalVelocity, float verticalVelocity)
    {
        if (Mathf.Abs(horizontalVelocity) <= _dirFlickThreshold || !CanEmitDirectionalImpulse())
            return;

        float magnitude = CalculateDirectionalImpulseMagnitude(horizontalVelocity, horizontalVelocity);
        Vector2 impulse = BuildDirectionalImpulse(horizontalVelocity, verticalVelocity, magnitude);
        DispatchDirectionalImpulse(
            impulse,
            "FLICK impulse",
            $"(vx={horizontalVelocity:F2}, mag={magnitude:F1})");
    }

    private bool CanEmitDirectionalImpulse()
    {
        return Time.time - _lastDirImpulseTime > _dirImpulseCooldown;
    }

    private float CalculateDirectionalImpulseMagnitude(float horizontalSource, float horizontalVelocity)
    {
        float asymmetry = horizontalSource > 0f ? 0.75f : 1.35f;
        return Mathf.Max(_dirImpulseMin, Mathf.Abs(horizontalVelocity) * _dirImpulseScale * asymmetry);
    }

    private Vector2 BuildDirectionalImpulse(float horizontalSource, float verticalSource, float magnitude)
    {
        float signX = horizontalSource > 0f ? -1f : 1f;
        float signY = verticalSource > 0f ? -1f : 1f;
        return new Vector2(signX * magnitude, signY * magnitude * 0.6f);
    }

    private void DispatchDirectionalImpulse(Vector2 impulse, string label, string details)
    {
        DispatchInertiaImpulse(impulse, label, details);
        _lastDirImpulseTime = Time.time;
    }

    private void DispatchInertiaImpulse(Vector2 impulse, string label, string details = null)
    {
        Vector2 rotatedImpulse = Rotate(impulse, Mathf.Sin(_turnPhase) * _turnAmplitudeDeg);
        startInertia?.Invoke(rotatedImpulse);
        InertiaImpulseDispatched?.Invoke(rotatedImpulse);
        LastInertiaImpulse = rotatedImpulse;

        if (string.IsNullOrEmpty(details))
            Debug.Log($"[Train] {label} {rotatedImpulse}");
        else
            Debug.Log($"[Train] {label} {rotatedImpulse} {details}");
    }

    private void ResetStartImpulseWhenNeeded()
    {
        if (_currentSpeed <= _startImpulseSpeedThreshold)
            _accelImpulseGiven = false;
    }

    private void UpdateCameraState(bool isAccelerating)
    {
        if (_camera == null)
            return;

        float followX = _cameraFollowHorizontal ? (_distanceTraveled * 1.2f) : 0f;
        float targetX = _cameraStartPosition.x + followX;
        float newX = Mathf.Lerp(_camera.position.x, targetX, _cameraFollowStrength * Time.deltaTime);
        float shakeAmplitude = _cameraShakeAmplitude * (isAccelerating ? 0.5f : 1f);
        float targetShake = Mathf.Clamp(-_currentAcceleration / _acceleration, -1f, 1f) * shakeAmplitude;

        _currentShakeOffset = Mathf.Lerp(_currentShakeOffset, targetShake, _cameraShakeSmooth * Time.deltaTime);
        _camera.position = new Vector3(newX, _cameraStartPosition.y + _currentShakeOffset, _cameraStartPosition.z);
    }

    private void UpdateParallaxState(bool isAccelerating)
    {
        float absoluteSpeed = Mathf.Abs(_currentSpeed);
        _parallaxEffect?.SetTrainSpeed(isAccelerating ? absoluteSpeed : 0f);
    }
}
