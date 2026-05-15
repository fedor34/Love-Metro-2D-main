using LoveMetro.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreCounter : MonoBehaviour, IScoreService
{
    private const string ScoreHudCanvasName = "ScoreHudCanvas";
    private const string ScoreCounterBadgeObjectName = "ScoreCounterBadge";
    private const string ScoreCounterTextObjectName = "ScoreCounterText";
    private static readonly Vector2 ScoreCounterAnchor = new Vector2(0f, 1f);
    private static readonly Vector2 ScoreCounterPivot = new Vector2(0f, 1f);
    private static readonly Vector2 ScoreHudReferenceResolution = new Vector2(2400f, 1080f);
    private static readonly Vector2 ScoreCounterSizeDelta = new Vector2(260f, 92f);
    private static readonly Vector2 ScoreCounterAnchoredPosition = new Vector2(48f, -64f);
    private const float ScoreCounterFontSize = 58f;

    [SerializeField] private int _initialScorePointsPerCouple;
    private float _currentScoreMultiplier = 1f;

    [SerializeField] private TMP_Text _floatingScorePref;
    [SerializeField] private float _minFloatingTextDisapearingDistance;
    [SerializeField] private float _floatingTextAcceleration;
    [SerializeField] private float _floatingTextInitialSpeed;
    [SerializeField] private float _floatingTextSpawnOffsetY;

    [SerializeField] private TMP_Text _matchesPerBrakeText;
    [SerializeField] private TrainManager _trainManager;
    [SerializeField] private ScoreHudView _hudView;
    [SerializeField] private FloatingScorePresenter _floatingScorePresenter;

    private int _matchesInCurrentBrake;
    private bool _brakingInProgress;
    private ScoreService _scoreService;
    private LoveMetro.Train.ITrainMotionEvents _trainEvents;

    private TextMeshProUGUI _textDisplay;
    private TMP_Text _ownerTextDisplay;
    private Animator _animator;
    private RectTransform _rectTransform;
    private Canvas _scoreCanvas;
    private RectTransform _scoreCanvasRect;
    private RectTransform _scoreBadgeRect;
    private Image _scoreBadgeImage;
    private bool _loggedHudState;

    public int CurrentScore => _scoreService?.CurrentScore ?? 0;
    public int BasePointsPerCouple => GetBasePointsPerCouple();
    public int MatchesInCurrentBrake => _matchesInCurrentBrake;
    public bool IsBrakingInProgress => _brakingInProgress;

    public event System.Action<ScoreChange> ScoreChanged;

    private void Awake()
    {
        EnsureScoreService();
        LoveMetro.Core.RuntimeServices.Instance.RegisterScoreService(this);
        EnsureRequiredComponents();

        if (Application.isPlaying)
            ConfigureScoreLayout();
        else
        {
            CacheExistingScoreCanvasReference();
            EnsureHudTextDisplay();
        }

        ConfigureScoreDisplay();
        ConfigurePresentationComponents();
        UpdateScoreDisplay();
        UpdateMatchesPerBrakeDisplay();
    }

    private void Start()
    {
        SubscribeToTrainManager();
    }

    private void LateUpdate()
    {
        PlaceCounterInHudCorner();
        LogHudStateOnce();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        EnsureRequiredComponents(false);
        ConfigureScoreDisplay();
        UpdateScoreDisplay();
    }

    private void OnDestroy()
    {
        LoveMetro.Core.RuntimeServices.Instance.UnregisterScoreService(this);
        UnsubscribeFromTrainManager();
        if (_scoreService != null)
            _scoreService.ScoreChanged -= HandleScoreServiceChanged;
    }

    public void Configure(ScoreHudView view, FloatingScorePresenter presenter)
    {
        if (view != null)
            _hudView = view;

        if (presenter != null)
            _floatingScorePresenter = presenter;

        ConfigurePresentationComponents();
        UpdateScoreDisplay();
    }

    public void ConfigureTrainEvents(LoveMetro.Train.ITrainMotionEvents trainEvents)
    {
        if (trainEvents == null)
            return;

        UnsubscribeFromTrainManager();
        _trainEvents = trainEvents;
        if (trainEvents is TrainManager trainManager)
            _trainManager = trainManager;
        SubscribeToTrainManager();
    }

    public void UpdateScorePointFromMatching(Vector3 matchingWorldPosition)
    {
        AwardMatchPoints(matchingWorldPosition, _initialScorePointsPerCouple);
    }

    public ScoreChange AwardMatchPoints(Vector3 matchingWorldPosition, int basePoints)
    {
        EnsureScoreService();
        RegisterBrakeMatch();
        return _scoreService.AwardMatchPoints(matchingWorldPosition, basePoints);
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
        if (_hudView != null && _hudView.IsConfigured)
            _hudView.SetScore(CurrentScore);
        else if (_textDisplay != null)
            _textDisplay.text = CurrentScore.ToString();
    }

    private void UpdateMatchesPerBrakeDisplay()
    {
        if (_matchesPerBrakeText != null)
            _matchesPerBrakeText.text = $"Пары за торможение: {_matchesInCurrentBrake}";
    }

    public ScoreChange ApplyPenalty(int amount, Vector3 worldPosition)
    {
        EnsureScoreService();
        return _scoreService.ApplyPenalty(amount, worldPosition);
    }

    public void Reset(int score = 0)
    {
        EnsureScoreService();
        _matchesInCurrentBrake = 0;
        _brakingInProgress = false;
        UpdateMatchesPerBrakeDisplay();
        _scoreService.Reset(score);
    }

    private void EnsureScoreService()
    {
        if (_scoreService != null)
            return;

        _scoreService = new ScoreService(scoreMultiplier: _currentScoreMultiplier);
        _scoreService.ScoreChanged += HandleScoreServiceChanged;
    }

    private void HandleScoreServiceChanged(ScoreChange change)
    {
        UpdateScoreDisplay();
        if (change.Kind == ScoreChangeKind.MatchAward)
        {
            if (!TryPresentScoreChange(change, Color.white, FinalizeMatchAward, waitForEndOfFrame: true))
                FinalizeMatchAward();
        }
        else if (change.Kind == ScoreChangeKind.Penalty && change.Delta != 0)
        {
            TryPresentScoreChange(change, new Color(1f, 0.25f, 0.25f), completed: null, waitForEndOfFrame: false);
        }

        ScoreChanged?.Invoke(change);
    }

    private bool TryPresentScoreChange(ScoreChange change, Color color, System.Action completed, bool waitForEndOfFrame)
    {
        ConfigurePresentationComponents();
        if (_floatingScorePresenter == null)
            return false;

        return _floatingScorePresenter.TryPresent(
            new ScorePresentationRequest(change, color, waitForEndOfFrame),
            completed);
    }

    private void SubscribeToTrainManager()
    {
        ResolveTrainEvents();

        if (_trainEvents == null)
            return;

        _trainEvents.BrakeStarted -= StartBrakingSession;
        _trainEvents.BrakeEnded -= EndBrakingSession;
        _trainEvents.BrakeStarted += StartBrakingSession;
        _trainEvents.BrakeEnded += EndBrakingSession;
    }

    private void UnsubscribeFromTrainManager()
    {
        if (_trainEvents == null)
            return;

        _trainEvents.BrakeStarted -= StartBrakingSession;
        _trainEvents.BrakeEnded -= EndBrakingSession;
    }

    private void RegisterBrakeMatch()
    {
        if (!_brakingInProgress)
            return;

        _matchesInCurrentBrake++;
        UpdateMatchesPerBrakeDisplay();
    }

    private void EnsureRequiredComponents(bool addMissingText = true)
    {
        _ownerTextDisplay = GetComponent<TMP_Text>();
        DisableOwnerTextDisplay();

        _animator = GetComponent<Animator>();
        if (_animator != null)
            _animator.enabled = false;

        _rectTransform = GetComponent<RectTransform>();
    }

    private void ResolveTrainEvents()
    {
        if (_trainEvents == null)
        {
            _trainEvents = _trainManager != null
                ? (LoveMetro.Train.ITrainMotionEvents)_trainManager
                : null;
        }

        if (_trainManager == null && _trainEvents is TrainManager trainManager)
            _trainManager = trainManager;
    }

    private void ConfigureScoreLayout()
    {
        Canvas parentCanvas = ResolveScoreCanvas();
        if (parentCanvas != null)
        {
            ConfigureScoreCanvas(parentCanvas);
            _scoreCanvas = parentCanvas;
            _scoreCanvasRect = parentCanvas.GetComponent<RectTransform>();
        }

        EnsureHudTextDisplay();
        DisableOwnerTextDisplay();
        PlaceCounterInHudCorner();
    }

    private Canvas ResolveScoreCanvas()
    {
        if (_scoreCanvas != null)
            return _scoreCanvas;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            return parentCanvas;

        GameObject hudCanvasObject = new GameObject(
            ScoreHudCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        return hudCanvasObject.GetComponent<Canvas>();
    }

    private static void ConfigureScoreCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        RemoveMenuManagerFromHudCanvas(canvas);

        if (canvas.transform is RectTransform canvasRectTransform)
        {
            canvasRectTransform.anchorMin = Vector2.zero;
            canvasRectTransform.anchorMax = Vector2.one;
            canvasRectTransform.pivot = new Vector2(0.5f, 0.5f);
            canvasRectTransform.anchoredPosition = Vector2.zero;
            canvasRectTransform.sizeDelta = Vector2.zero;
            canvasRectTransform.localScale = Vector3.one;
            canvasRectTransform.localRotation = Quaternion.identity;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ScoreHudReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private static void RemoveMenuManagerFromHudCanvas(Canvas canvas)
    {
        if (canvas == null || canvas.name != ScoreHudCanvasName)
            return;

        MenuManager menuManager = canvas.GetComponent<MenuManager>();
        if (menuManager == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(menuManager);
        else
            UnityEngine.Object.DestroyImmediate(menuManager);
    }

    private void EnsureScoreCanvasReference()
    {
        if (_scoreCanvas != null && _scoreCanvasRect != null)
            return;

        if (!Application.isPlaying)
        {
            CacheExistingScoreCanvasReference();
            return;
        }

        Canvas parentCanvas = ResolveScoreCanvas();
        if (parentCanvas == null)
            return;

        ConfigureScoreCanvas(parentCanvas);
        _scoreCanvas = parentCanvas;
        _scoreCanvasRect = parentCanvas.GetComponent<RectTransform>();
    }

    private void CacheExistingScoreCanvasReference()
    {
        Canvas parentCanvas = _scoreCanvas != null ? _scoreCanvas : GetComponentInParent<Canvas>();

        _scoreCanvas = parentCanvas;
        _scoreCanvasRect = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;
    }

    private void ConfigureScoreDisplay()
    {
        EnsureHudTextDisplay();

        if (_textDisplay == null)
            return;

        ConfigureHudScoreFont(_textDisplay);
        _textDisplay.raycastTarget = false;
        _textDisplay.enableVertexGradient = false;
        _textDisplay.enableWordWrapping = false;
        _textDisplay.overflowMode = TextOverflowModes.Overflow;
        _textDisplay.alignment = TextAlignmentOptions.Center;
        _textDisplay.extraPadding = true;
        _textDisplay.margin = Vector4.zero;
        _textDisplay.enableAutoSizing = false;
        _textDisplay.fontSize = ScoreCounterFontSize;
        _textDisplay.color = Color.white;
        _textDisplay.enabled = true;
        _textDisplay.gameObject.SetActive(true);
    }

    private void ConfigurePresentationComponents()
    {
        if (_hudView == null)
            _hudView = GetComponent<ScoreHudView>() ?? gameObject.AddComponent<ScoreHudView>();

        _hudView.Configure(_scoreCanvas, _scoreBadgeRect, _textDisplay);

        if (_floatingScorePresenter == null)
            _floatingScorePresenter = GetComponent<FloatingScorePresenter>() ?? gameObject.AddComponent<FloatingScorePresenter>();

        _floatingScorePresenter.Configure(
            _hudView,
            _floatingScorePref,
            _minFloatingTextDisapearingDistance,
            _floatingTextAcceleration,
            _floatingTextInitialSpeed,
            _floatingTextSpawnOffsetY,
            _animator);
    }

    private void EnsureHudTextDisplay()
    {
        if (_textDisplay != null)
        {
            RemoveLegacyHudDuplicates();
            return;
        }

        EnsureScoreCanvasReference();
        if (_scoreCanvasRect == null)
            return;

        Transform badgeTransform = _scoreCanvasRect.Find(ScoreCounterBadgeObjectName);
        if (badgeTransform == null)
        {
            GameObject badgeObject = new GameObject(
                ScoreCounterBadgeObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            badgeObject.transform.SetParent(_scoreCanvasRect, false);
            badgeTransform = badgeObject.transform;
        }

        _scoreBadgeRect = badgeTransform.GetComponent<RectTransform>();
        _scoreBadgeImage = badgeTransform.GetComponent<Image>();
        if (_scoreBadgeImage == null)
            _scoreBadgeImage = badgeTransform.gameObject.AddComponent<Image>();

        _scoreBadgeImage.color = new Color(0f, 0f, 0f, 0.72f);
        _scoreBadgeImage.raycastTarget = false;
        RemoveLegacyHudDuplicates();

        Transform existingTextTransform = badgeTransform.Find(ScoreCounterTextObjectName);
        if (existingTextTransform == null)
        {
            GameObject textObject = new GameObject(
                ScoreCounterTextObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(badgeTransform, false);
            existingTextTransform = textObject.transform;
        }

        _textDisplay = existingTextTransform.GetComponent<TextMeshProUGUI>();
        if (_textDisplay == null)
            _textDisplay = existingTextTransform.gameObject.AddComponent<TextMeshProUGUI>();

        RectTransform textRect = _textDisplay.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        textRect.localScale = Vector3.one;
        textRect.localRotation = Quaternion.identity;
        textRect.SetAsLastSibling();
    }

    private void RemoveLegacyHudDuplicates()
    {
        if (_scoreCanvasRect == null)
            return;

        for (int i = _scoreCanvasRect.childCount - 1; i >= 0; i--)
        {
            Transform child = _scoreCanvasRect.GetChild(i);
            if (child == null || child == _scoreBadgeRect)
                continue;

            if (child.name == ScoreCounterTextObjectName || child.name == ScoreCounterBadgeObjectName)
                DestroyHudObject(child.gameObject);
        }
    }

    private static void DestroyHudObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }

    private void DisableOwnerTextDisplay()
    {
        TMP_Text[] ownerTexts = GetComponents<TMP_Text>();
        if (ownerTexts == null || ownerTexts.Length == 0)
            return;

        foreach (TMP_Text ownerText in ownerTexts)
        {
            if (ownerText == null || ownerText == _textDisplay)
                continue;

            ownerText.text = string.Empty;
            ownerText.enabled = false;
            ownerText.raycastTarget = false;
        }
    }

    private void ConfigureHudScoreFont(TMP_Text text)
    {
        if (text == null)
            return;

        if (text.font == null && TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        if (text.font == null)
            return;

        text.fontSharedMaterial = text.font.material;
    }

    private void PlaceCounterInHudCorner()
    {
        EnsureScoreCanvasReference();
        if (_scoreCanvas != null)
            ConfigureScoreCanvas(_scoreCanvas);

        EnsureHudTextDisplay();
        if (_scoreBadgeRect == null)
            return;

        _scoreBadgeRect.anchorMin = ScoreCounterAnchor;
        _scoreBadgeRect.anchorMax = ScoreCounterAnchor;
        _scoreBadgeRect.pivot = ScoreCounterPivot;
        _scoreBadgeRect.anchoredPosition = ScoreCounterAnchoredPosition;
        _scoreBadgeRect.sizeDelta = ScoreCounterSizeDelta;
        _scoreBadgeRect.localScale = Vector3.one;
        _scoreBadgeRect.localRotation = Quaternion.identity;
        _scoreBadgeRect.SetAsLastSibling();

    }

    private void LogHudStateOnce()
    {
        if (_loggedHudState || !Application.isPlaying)
            return;

        _loggedHudState = true;
        string canvasName = _scoreCanvas != null ? _scoreCanvas.name : "null";
        string textName = _textDisplay != null ? _textDisplay.name : "null";
        bool textEnabled = _textDisplay != null && _textDisplay.enabled;
        Vector3 badgePosition = _scoreBadgeRect != null ? _scoreBadgeRect.position : Vector3.zero;
        Debug.Log($"[ScoreCounter] HUD state: canvas={canvasName}, text={textName}, textEnabled={textEnabled}, screen={Screen.width}x{Screen.height}, badgePosition={badgePosition}, score={CurrentScore}");
    }

    private void FinalizeMatchAward()
    {
        if (_floatingScorePresenter != null)
            _floatingScorePresenter.TriggerCounterAnimation();
        else if (_animator != null && _animator.enabled)
            _animator.SetTrigger("Jump");

        UpdateScoreDisplay();
    }

    public int GetBasePointsPerCouple() => _initialScorePointsPerCouple;
}
