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

    [SerializeField] private float _maxSpeed = 15f; // увеличили с 10 до 15
    [SerializeField] private float _minSpeed = 1f;
    [SerializeField] private float _acceleration = 8f; // установили более высокое ускорение
    [SerializeField] private float _deceleration = 6f; // установили более высокое замедление
    [SerializeField] private float _brakeDeceleration = 25f; // новый параметр для торможения пробелом
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
        // Ограничиваем скорость снизу нулем вместо _minSpeed при торможении
        float minLimit = _isBraking ? 0f : _minSpeed;
        _currentSpeed = Mathf.Clamp(_currentSpeed, minLimit, _maxSpeed);
    }

    private float _elapsedTime = 0;
    private void Update()
    {
        _previousSpeed = _currentSpeed;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isBraking = true;
            startInertia.Invoke(Vector2.left * (_maxSpeed - _currentSpeed) * (_acceleration / _deceleration));
            // Вызываем событие начала торможения
            OnBrakeStart?.Invoke();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            _isBraking = false;
            startInertia.Invoke(Vector2.right * (_currentSpeed - _minSpeed) * (_acceleration / _deceleration));
            // Вызываем событие окончания торможения
            OnBrakeEnd?.Invoke();
        }

        // Если поезд остановлен, не двигаем дальше
        if (_isStopped)
            return;

        // --- Остановка только если не осталось возможных пар ---
        int freeFemales = 0;
        int freeMales = 0;
        foreach (var p in _passangers.Passangers)
        {
            if (!p.IsInCouple)
            {
                if (p.IsFemale) freeFemales++;
                else freeMales++;
            }
        }
        int possiblePairs = Mathf.Min(freeFemales, freeMales);
        Debug.Log($"[TrainManager] Свободных женщин: {freeFemales}, мужчин: {freeMales}, возможных пар: {possiblePairs}, stopCoroutineStarted: {stopCoroutineStarted}, _isStopped: {_isStopped}");
        if (possiblePairs == 0 && !stopCoroutineStarted && !_isStopped)
        {
            stopCoroutineStarted = true;
            StartCoroutine(StopAfterDelay(3f));
        }
        if (possiblePairs > 0)
        {
            stopCoroutineStarted = false;
        }
        // --- Конец блока ---

        if (_isBraking)
        {
            // При торможении используем более сильное замедление
            SetSpeed(_currentSpeed - _brakeDeceleration * Time.deltaTime);
        }
        else
        {
            // Обычное ускорение до максимальной скорости
            if (_currentSpeed < _maxSpeed)
            {
                SetSpeed(_currentSpeed + _acceleration * Time.deltaTime);
            }
        }

        _camera.position = Vector3.Lerp(_camera.position, 
            _cameraStartPosition + Vector3.right * _currentSpeed * _cameraModifier, _cameraSpeed);

        // Для фона используем только положительную скорость
        float displaySpeed = Mathf.Max(0, _currentSpeed);
        
        // Передаем накопленное время для анимации фона
        _backGround.material.SetFloat("_elapsedTime", _elapsedTime);
        _elapsedTime += Time.deltaTime * displaySpeed;
        
        // Передаем текущую скорость как процент от максимальной для правильного отображения
        float speedPercent = displaySpeed / _maxSpeed;
        _backGround.material.SetFloat("_Speed", speedPercent);
        
        // Также передаем абсолютную скорость
        _backGround.material.SetFloat("_CurrentSpeed", displaySpeed);
        _backGround.material.SetFloat("_MaxSpeed", _maxSpeed);
        
        // Обновляем параллакс эффект только с положительной скоростью
        if (_parallaxEffect != null)
        {
            _parallaxEffect.SetTrainSpeed(displaySpeed);
        }

        // Считаем пройденное расстояние (может пригодиться для статистики)
        _distanceTraveled += Mathf.Abs(displaySpeed) * Time.deltaTime;
        Debug.Log($"[TrainManager] Speed: {_currentSpeed:F2}, Display Speed: {displaySpeed:F2}, Distance: {_distanceTraveled:F2}");
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
