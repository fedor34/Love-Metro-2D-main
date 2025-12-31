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
        
        StartCoroutine(ScorePointsFromMatching(matchingPosition, _initialScorePointsPerCouple));
    }

    // Award provided points (abilities may modify base value)
    public void AwardMatchPoints(Vector3 matchingPosition, int basePoints)
    {
        if (_brakingInProgress)
        {
            _matchesInCurrentBrake++;
            UpdateMatchesPerBrakeDisplay();
        }
        StartCoroutine(ScorePointsFromMatching(matchingPosition, basePoints));
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

    // Публичный штраф очков с плавающим текстом
    public void ApplyPenalty(int amount, Vector3 worldPosition)
    {
        if (amount <= 0) return;
        // Конвертируем мировые координаты удара в экранные, чтобы UI текст появлялся в нужной точке
        Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPosition) : worldPosition;
        StartCoroutine(ShowFloatingDelta(-amount, screenPos, new Color(1f, 0.25f, 0.25f)));
        _score -= amount;
        UpdateScoreDisplay();
    }

    private IEnumerator ShowFloatingDelta(int delta, Vector3 screenPos, Color color)
    {
        // Check if prefab is assigned before instantiating
        if (_floatingScorePref == null)
        {
            yield break;
        }

        Vector3 start = screenPos + Vector3.up * _floatingTextSpawnOffsetY;
        TMP_Text floatingText = Instantiate(_floatingScorePref, start, Quaternion.identity, transform.parent);
        floatingText.color = color;
        floatingText.text = delta.ToString();
        // Стартовая скорость штрафа равна скорости плюса: пропорциональна величине очков
        float curSpeed = Mathf.Abs(delta) * 0.5f;
        while (Vector3.Distance(floatingText.transform.position, transform.position) >= _minFloatingTextDisapearingDistance)
        {
            curSpeed += _floatingTextAcceleration * 0.5f * Time.deltaTime; // замедлим ускорение в 2 раза
            floatingText.transform.position += (transform.position - start).normalized * curSpeed * Time.deltaTime;
            yield return null;
        }
        Destroy(floatingText.gameObject);
    }

    private IEnumerator ScorePointsFromMatching(Vector3 initialMatchingPosition, int basePoints)
    {
        // Award points even if floating text prefab is not assigned
        _score += (int)(basePoints * _currentScoreMultiplier);
        UpdateScoreDisplay();

        // Check if prefab is assigned before instantiating
        if (_floatingScorePref == null)
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Jump");
            }
            yield break;
        }

        Vector3 matchingPosition = initialMatchingPosition + Vector3.up * _floatingTextSpawnOffsetY;
        TMP_Text floatingText = Instantiate(_floatingScorePref, matchingPosition, Quaternion.identity, transform.parent);
        RectTransform floatingTextTransform = floatingText.GetComponent<RectTransform>();
        float currentSpeed = basePoints * 0.5f; // замедлим стартовую скорость в 2 раза
        floatingText.text = (basePoints * _currentScoreMultiplier).ToString();

        while(Vector3.Distance(floatingText.transform.position, transform.position) >= _minFloatingTextDisapearingDistance)
        {
            currentSpeed += _floatingTextAcceleration * Time.deltaTime * 0.25f; // суммарно ускорение вдвое меньше
            floatingText.transform.position += (transform.position - matchingPosition).normalized * currentSpeed * Time.deltaTime;
            currentSpeed += _floatingTextAcceleration * Time.deltaTime * 0.25f;
            yield return new WaitForEndOfFrame();
        }

        if (_animator != null)
        {
            _animator.SetTrigger("Jump");
        }
        Destroy(floatingText.gameObject);
    }

    public int GetBasePointsPerCouple() => _initialScorePointsPerCouple;
}
