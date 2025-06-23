using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text), typeof(Animator))]
public class ScoreCounter : MonoBehaviour
{
    private int _score;
    [SerializeField] private int _initialScorePointsPerCouple;
    private float _currentScoreMultiplier = 1f;

    [SerializeField] private TMP_Text _floatingScorePref;
    [SerializeField] private float _minFloatingTextDisapearingDistance;
    [SerializeField] private float _floatingTextAcceleration;
    [SerializeField] private float _floatingTextInitialSpeed;
    [SerializeField] private float _floatingTextSpawnOffsetY;
    
    [SerializeField] private TMP_Text _matchesPerBrakeText; // Текст для отображения пар за торможение
    [SerializeField] private TrainManager _trainManager; // Прямая ссылка на TrainManager
    
    private int _matchesInCurrentBrake = 0; // Счетчик пар за текущее торможение
    private bool _brakingInProgress = false; // Флаг активного торможения

    private TMP_Text _textDisplay;
    private Animator _animator;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _textDisplay = GetComponent<TMP_Text>();
        _animator = GetComponent<Animator>();
        _rectTransform = GetComponent<RectTransform>();
        
        // Устанавливаем позицию в левый верхний угол, если не задано другое
        if (_rectTransform != null)
        {
            _rectTransform.anchorMin = new Vector2(0, 1);
            _rectTransform.anchorMax = new Vector2(0, 1);
            _rectTransform.pivot = new Vector2(0, 1);
            _rectTransform.anchoredPosition = new Vector2(20, -20);
        }
        
        // Обновляем отображение при старте
        UpdateScoreDisplay();
        UpdateMatchesPerBrakeDisplay();
    }
    
    private void Start()
    {
        // Добавляем подписку на события торможения в TrainManager
        if (_trainManager != null)
        {
            _trainManager.OnBrakeStart += StartBrakingSession;
            _trainManager.OnBrakeEnd += EndBrakingSession;
        }
    }

    public void UpdateScorePointFromMatching(Vector3 matchingPosition)
    {
        // Если идет торможение, увеличиваем счетчик пар
        if (_brakingInProgress)
        {
            _matchesInCurrentBrake++;
            UpdateMatchesPerBrakeDisplay();
        }
        
        StartCoroutine(ScorePointsFromMatching(matchingPosition));
    }
    
    // Метод для начала новой сессии торможения
    public void StartBrakingSession()
    {
        _brakingInProgress = true;
        _matchesInCurrentBrake = 0;
        UpdateMatchesPerBrakeDisplay();
    }
    
    // Метод для завершения сессии торможения
    public void EndBrakingSession()
    {
        _brakingInProgress = false;
        // Счетчик пар не сбрасываем, чтобы показывать результат последнего торможения
    }
    
    // Обновление отображения счета
    private void UpdateScoreDisplay()
    {
        if (_textDisplay != null)
        {
            _textDisplay.text = _score.ToString();
        }
    }
    
    // Обновление отображения количества пар за торможение
    private void UpdateMatchesPerBrakeDisplay()
    {
        if (_matchesPerBrakeText != null)
        {
            _matchesPerBrakeText.text = $"Пары за торможение: {_matchesInCurrentBrake}";
        }
    }

    private IEnumerator ScorePointsFromMatching(Vector3 initialMatchingPosition)
    {
       Vector3 matchingPosition = initialMatchingPosition + Vector3.up * _floatingTextSpawnOffsetY;
        TMP_Text floatingText = Instantiate(_floatingScorePref, matchingPosition, Quaternion.identity, transform.parent);
        RectTransform floatingTextTransform = floatingText.GetComponent<RectTransform>();
        float currentSpeed = _initialScorePointsPerCouple;
        floatingText.text = (_initialScorePointsPerCouple * _currentScoreMultiplier).ToString();
        _score += (int)(_initialScorePointsPerCouple * _currentScoreMultiplier);

        while(Vector3.Distance(floatingText.transform.position, transform.position) >= _minFloatingTextDisapearingDistance)
        {
            currentSpeed += _floatingTextAcceleration * Time.deltaTime * 0.5f;
            floatingText.transform.position += (transform.position - matchingPosition).normalized * currentSpeed * Time.deltaTime;
            currentSpeed += _floatingTextAcceleration * Time.deltaTime * 0.5f;
            yield return new WaitForEndOfFrame();
        }

        _animator.SetTrigger("Jump");
        UpdateScoreDisplay();
        Destroy(floatingText.gameObject);
    }
}
