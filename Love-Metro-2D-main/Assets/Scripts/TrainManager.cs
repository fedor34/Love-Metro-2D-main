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

    [Header("Параметры движения")]
    [SerializeField] private float _maxSpeed = 25f;
    [SerializeField] private float _minSpeed = 1f;
    [SerializeField] private float _acceleration = 60f;  // Очень быстрое ускорение
    [SerializeField] private float _deceleration = 10f;   // Замедление
    [SerializeField] private float _brakeDeceleration = 25f; // Резкое торможение по S
    [SerializeField] private float _startBoost = 8f; // мгновенный прирост скорости при старте

    [Header("Настройки камеры и фона")]
    [SerializeField] private SpriteRenderer _backGround;
    [SerializeField] private PassangersContainer _passangers;
    [SerializeField] private Transform _camera;
    [Header("Камера: следование и качка")]
    [SerializeField] private float _cameraFollowStrength = 8f; // коэффициент lerp
    [SerializeField] private float _cameraShakeAmplitude = 0.2f; // макс смещение по Y
    [SerializeField] private float _cameraShakeSmooth = 5f; // скорость сглаживания
    private float _currentShakeOffset = 0f;

    // Ссылка на параллакс эффект
    [SerializeField] private ParallaxEffect _parallaxEffect;

    private Vector3 _cameraStartPosition;
    private float _currentSpeed;
    private bool _isBraking; // флаг для торможения
    private float _previousSpeed;

    // Счётчик пройденного расстояния
    private float _distanceTraveled = 0f;
    // Массив точек остановок (больше не используется)
    //[SerializeField] private float[] _stopDistances = new float[] { 0.2f, 0.4f, 0.6f };
    //private int _nextStopIndex = 0;
    private bool _isStopped = false;
    [SerializeField] private PassangerSpawner _spawner;

    private bool stopCoroutineStarted = false; // Флаг для задержки остановки

    // Текущее фактическое ускорение (полезно для отладки или эффектов)
    private float _currentAcceleration = 0f;

    // Публичный метод для получения текущей скорости
    public float GetCurrentSpeed()
    {
        return _currentSpeed;
    }

    private void Start()
    {
        SetSpeed(_minSpeed);
        _cameraStartPosition = _camera.position;
    }

    private void SetSpeed(float newSpeed)
    {
        _currentSpeed = newSpeed;
        // Ограничиваем скорость, но позволяем ей опускаться до 0
        _currentSpeed = Mathf.Clamp(newSpeed, 0f, _maxSpeed);
    }

    private float _elapsedTime = 0;
    private void Update()
    {
        _previousSpeed = _currentSpeed;
        
        bool isAccelerating = Input.GetKey(KeyCode.Space);
        _isBraking = Input.GetKey(KeyCode.S); // Торможение на S

        // ----------------------------------------------------
        // Проверка количества возможных пар для остановки
        // ----------------------------------------------------
        if (!_isStopped && !stopCoroutineStarted && _passangers != null)
        {
            int possiblePairs = CalculatePossiblePairs();
            if (possiblePairs <= 1)
            {
                Debug.Log($"TrainManager: Автоматическая остановка! Возможных пар: {possiblePairs}");
                stopCoroutineStarted = true;
                StartCoroutine(StopAfterDelay(2f));
            }
        }
        
        // Принудительная остановка для тестирования (клавиша R)
        if (Input.GetKeyDown(KeyCode.R) && !_isStopped && !stopCoroutineStarted)
        {
            Debug.Log("TrainManager: Принудительная остановка по клавише R");
            stopCoroutineStarted = true;
            StartCoroutine(StopAfterDelay(0.5f));
        }

        // ----------------------------------------------------
        // 1.  Вызовы инерции (делегаты) – разово при смене фаз
        // ----------------------------------------------------
        // Во время остановки игнорируем управление игрока
        if (!_isStopped)
        {
            // Начало ускорения: мягкий толчок назад + резкий старт
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Мгновенный прирост скорости
                SetSpeed(_currentSpeed + _startBoost);
                startInertia?.Invoke(Vector2.right * _acceleration * 0.2f);
                OnBrakeEnd?.Invoke();
            }
            // Конец ускорения: мягкий толчок вперёд
            if (Input.GetKeyUp(KeyCode.Space))
            {
                startInertia?.Invoke(Vector2.left * _deceleration);
            }

            // Резкое торможение (S)
            if (Input.GetKeyDown(KeyCode.S))
            {
                startInertia?.Invoke(Vector2.left * _currentSpeed * 0.5f);
                OnBrakeStart?.Invoke();
            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                OnBrakeEnd?.Invoke();
            }
        }

        // ----------------------------------------------------
        // 2.  Фактическая физика движения поезда
        // ----------------------------------------------------
        float accelerationValue = 0f;
        
        // Во время остановки игнорируем управление игрока
        if (!_isStopped)
        {
            if (_isBraking)
            {
                accelerationValue = -_brakeDeceleration;
            }
            else if (isAccelerating)
            {
                accelerationValue = _acceleration;
            }
            else
            {
                // Естественное трение/сопротивление движению
                accelerationValue = -_deceleration;
            }

            // Сохраняем для отладки
            _currentAcceleration = accelerationValue;

            // Интегрируем скорость
            SetSpeed(_currentSpeed + accelerationValue * Time.deltaTime);
        }

        // --- Камера: плавное следование + атмосферная качка ---
        if (_camera != null)
        {
            // 1. Горизонтальное следование за пройденным путём
            float targetX = _cameraStartPosition.x + _distanceTraveled;
            float newX = Mathf.Lerp(_camera.position.x, targetX, _cameraFollowStrength * Time.deltaTime);

            // 2. Вертикальная качка от ускорения
            float targetShake = Mathf.Clamp(-_currentAcceleration / _acceleration, -1f, 1f) * _cameraShakeAmplitude;
            _currentShakeOffset = Mathf.Lerp(_currentShakeOffset, targetShake, _cameraShakeSmooth * Time.deltaTime);

            _camera.position = new Vector3(newX, _cameraStartPosition.y + _currentShakeOffset, _cameraStartPosition.z);
        }

        // --- Фон и параллакс ---
        if (_backGround?.material != null)
        {
            _backGround.material.SetFloat("_elapsedTime", _elapsedTime);
            _backGround.material.SetFloat("_Speed", _currentSpeed / _maxSpeed);
            _backGround.material.SetFloat("_CurrentSpeed", _currentSpeed);
        }
        _elapsedTime += Time.deltaTime * _currentSpeed;
        
        _parallaxEffect?.SetTrainSpeed(_currentSpeed);

        // Считаем пройденное расстояние
        _distanceTraveled += Mathf.Abs(_currentSpeed) * Time.deltaTime;
    }

    // Метод для расчета количества возможных пар
    private int CalculatePossiblePairs()
    {
        if (_passangers == null || _passangers.Passangers == null)
            return 0;

        int males = 0;
        int females = 0;

        foreach (var passenger in _passangers.Passangers)
        {
            // Считаем только пассажиров, которые могут образовать пары
            if (passenger != null && passenger.IsMatchable && !passenger.IsInCouple)
            {
                if (passenger.IsFemale)
                    females++;
                else
                    males++;
            }
        }

        // Возвращаем количество возможных пар
        return Mathf.Min(males, females);
    }

    // Корутина задержки остановки поезда, если осталось 2 пассажира
    private IEnumerator StopAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(HandleTrainStop());
    }

    // Корутина остановки поезда, удаления пар и пассажиров, спавна новых
    private IEnumerator HandleTrainStop()
    {
        _isStopped = true;
        
        // Плавное торможение поезда до полной остановки
        float originalSpeed = _currentSpeed;
        float brakeTime = 2.0f; // Время торможения
        float elapsedTime = 0f;
        
        // Сигнал инерции для торможения
        startInertia?.Invoke(Vector2.left * _currentSpeed * 0.8f);
        OnBrakeStart?.Invoke();
        
        while (elapsedTime < brakeTime && _currentSpeed > 0.1f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / brakeTime;
            // Плавное замедление по кривой
            float newSpeed = Mathf.Lerp(originalSpeed, 0f, t * t); // Квадратичная кривая для реалистичного торможения
            SetSpeed(newSpeed);
            yield return null;
        }
        
        // Окончательная остановка
        SetSpeed(0);
        OnBrakeEnd?.Invoke();
        
        // Ждём момент для эффекта полной остановки
        yield return new WaitForSeconds(0.5f);

        // Удаляем все пары
        foreach (var couple in FindObjectsOfType<Couple>())
        {
            Destroy(couple.gameObject);
        }
        
        // Принудительно очищаем список от удаленных объектов
        if (_passangers != null && _passangers.Passangers != null)
        {
            _passangers.Passangers.RemoveAll(p => p == null);
            Debug.Log($"TrainManager: После очистки осталось {_passangers.Passangers.Count} пассажиров");
        }
        
        // Одиночные пассажиры остаются в поезде
        Debug.Log("TrainManager: Пары удалены, одиночные пассажиры остаются в поезде");

        // Ждём чуть-чуть для эффекта
        yield return new WaitForSeconds(0.5f);

        // Спавним новых пассажиров
        if (_spawner != null)
        {
            Debug.Log("TrainManager: Вызываем спавн новых пассажиров на остановке");
            _spawner.spawnPassangers();
        }
        else
        {
            Debug.LogError("TrainManager: Спавнер не назначен!");
        }

        // Ждём и снова запускаем поезд
        yield return new WaitForSeconds(1.5f);
        _isStopped = false;
        SetSpeed(_minSpeed);
        _distanceTraveled = 0f; // Сброс расстояния после остановки
        stopCoroutineStarted = false; // Сброс флага
    }
}
