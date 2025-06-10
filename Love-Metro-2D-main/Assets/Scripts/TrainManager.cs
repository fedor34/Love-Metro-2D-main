using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Этот класс управляет движением поезда и следит за событиями
// начала и конца торможения. Также он отвечает за остановки и
// повторный запуск состава после спавна пассажиров.

public class TrainManager : MonoBehaviour
{
    public delegate void StartInertia(Vector2 force);
    public StartInertia startInertia;

    // События для отслеживания начала и конца торможения
    public delegate void BrakeAction();
    public event BrakeAction OnBrakeStart;
    public event BrakeAction OnBrakeEnd;

    [SerializeField] private float _maxSpeed = 10f;
    [SerializeField] private float _minSpeed = 1f;
    [SerializeField] private float _acceleration;
    [SerializeField] private float _deceleration;
    [SerializeField] private SpriteRenderer _backGround;
    [SerializeField] private PassangersContainer _passangers;
    [SerializeField] private Transform _camera;
    [SerializeField] private float _cameraSpeed;
    [SerializeField] private float _cameraModifier;

    private Vector3 _cameraStartPosition;
    private float _currentSpeed;
    private bool _isAccelerated;
    private float _previousSpeed;

    // Счётчик пройденного расстояния
    private float _distanceTraveled = 0f;
    // Массив точек остановок (больше не используется)
    //[SerializeField] private float[] _stopDistances = new float[] { 0.2f, 0.4f, 0.6f };
    //private int _nextStopIndex = 0;
    private bool _isStopped = false;
    private PassangerSpawner _spawner;

    private bool stopCoroutineStarted = false; // Флаг для задержки остановки

    // Инициализация начальной скорости и кэширование важных объектов
    private void Start()
    {
        SetSpeed(_minSpeed);
        _cameraStartPosition = _camera.position;
        _spawner = FindObjectOfType<PassangerSpawner>();
    }

    // Устанавливает скорость поезда с учетом ограничений
    private void SetSpeed(float newSpeed)
    {
        _currentSpeed = newSpeed;
        _currentSpeed = Mathf.Clamp(_currentSpeed, _minSpeed, _maxSpeed);
    }

    private float _elapsedTime = 0;
    // Основной цикл управления движением поезда
    private void Update()
    {
        _previousSpeed = _currentSpeed;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isAccelerated = true;
            startInertia.Invoke(Vector2.left * (_maxSpeed - _currentSpeed) * (_acceleration / _deceleration));
            // Вызываем событие начала торможения
            OnBrakeStart?.Invoke();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            _isAccelerated = false;
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

        if (_isAccelerated)
        {
            SetSpeed(_currentSpeed + _acceleration * Time.deltaTime);
        }
        else
        {
            SetSpeed(_currentSpeed - _deceleration * Time.deltaTime);
        }

        _camera.position = Vector3.Lerp(_camera.position, 
            _cameraStartPosition + Vector3.right * _currentSpeed * _cameraModifier, _cameraSpeed);

        _backGround.material.SetFloat("_elapsedTime", _elapsedTime);
        _elapsedTime += Time.deltaTime * _currentSpeed;

        // Считаем пройденное расстояние (может пригодиться для статистики)
        _distanceTraveled += Mathf.Abs(_currentSpeed) * Time.deltaTime;
        Debug.Log($"[TrainManager] Speed: {_currentSpeed:F2}, Distance: {_distanceTraveled:F2}");
    }

    // Корутина ожидания перед полной остановкой поезда,
    // запускается когда больше нет возможных пар
    private IEnumerator StopAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(HandleTrainStop());
    }

    // Полная последовательность остановки и перезапуска поезда
    // удаляет старые пары, спавнит новых пассажиров и
    // вновь начинает движение
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
