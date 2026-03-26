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

    [SerializeField] private TMP_Text _matchesPerBrakeText;
    [SerializeField] private TrainManager _trainManager;

    private int _matchesInCurrentBrake;
    private bool _brakingInProgress;

    private TMP_Text _textDisplay;
    private Animator _animator;
    private RectTransform _rectTransform;

    public int CurrentScore => _score;
    public int MatchesInCurrentBrake => _matchesInCurrentBrake;
    public bool IsBrakingInProgress => _brakingInProgress;

    private void Awake()
    {
        _textDisplay = GetComponent<TMP_Text>();
        _animator = GetComponent<Animator>();
        _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform != null)
        {
            _rectTransform.anchorMin = new Vector2(0, 1);
            _rectTransform.anchorMax = new Vector2(0, 1);
            _rectTransform.pivot = new Vector2(0, 1);
            _rectTransform.anchoredPosition = new Vector2(20, -20);
        }

        UpdateScoreDisplay();
        UpdateMatchesPerBrakeDisplay();
    }

    private void Start()
    {
        SubscribeToTrainManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTrainManager();
    }

    public void UpdateScorePointFromMatching(Vector3 matchingPosition)
    {
        AwardMatchPoints(matchingPosition, _initialScorePointsPerCouple);
    }

    public void AwardMatchPoints(Vector3 matchingPosition, int basePoints)
    {
        RegisterBrakeMatch();

        int awardedPoints = GetScaledPoints(basePoints);
        _score += awardedPoints;

        if (_floatingScorePref == null)
        {
            FinalizeMatchAward();
            return;
        }

        StartCoroutine(ScorePointsFromMatching(matchingPosition, awardedPoints));
    }

    public void StartBrakingSession()
    {
        _brakingInProgress = true;
        _matchesInCurrentBrake = 0;
        UpdateMatchesPerBrakeDisplay();
    }

    public void EndBrakingSession()
    {
        _brakingInProgress = false;
    }

    private void UpdateScoreDisplay()
    {
        if (_textDisplay != null)
            _textDisplay.text = _score.ToString();
    }

    private void UpdateMatchesPerBrakeDisplay()
    {
        if (_matchesPerBrakeText != null)
            _matchesPerBrakeText.text = $"Пары за торможение: {_matchesInCurrentBrake}";
    }

    public void ApplyPenalty(int amount, Vector3 worldPosition)
    {
        int penalty = Mathf.Max(0, amount);
        if (penalty == 0)
            return;

        Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPosition) : worldPosition;
        if (_floatingScorePref != null)
            StartCoroutine(ShowFloatingDelta(-penalty, screenPos, new Color(1f, 0.25f, 0.25f)));

        _score -= penalty;
        UpdateScoreDisplay();
    }

    private void SubscribeToTrainManager()
    {
        if (_trainManager == null)
            return;

        _trainManager.OnBrakeStart -= StartBrakingSession;
        _trainManager.OnBrakeEnd -= EndBrakingSession;
        _trainManager.OnBrakeStart += StartBrakingSession;
        _trainManager.OnBrakeEnd += EndBrakingSession;
    }

    private void UnsubscribeFromTrainManager()
    {
        if (_trainManager == null)
            return;

        _trainManager.OnBrakeStart -= StartBrakingSession;
        _trainManager.OnBrakeEnd -= EndBrakingSession;
    }

    private void RegisterBrakeMatch()
    {
        if (!_brakingInProgress)
            return;

        _matchesInCurrentBrake++;
        UpdateMatchesPerBrakeDisplay();
    }

    private int GetScaledPoints(int basePoints)
    {
        return Mathf.Max(0, Mathf.RoundToInt(basePoints * _currentScoreMultiplier));
    }

    private TMP_Text CreateFloatingText(Vector3 startPosition)
    {
        if (_floatingScorePref == null)
            return null;

        return Instantiate(_floatingScorePref, startPosition, Quaternion.identity, transform.parent);
    }

    private void FinalizeMatchAward()
    {
        if (_animator != null)
            _animator.SetTrigger("Jump");

        UpdateScoreDisplay();
    }

    private IEnumerator ShowFloatingDelta(int delta, Vector3 screenPos, Color color)
    {
        Vector3 start = screenPos + Vector3.up * _floatingTextSpawnOffsetY;
        TMP_Text floatingText = CreateFloatingText(start);
        if (floatingText == null)
            yield break;

        floatingText.color = color;
        floatingText.text = delta.ToString();

        if (Vector3.Distance(start, transform.position) < Mathf.Max(0.001f, _minFloatingTextDisapearingDistance))
        {
            Destroy(floatingText.gameObject);
            yield break;
        }

        float curSpeed = Mathf.Max(_floatingTextInitialSpeed, Mathf.Abs(delta) * 0.5f);
        while (Vector3.Distance(floatingText.transform.position, transform.position) >= _minFloatingTextDisapearingDistance)
        {
            Vector3 direction = (transform.position - floatingText.transform.position).normalized;
            curSpeed += _floatingTextAcceleration * 0.5f * Time.deltaTime;
            floatingText.transform.position += direction * curSpeed * Time.deltaTime;
            yield return null;
        }

        Destroy(floatingText.gameObject);
    }

    private IEnumerator ScorePointsFromMatching(Vector3 initialMatchingPosition, int awardedPoints)
    {
        Vector3 matchingPosition = initialMatchingPosition + Vector3.up * _floatingTextSpawnOffsetY;
        TMP_Text floatingText = CreateFloatingText(matchingPosition);
        if (floatingText == null)
        {
            FinalizeMatchAward();
            yield break;
        }

        floatingText.text = awardedPoints.ToString();

        if (Vector3.Distance(matchingPosition, transform.position) < Mathf.Max(0.001f, _minFloatingTextDisapearingDistance))
        {
            FinalizeMatchAward();
            Destroy(floatingText.gameObject);
            yield break;
        }

        float currentSpeed = Mathf.Max(_floatingTextInitialSpeed, awardedPoints * 0.5f);
        while (Vector3.Distance(floatingText.transform.position, transform.position) >= _minFloatingTextDisapearingDistance)
        {
            Vector3 direction = (transform.position - floatingText.transform.position).normalized;
            currentSpeed += _floatingTextAcceleration * Time.deltaTime * 0.5f;
            floatingText.transform.position += direction * currentSpeed * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        FinalizeMatchAward();
        Destroy(floatingText.gameObject);
    }

    public int GetBasePointsPerCouple() => _initialScorePointsPerCouple;
}
