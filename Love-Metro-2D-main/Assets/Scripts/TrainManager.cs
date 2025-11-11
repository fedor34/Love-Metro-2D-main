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
    [SerializeField] private float _maxSpeed = 480f; // увеличен максимум вдвое
    [SerializeField] private float _minSpeed = 1f;
    [SerializeField] private float _acceleration = 180f;  // базовое ускорение (x2)
    [SerializeField] private float _deceleration = 10f;   // Замедление
    [SerializeField] private float _brakeDeceleration = 25f; // Торможение при отпускании
    [Header("Условия выдачи стартового импульса пассажирам")]
    [SerializeField] private float _startImpulseSpeedThreshold = 2.0f; // считаем поезд почти стоящим ниже этого порога
    [SerializeField] private float _startBoost = 20f; // более сильный старт

    [Header("Импульс при смене направления (жесты)")]
    [SerializeField] private float _dirImpulseMin = 6f;        // минимальная сила импульса
    [SerializeField] private float _dirImpulseScale = 35f;     // множитель от скорости перетаскивания
    [SerializeField] private float _dirImpulseCooldown = 0.15f;// кулдаун между импульсами
    [SerializeField] private float _dirFlickThreshold = 0.95f; // порог скорости жеста для повторных импульсов

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

    // Трек смены направления для инерционных импульсов
    private float _lastAxis = 0f;
    private float _lastDirImpulseTime = -999f;

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
        if (_maxSpeed < 460f) _maxSpeed = 480f;
        // Гарантируем повышенное ускорение даже если в сцене сохранено старое сериализованное
        if (_acceleration < 180f) _acceleration = 180f;
        if (_startImpulseSpeedThreshold < 0.5f) _startImpulseSpeedThreshold = 2.0f;
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
            // Начало удержания: мягкий старт при нажатии ЛКМ + стартовый импульс, если стояли
            if (Input.GetMouseButtonDown(0))
            {
                float prevSpeed = _currentSpeed;
                bool wasAtRest = prevSpeed <= _startImpulseSpeedThreshold;

                float boostSpeed = Mathf.Max(_minSpeed, _startBoost);
                SetSpeed(boostSpeed);
                OnBrakeEnd?.Invoke();

                // Однократный импульс пассажирам при отправке — только если стартовали из покоя
                if (!_accelImpulseGiven && wasAtRest)
                {
                    float baseSpeed = boostSpeed;
                    float accelMag = Mathf.Max(4f, baseSpeed * 1.6f + baseSpeed * baseSpeed * 0.18f);
                    var impulse = Vector2.left * accelMag;
                    impulse = Rotate(impulse, Mathf.Sin(_turnPhase) * _turnAmplitudeDeg);
                    startInertia?.Invoke(impulse);
                    LastInertiaImpulse = impulse;
                    _accelImpulseGiven = true;
                    Debug.Log($"[Train] START impulse {impulse} (mouse down from rest)");
                }
            }
            // На отпускании импульс не даём — управление по направлению
            if (Input.GetMouseButtonUp(0))
            {
                OnBrakeStart?.Invoke();
                _isBraking = true;
            }
        }

        // --- Фактическая физика движения поезда ---
        float accelerationValue = 0f;
        if (!_isStopped)
        {
            if (isAccelerating)
            {
                // Новый режим: горизонтальное перетаскивание управляет ускорением/торможением
                float x = ClickDirectionManager.HorizontalAxis; // -1..1
                float vx = ClickDirectionManager.HorizontalVelocity; // норм. скорость перетаскивания
                // Мёртвая зона уже учтена в менеджере; здесь только масштабируем
                if (x > 0f)
                {
                    accelerationValue = x * _acceleration * 4f; // справа — разгон
                }
                else if (x < 0f)
                {
                    accelerationValue = x * _brakeDeceleration * 3f; // слева — торможение
                }
                else
                {
                    accelerationValue = 0f; // палец по центру — скорость сохраняется
                }

                // Флики: резкий жест вправо/влево усиливает действие
                if (vx > 0.7f) accelerationValue += _acceleration * 3f * Mathf.Clamp01(vx - 0.7f);
                if (vx < -0.7f) accelerationValue += -_brakeDeceleration * 4f * Mathf.Clamp01(-0.7f - vx);

                _isBraking = accelerationValue < 0f;
                if (!_isBraking) OnBrakeEnd?.Invoke();

                // Инерционный импульс пассажирам при резкой смене направления
                float dead = 0.06f;
                bool validPrev = Mathf.Abs(_lastAxis) > dead;
                bool validNow  = Mathf.Abs(x) > dead;
                if (validPrev && validNow && Mathf.Sign(x) != Mathf.Sign(_lastAxis))
                {
                    if (Time.time - _lastDirImpulseTime > _dirImpulseCooldown)
                    {
                        float v = Mathf.Abs(vx);
                        // Асимметрия: при разгоне (x>0) слабее, при торможении (x<0) сильнее
                        float asym = x > 0f ? 0.75f : 1.35f;
                        float mag = Mathf.Max(_dirImpulseMin, v * _dirImpulseScale * asym);
                        Vector2 impulse = (x > 0f ? Vector2.left : Vector2.right) * mag;
                        impulse = Rotate(impulse, Mathf.Sin(_turnPhase) * _turnAmplitudeDeg);
                        startInertia?.Invoke(impulse);
                        LastInertiaImpulse = impulse;
                        _lastDirImpulseTime = Time.time;
                        Debug.Log($"[Train] DIR-CHANGE impulse {impulse} (x:{_lastAxis:F2}->{x:F2}, |vx|={v:F2}, mag={mag:F1})");
                    }
                }

                // Повторные импульсы по сильным фликам в ту же сторону
                if (Mathf.Abs(vx) > _dirFlickThreshold)
                {
                    if (Time.time - _lastDirImpulseTime > _dirImpulseCooldown)
                    {
                        float v = Mathf.Abs(vx);
                        float asym = vx > 0f ? 0.75f : 1.35f; // вправо (разгон) слабее, влево (тормоз) сильнее
                        float mag = Mathf.Max(_dirImpulseMin, v * _dirImpulseScale * asym);
                        Vector2 impulse = (vx > 0f ? Vector2.left : Vector2.right) * mag; // ускорение -> инерция противоположно
                        impulse = Rotate(impulse, Mathf.Sin(_turnPhase) * _turnAmplitudeDeg);
                        startInertia?.Invoke(impulse);
                        LastInertiaImpulse = impulse;
                        _lastDirImpulseTime = Time.time;
                        Debug.Log($"[Train] FLICK impulse {impulse} (vx={vx:F2}, mag={mag:F1})");
                    }
                }
                _lastAxis = x;
            }
            else
            {
                // Без удержания — лёгкое естественное замедление
                accelerationValue = -_deceleration * 0.35f;
                _lastAxis = 0f;
            }

            _currentAcceleration = accelerationValue;
            SetSpeed(_currentSpeed + accelerationValue * Time.deltaTime);

            // Сброс флага «выдан стартовый импульс» когда почти остановились
            if (_currentSpeed <= _startImpulseSpeedThreshold)
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

        // Требование: при отпускании ЛКМ фон мгновенно замирает
        _parallaxEffect?.SetTrainSpeed(ClickDirectionManager.IsMouseHeld ? absSpeed : 0f);
        
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
