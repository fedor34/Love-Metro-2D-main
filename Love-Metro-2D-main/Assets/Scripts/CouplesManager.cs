using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active couples and decides when the train should stop at a station.
/// </summary>
public class CouplesManager : MonoBehaviour
{
    private enum StationStopReason
    {
        ThresholdReached,
        NoPairsPossible
    }

    private const float DefaultStationPauseSeconds = 1.0f;

    public static CouplesManager Instance { get; private set; }

    [SerializeField] private int _stationThreshold = 6;

    [Header("Auto-stop when no pairs possible")]
    [SerializeField] private bool _stopWhenNoPairs = true;
    [SerializeField] private int _minPairsBeforeStop = 1;
    [SerializeField] private float _checkInterval = 1.0f;
    [SerializeField] private float _cooldownAfterStop = 2.0f;

    [Header("Dependencies (cached)")]
    [SerializeField] private TrainManager _trainManager;

    private readonly List<Couple> _activeCouples = new List<Couple>();
    private LoveMetro.Core.IPassengerRegistry _passengerRegistry;
    private LoveMetro.Train.ITrainMotionEvents _trainEvents;
    private LoveMetro.Train.IStationFlowService _stationFlow;
    private LoveMetro.Scoring.IScoreService _scoreService;
    private float _nextCheckTime;

    public int ActiveCouplesCount
    {
        get
        {
            CleanupMissingCouples();
            return _activeCouples.Count;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ResolveDependencies();
        ScheduleNextCheck(_checkInterval);
    }

    private void Update()
    {
        CleanupMissingCouples();
        TryHandleAutoStopWhenNoPairsRemain();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        _activeCouples.Clear();
    }

    public void RegisterCouple(Couple couple)
    {
        if (couple == null)
            return;

        CleanupMissingCouples();
        if (_activeCouples.Contains(couple))
            return;

        _activeCouples.Add(couple);
        if (ShouldStopAtThreshold())
            HandleStationStop(StationStopReason.ThresholdReached);
    }

    public void UnregisterCouple(Couple couple)
    {
        if (couple == null)
            return;

        _activeCouples.Remove(couple);
    }

    public void DespawnAllCouples()
    {
        DespawnAllCouplesInternal();
        TriggerStationStop();
        ScheduleNextCheck(_cooldownAfterStop);
    }

    public void Configure(
        LoveMetro.Core.IPassengerRegistry registry,
        LoveMetro.Train.ITrainMotionEvents trainEvents,
        LoveMetro.Train.IStationFlowService stationFlow,
        LoveMetro.Scoring.IScoreService scoreService)
    {
        _passengerRegistry = registry;
        _trainEvents = trainEvents;
        _stationFlow = stationFlow;
        _scoreService = scoreService;

        if (_trainManager == null && stationFlow is TrainManager trainManager)
            _trainManager = trainManager;
    }

    public void Configure(
        LoveMetro.Core.IPassengerRegistry registry,
        LoveMetro.Train.ITrainMotionEvents trainEvents,
        LoveMetro.Scoring.IScoreService scoreService)
    {
        Configure(registry, trainEvents, trainEvents as LoveMetro.Train.IStationFlowService, scoreService);
    }

    private void TryHandleAutoStopWhenNoPairsRemain()
    {
        if (!ShouldEvaluateAutoStop())
            return;

        if (GetPossiblePairsCount() > Mathf.Max(0, _minPairsBeforeStop))
        {
            ScheduleNextCheck(_checkInterval);
            return;
        }

        HandleStationStop(StationStopReason.NoPairsPossible);
    }

    private bool ShouldEvaluateAutoStop()
    {
        return _stopWhenNoPairs && Time.time >= _nextCheckTime;
    }

    private bool ShouldStopAtThreshold()
    {
        return _stationThreshold > 0 && _activeCouples.Count >= _stationThreshold;
    }

    private void HandleStationStop(StationStopReason reason)
    {
        LogStationStop(reason);
        DespawnAllCouplesInternal();
        TriggerStationStop();
        ScheduleNextCheck(_cooldownAfterStop);
    }

    private void DespawnAllCouplesInternal()
    {
        var snapshot = new List<Couple>(_activeCouples);
        _activeCouples.Clear();

        for (int i = 0; i < snapshot.Count; i++)
        {
            Couple couple = snapshot[i];
            if (couple != null)
                couple.DespawnAtStation();
        }
    }

    private int GetPossiblePairsCount()
    {
        ResolveDependencies();
        LoveMetro.Core.IPassengerRegistry registry = _passengerRegistry;
        if (registry != null)
            return registry.GetPossiblePairsCount();

        Diagnostics.Warn("[Pair][Station] Passenger registry is not configured; skipping no-pairs auto-stop check.");
        return int.MaxValue;
    }

    private void ResolveDependencies()
    {
        LoveMetro.Core.RuntimeServices services = LoveMetro.Core.RuntimeServices.Instance;

        _passengerRegistry ??= services.PassengerRegistry;
        _trainEvents ??= services.TrainMotionEvents;
        _stationFlow ??= services.StationFlowService ?? (_trainEvents as LoveMetro.Train.IStationFlowService);
        _scoreService ??= services.ScoreService;

        if (_stationFlow == null && _trainManager != null)
            _stationFlow = _trainManager;

        if (_trainManager == null && _stationFlow is TrainManager trainManager)
            _trainManager = trainManager;
    }

    private void TriggerStationStop()
    {
        ResolveDependencies();
        if (_stationFlow == null)
        {
            Diagnostics.Warn("[Pair][Station] Station flow service is not configured.");
            return;
        }

        _stationFlow.StationStopAndSpawn(DefaultStationPauseSeconds);
    }

    private void CleanupMissingCouples()
    {
        _activeCouples.RemoveAll(IsMissingCouple);
    }

    private static bool IsMissingCouple(Couple couple)
    {
        return couple == null || !couple;
    }

    private void ScheduleNextCheck(float delay)
    {
        _nextCheckTime = Time.time + Mathf.Max(0f, delay);
    }

    private void LogStationStop(StationStopReason reason)
    {
        if (reason == StationStopReason.NoPairsPossible)
        {
            ResolveDependencies();
            int maleSingles = _passengerRegistry?.MaleSinglesCount ?? 0;
            int femaleSingles = _passengerRegistry?.FemaleSinglesCount ?? 0;
            Diagnostics.Log($"[Pair][Station] reason=no-pairs males={maleSingles} females={femaleSingles}");
            return;
        }

        Diagnostics.Log($"[Pair][Station] reason=threshold activeCouples={_activeCouples.Count}");
    }
}
