using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public delegate void StartInertia(Vector2 force);
    public StartInertia startInertia;

    // События для отслеживания начала и конца торможения
    public delegate void BrakeAction();
    public event BrakeAction OnBrakeStart;
    public event BrakeAction OnBrakeEnd;

    // Вектор последнего импульса инерции (для HUD-стрелки)
    public static Vector2 LastInertiaImpulse { get; private set; }

    [Header("Повороты поезда (симуляция)")]
    [SerializeField] private float _turnAmplitudeDeg = 45f; // максимальный угол отклонения импульса
    [SerializeField] private float _turnSpeed = 0.6f;       // скорость изменения поворота
    private float _turnPhase = 0f;

    [Header("Параметры движения")]
    [SerializeField] private float _maxSpeed = 240f; // значительно повышен максимум
    [SerializeField] private float _minSpeed = 1f;
    [SerializeField] private float _acceleration = 90f;  // базовое ускорение
    [SerializeField] private float _deceleration = 10f;   // Замедление
    [SerializeField] private float _brakeDeceleration = 25f; // Торможение при отпускании
    [SerializeField] private float _startBoost = 20f; // более сильный старт

    [Header("Настройки камеры и фона")]
    [SerializeField] private SpriteRenderer _backGround;
    [SerializeField] private PassangersContainer _passangers;
    [SerializeField] private Transform _camera;
    [Header("Камера: следование и качка")]
    [SerializeField] private float _cameraFollowStrength = 8f; // коэффициент lerp
    [SerializeField] private float _cameraShakeAmplitude = 0.2f; // макс смещение по Y
    [SerializeField] private float _cameraShakeSmooth = 5f; // скорость сглаживания
    [SerializeField] private bool _cameraFollowHorizontal = false; // по умолчанию не следуем по X
    private float _currentShakeOffset = 0f;

    // Ссылка на параллакс эффект
    [SerializeField] private ParallaxEffect _parallaxEffect;

    private Vector3 _cameraStartPosition;
    private float _currentSpeed;
    private bool _isBraking; // флаг для торможения
    private float _previousSpeed;

    // Счётчик пройденного расстояния
    private float _distanceTraveled = 0f;
    private bool _isStopped = false;
    [SerializeField] private PassangerSpawner _spawner;

    private bool stopCoroutineStarted = false; // Флаг для задержки остановки

    // Текущее фактическое ускорение (полезно для отладки или эффектов)
    private float _currentAcceleration = 0f;

    // Флаг: импульс ускорения пассажирам уже выдан при текущем запуске
    private bool _accelImpulseGiven = false;

    // Время удержания ЛКМ в текущем цикле ускорения
    private float _accelHoldTime = 0f;

    // Публичный метод для получения текущей скорости
    public float GetCurrentSpeed()
    {
        return _currentSpeed;
    }

    private void Start()
    {
        SetSpeed(_minSpeed);
        _cameraStartPosition = _camera.position;
        // Гарантируем высокий максимум, если проект/сцена содержит старое значение
        if (_maxSpeed < 220f) _maxSpeed = 240f;
        if (_spawner == null)
        {
            _spawner = FindObjectOfType<PassangerSpawner>();
        }
        if (_parallaxEffect == null)
        {
            _parallaxEffect = FindObjectOfType<ParallaxEffect>();
        }
    }

    private void SetSpeed(float newSpeed)
    {
        _currentSpeed = Mathf.Clamp(newSpeed, 0f, _maxSpeed);
    }

    [SerializeField] private float _stopEpsilon = 0.02f;
    
    private void Update()
    {
        _previousSpeed = _currentSpeed;
        _turnPhase += Time.deltaTime * _turnSpeed;
        bool isAccelerating = ClickDirectionManager.IsMouseHeld;

        // Трек времени удержания для квадратичного бонуса скорости
        if (Input.GetMouseButtonDown(0))
        {
            _accelHoldTime = 0f;
        }
        if (isAccelerating)
        {
            _accelHoldTime += Time.deltaTime;
        }
        else if (_accelHoldTime > 0f)
        {
            _accelHoldTime = 0f;
        }

        if (!_isStopped)
        {
            // Начало ускорения: мягкий старт при нажатии ЛКМ
            if (Input.GetMouseButtonDown(0))
            {
                // Запоминаем, что были почти в покое до старта
                bool wasAtRest = _currentSpeed <= _minSpeed + _stopEpsilon;
                
                // Мгновенно устанавливаем скорость = _startBoost (минимум _minSpeed)
                float boostSpeed = Mathf.Max(_minSpeed, _startBoost);
                SetSpeed(boostSpeed);
                
                // Однократный импульс пассажирам при отправке — только если стартовали из покоя
                if (!_accelImpulseGiven && wasAtRest)
                {
                    float baseSpeed = boostSpeed;
                    float accelMag = Mathf.Max(4f, baseSpeed * 1.6f + baseSpeed * baseSpeed * 0.18f);
                    var impulse = Vector2.left * accelMag;
                    impulse = Rotate(impulse, Mathf.Sin(_turnPhase) * _turnAmplitudeDeg);
                    startInertia?.Invoke(impulse);
                    LastInertiaImpulse = impulse;
                    Debug.Log($"[Train] ACCEL impulse {impulse} (mouse down, set speed={boostSpeed:F1}, quad)");
                    _accelImpulseGiven = true;
                }
                OnBrakeEnd?.Invoke();
            }
            // Конец удержания: торможение при отпускании ЛКМ и однократный импульс пассажирам
            if (Input.GetMouseButtonUp(0))
            {
                float s = Mathf.Max(0f, _currentSpeed);
                float brakeMag = Mathf.Max(8f, s * 2.6f + s * s * 0.45f);
                var impulse = Vector2.right * brakeMag;
                impulse = Rotate(impulse, Mathf.Sin(_turnPhase) * _turnAmplitudeDeg);
                startInertia?.Invoke(impulse);
                LastInertiaImpulse = impulse;
                Debug.Log($"[Train] BRAKE impulse {impulse} (mouse up, quad)");
                OnBrakeStart?.Invoke();
                _isBraking = true;
            }
        }

        // --- Фактическая физика движения поезда ---
        float accelerationValue = 0f;
        if (!_isStopped)
        {
            if (_isBraking)
            {
                accelerationValue = -_brakeDeceleration;
                if (_currentSpeed <= _minSpeed || isAccelerating)
                {
                    _isBraking = false;
                    OnBrakeEnd?.Invoke();
                }
            }
            else if (isAccelerating)
            {
                // Восстановлен сильный базовый разгон + квадратичный бонус от времени удержания
                float normHold = Mathf.Clamp01(_accelHoldTime / 1.5f); // 0..1 за ~1.5 c
                float bonus = 8f * normHold * normHold;                // 0..8
                accelerationValue = _acceleration * (4f + bonus);
            }
            else
            {
                accelerationValue = -_deceleration;
            }

            _currentAcceleration = accelerationValue;
            SetSpeed(_currentSpeed + accelerationValue * Time.deltaTime);

            // Сброс флага «выдан стартовый импульс» когда почти остановились
            if (_currentSpeed <= _minSpeed + _stopEpsilon)
            {
                _accelImpulseGiven = false;
            }
        }

        // --- Камера ---
        if (_camera != null)
        {
            float followX = _cameraFollowHorizontal ? (_distanceTraveled * 1.2f) : 0f;
            float targetX = _cameraStartPosition.x + followX;
            float newX = Mathf.Lerp(_camera.position.x, targetX, _cameraFollowStrength * Time.deltaTime);
            float shakeAmp = _cameraShakeAmplitude * (ClickDirectionManager.IsMouseHeld ? 0.5f : 1f);
            float targetShake = Mathf.Clamp(-_currentAcceleration / _acceleration, -1f, 1f) * shakeAmp;
            _currentShakeOffset = Mathf.Lerp(_currentShakeOffset, targetShake, _cameraShakeSmooth * Time.deltaTime);
            _camera.position = new Vector3(newX, _cameraStartPosition.y + _currentShakeOffset, _cameraStartPosition.z);
        }

        float absSpeed = Mathf.Abs(_currentSpeed);

        // Увеличиваем визуальную скорость параллакса — напрямую от скорости поезда
        _parallaxEffect?.SetTrainSpeed(absSpeed);
        
        _distanceTraveled += Mathf.Abs(_currentSpeed) * Time.deltaTime;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad); float s = Mathf.Sin(rad);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }

    // Force brief station stop: freeze speed and spawn fresh passengers
    public void StationStopAndSpawn(float pauseSeconds = 1.0f)
    {
        if (stopCoroutineStarted) return;
        StartCoroutine(StationStopRoutine(pauseSeconds));
    }

    private System.Collections.IEnumerator StationStopRoutine(float pauseSeconds)
    {
        stopCoroutineStarted = true;
        _isStopped = true;
        SetSpeed(0f);
        OnBrakeStart?.Invoke();
        yield return new WaitForSeconds(Mathf.Max(0.05f, pauseSeconds));
        if (_spawner == null) _spawner = FindObjectOfType<PassangerSpawner>();
        _spawner?.spawnPassangers();
        OnBrakeEnd?.Invoke();
        _isStopped = false;
        stopCoroutineStarted = false;
    }
}
