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
    [Header("Special pair bonus")]
    [SerializeField] private int _specialPairBonus = 500;
    private bool _specialPairAwarded = false;

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

    // Публичный штраф очков с плавающим текстом
    public void ApplyPenalty(int amount, Vector3 worldPosition)
    {
        if (amount <= 0) return;
        StartCoroutine(ShowFloatingDelta(-amount, worldPosition, new Color(1f, 0.25f, 0.25f)));
        _score -= amount;
        UpdateScoreDisplay();
    }

    private IEnumerator ShowFloatingDelta(int delta, Vector3 worldPos, Color color)
    {
        Vector3 start = worldPos + Vector3.up * _floatingTextSpawnOffsetY;
        TMP_Text floatingText = Instantiate(_floatingScorePref, start, Quaternion.identity, transform.parent);
        floatingText.color = color;
        floatingText.text = delta.ToString();
        float curSpeed = _floatingTextInitialSpeed;
        while (Vector3.Distance(floatingText.transform.position, transform.position) >= _minFloatingTextDisapearingDistance)
        {
            curSpeed += _floatingTextAcceleration * Time.deltaTime;
            floatingText.transform.position += (transform.position - start).normalized * curSpeed * Time.deltaTime;
            yield return null;
        }
        Destroy(floatingText.gameObject);
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

    // Бонус за сведение особой пары (VIP)
    public void CheckSpecialCoupleBonus(Passenger a, Passenger b, Vector3 screenPos)
    {
        if (_specialPairAwarded) return;
        if (a == null || b == null) return;
        if (!a.IsVIP || !b.IsVIP) return;
        _specialPairAwarded = true;
        Diagnostics.Log($"[Score] VIP bonus awarded for pair: A={a.name}(F={a.IsFemale}) B={b.name}(F={b.IsFemale}) +{_specialPairBonus}");
        _score += _specialPairBonus;
        UpdateScoreDisplay();
        // Плавающий зелёный текст бонуса рядом с местом пары
        StartCoroutine(ShowFloatingDelta(+_specialPairBonus, screenPos, new Color(0.2f, 1f, 0.4f)));
    }
}
