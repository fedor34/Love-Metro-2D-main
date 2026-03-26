using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TrainManager : MonoBehaviour
{
    public delegate void StartInertia(Vector2 force);
    public StartInertia startInertia;

    public delegate void BrakeAction();
    public event BrakeAction OnBrakeStart;
    public event BrakeAction OnBrakeEnd;

    public static Vector2 LastInertiaImpulse { get; private set; }

    [Header("Train Turn Simulation")]
    [SerializeField] private float _turnAmplitudeDeg = 45f;
    [SerializeField] private float _turnSpeed = 0.6f;
    private float _turnPhase;

    [Header("Movement")]
    [SerializeField] private float _maxSpeed = 480f;
    [SerializeField] private float _minSpeed = 1f;
    [SerializeField] private float _acceleration = 180f;
    [SerializeField] private float _deceleration = 10f;
    [SerializeField] private float _brakeDeceleration = 25f;

    [Header("Passenger Start Impulse")]
    [SerializeField] private float _startImpulseSpeedThreshold = 2.0f;
    [SerializeField] private float _startBoost = 20f;

    [Header("Direction Change Impulse")]
    [SerializeField] private float _dirImpulseMin = 6f;
    [SerializeField] private float _dirImpulseScale = 35f;
    [SerializeField] private float _dirImpulseCooldown = 0.15f;
    [SerializeField] private float _dirFlickThreshold = 0.95f;

    [Header("Camera And Background")]
    [SerializeField] private SpriteRenderer _backGround;
    [SerializeField] private PassangersContainer _passangers;
    [SerializeField] private Transform _camera;
    [Header("Camera Motion")]
    [SerializeField] private float _cameraFollowStrength = 8f;
    [SerializeField] private float _cameraShakeAmplitude = 0.2f;
    [SerializeField] private float _cameraShakeSmooth = 5f;
    [SerializeField] private bool _cameraFollowHorizontal;
    private float _currentShakeOffset;

    [SerializeField] private ParallaxEffect _parallaxEffect;

    private Vector3 _cameraStartPosition;
    private float _currentSpeed;
    private bool _isBraking;
    private float _previousSpeed;
    private float _distanceTraveled;
    private bool _isStopped;
    [SerializeField] private PassangerSpawner _spawner;

    private bool stopCoroutineStarted;
    private float _currentAcceleration;
    private bool _accelImpulseGiven;
    private float _accelHoldTime;
    private float _lastAxis;
    private float _lastDirImpulseTime = -999f;

    [SerializeField] private float _stopEpsilon = 0.02f;

    public float GetCurrentSpeed()
    {
        return _currentSpeed;
    }

    private void Start()
    {
        SetSpeed(_minSpeed);
        _cameraStartPosition = _camera != null ? _camera.position : Vector3.zero;
        ApplySerializedFallbacks();
        CacheReferences();
    }

    private void CacheReferences()
    {
        EnsureSpawnerReference();
        EnsureParallaxReference();
    }

    private void SetSpeed(float newSpeed)
    {
        _currentSpeed = Mathf.Clamp(newSpeed, 0f, _maxSpeed);
    }

    private void Update()
    {
        _previousSpeed = _currentSpeed;
        _turnPhase += Time.deltaTime * _turnSpeed;

        bool isAccelerating = ClickDirectionManager.IsMouseHeld;
        UpdateAccelerationHoldTime(isAccelerating);
        HandlePointerInputTransitions();
        UpdateTrainMotion(isAccelerating);
        UpdateCameraState(isAccelerating);
        UpdateParallaxState(isAccelerating);

        _distanceTraveled += Mathf.Abs(_currentSpeed) * Time.deltaTime;
    }

    private static Vector2 Rotate(Vector2 velocity, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);
        return new Vector2(cosine * velocity.x - sine * velocity.y, sine * velocity.x + cosine * velocity.y);
    }

    public void StationStopAndSpawn(float pauseSeconds = 1.0f)
    {
        if (stopCoroutineStarted)
            return;

        Diagnostics.Log($"[Station] stop+spawn requested pause={pauseSeconds:F2}");
        StartCoroutine(StationStopRoutine(pauseSeconds));
    }

    private IEnumerator StationStopRoutine(float pauseSeconds)
    {
        stopCoroutineStarted = true;
        _isStopped = true;
        SetSpeed(0f);
        Diagnostics.Log("[Station] stop begin");
        OnBrakeStart?.Invoke();

        yield return new WaitForSeconds(Mathf.Max(0.05f, pauseSeconds));

        EnsurePassengersContainerReference();
        if (_passangers != null)
        {
            _passangers.DestroyAllPassengers();
        }
        else
        {
            Diagnostics.Warn("[Station] PassangersContainer not found before spawn");
        }

        EnsureSpawnerReference();
        _spawner?.SpawnPassengers();
        OnBrakeEnd?.Invoke();
        _isStopped = false;
        stopCoroutineStarted = false;
        Diagnostics.Log("[Station] stop end");
    }
}
