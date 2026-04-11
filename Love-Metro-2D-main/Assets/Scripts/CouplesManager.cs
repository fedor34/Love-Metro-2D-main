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
        ResolveTrainManager();
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
        PassengerRegistry registry = PassengerRegistry.Instance;
        if (registry != null)
            return registry.GetPossiblePairsCount();

        return CountPossiblePairsWithoutRegistry();
    }

    private static int CountPossiblePairsWithoutRegistry()
    {
        Passenger[] passengers = Object.FindObjectsOfType<Passenger>();
        int maleSingles = 0;
        int femaleSingles = 0;

        for (int i = 0; i < passengers.Length; i++)
        {
            Passenger passenger = passengers[i];
            if (passenger == null || passenger.IsInCouple)
                continue;

            if (passenger.IsFemale)
                femaleSingles++;
            else
                maleSingles++;
        }

        return Mathf.Min(maleSingles, femaleSingles);
    }

    private void ResolveTrainManager()
    {
        if (_trainManager == null)
            _trainManager = Object.FindObjectOfType<TrainManager>();
    }

    private void TriggerStationStop()
    {
        ResolveTrainManager();
        if (_trainManager == null)
        {
            Diagnostics.Warn("[Pair][Station] TrainManager not found for station stop.");
            return;
        }

        _trainManager.StationStopAndSpawn(DefaultStationPauseSeconds);
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
            int maleSingles = PassengerRegistry.Instance?.MaleSinglesCount ?? 0;
            int femaleSingles = PassengerRegistry.Instance?.FemaleSinglesCount ?? 0;
            Diagnostics.Log($"[Pair][Station] reason=no-pairs males={maleSingles} females={femaleSingles}");
            return;
        }

        Diagnostics.Log($"[Pair][Station] reason=threshold activeCouples={_activeCouples.Count}");
    }
}
