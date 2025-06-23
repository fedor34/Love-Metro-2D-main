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
    [SerializeField] private float _maxSpeed = 15f;
    [SerializeField] private float _minSpeed = 1f;
    [SerializeField] private float _acceleration = 8f;   // Реалистичное ускорение
    [SerializeField] private float _deceleration = 3f;   // Плавное естественное замедление
    [SerializeField] private float _brakeDeceleration = 15f; // Резкое торможение по S

    [Header("Настройки камеры и фона")]
    [SerializeField] private SpriteRenderer _backGround;
    [SerializeField] private PassangersContainer _passangers;
    [SerializeField] private Transform _camera;
    [SerializeField] private float _cameraSpeed;
    [SerializeField] private float _cameraModifier;
    
    // Ссылка на параллакс эффект
    private ParallaxEffect _parallaxEffect;

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
    private PassangerSpawner _spawner;

    private bool stopCoroutineStarted = false; // Флаг для задержки остановки

    // Текущее фактическое ускорение (полезно для отладки или эффектов)
    private float _currentAcceleration = 0f;

    private void Start()
    {
        SetSpeed(_minSpeed);
        _cameraStartPosition = _camera.position;
        _spawner = FindObjectOfType<PassangerSpawner>();
        _parallaxEffect = FindObjectOfType<ParallaxEffect>();
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
        // 1.  Вызовы инерции (делегаты) – разово при смене фаз
        // ----------------------------------------------------
        // Сильный толчок назад в начале ускорения
        if (Input.GetKeyDown(KeyCode.Space))
        {
            startInertia?.Invoke(Vector2.right * _acceleration * 2f);
            OnBrakeEnd?.Invoke();
        }
        // Мягкий толчок вперёд, когда игрок отпускает ускорение
        if (Input.GetKeyUp(KeyCode.Space))
        {
            float predictedDecel = Mathf.Clamp(_currentSpeed, 0, _deceleration * 4f);
            startInertia?.Invoke(Vector2.left * predictedDecel);
        }

        // Резкий толчок вперёд при экстренном торможении (S)
        if (Input.GetKeyDown(KeyCode.S))
        {
            startInertia?.Invoke(Vector2.left * Mathf.Max(_currentSpeed, _brakeDeceleration * 2f));
            OnBrakeStart?.Invoke();
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            OnBrakeEnd?.Invoke();
        }

        // ----------------------------------------------------
        // 2.  Фактическая физика движения поезда
        // ----------------------------------------------------
        float accelerationValue = 0f;
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

        if (_camera != null)
        {
            _camera.position = Vector3.Lerp(_camera.position, 
                _cameraStartPosition + Vector3.right * _currentSpeed * _cameraModifier, _cameraSpeed);
        }

        // Для фона используем только положительную скорость
        float displaySpeed = Mathf.Max(0, _currentSpeed);
        
        // Передаем параметры в шейдер фона
        if (_backGround != null && _backGround.material != null)
        {
            _backGround.material.SetFloat("_elapsedTime", _elapsedTime);
            float speedPercent = displaySpeed / _maxSpeed;
            _backGround.material.SetFloat("_Speed", speedPercent);
            _backGround.material.SetFloat("_CurrentSpeed", displaySpeed);
            _backGround.material.SetFloat("_MaxSpeed", _maxSpeed);
        }
        _elapsedTime += Time.deltaTime * displaySpeed;
        
        // Обновляем параллакс эффект
        if (_parallaxEffect != null)
        {
            _parallaxEffect.SetTrainSpeed(displaySpeed);
        }

        // Считаем пройденное расстояние
        _distanceTraveled += Mathf.Abs(displaySpeed) * Time.deltaTime;
        //Debug.Log($"[TrainManager] Speed: {_currentSpeed:F2}, Display Speed: {displaySpeed:F2}, Distance: {_distanceTraveled:F2}");
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
        SetSpeed(0);
        // Ждём чуть-чуть для эффекта остановки
        yield return new WaitForSeconds(1.0f);

        // Удаляем все пары
        foreach (var couple in FindObjectsOfType<Couple>())
        {
            Destroy(couple.gameObject);
        }
        // Не трогаем пассажиров без пары

        // Ждём чуть-чуть для эффекта
        yield return new WaitForSeconds(0.5f);

        // Спавним новых пассажиров
        if (_spawner != null)
            _spawner.spawnPassangers();

        // Ждём и снова запускаем поезд
        yield return new WaitForSeconds(1.0f);
        _isStopped = false;
        SetSpeed(_minSpeed);
        _distanceTraveled = 0f; // Сброс расстояния после остановки
        stopCoroutineStarted = false; // Сброс флага

        Debug.Log("[TrainManager] Остановка поезда! Distance: " + _distanceTraveled);
    }
}
