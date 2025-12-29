using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active couples and triggers a 'station stop' when threshold is reached.
/// Оптимизирован для использования PassengerRegistry вместо FindObjectsOfType.
/// </summary>
public class CouplesManager : MonoBehaviour
{
    public static CouplesManager Instance { get; private set; }

    [SerializeField] private int _stationThreshold = 6;

    [Header("Auto-stop when no pairs possible")]
    [SerializeField] private bool _stopWhenNoPairs = true;
    [SerializeField] private float _checkInterval = 1.0f;
    [SerializeField] private float _cooldownAfterStop = 2.0f;

    [Header("Dependencies (cached)")]
    [SerializeField] private TrainManager _trainManager;

    private readonly List<Couple> _activeCouples = new List<Couple>();
    private float _nextCheckTime = 0f;

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
        // Кешируем TrainManager один раз при старте
        if (_trainManager == null)
        {
            _trainManager = Object.FindObjectOfType<TrainManager>();
        }
    }

    private void Update()
    {
        if (!_stopWhenNoPairs) return;
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + _checkInterval;

        // Используем PassengerRegistry вместо FindObjectsOfType
        int pairsPossible = GetPossiblePairsCount();

        if (pairsPossible <= 1)
        {
            int maleSingles = PassengerRegistry.Instance?.MaleSinglesCount ?? 0;
            int femaleSingles = PassengerRegistry.Instance?.FemaleSinglesCount ?? 0;
            Debug.Log($"[Pair][Station] Low pairs possible (<=1). males={maleSingles} females={femaleSingles}. Triggering stop.");
            DespawnAllCouples();
            _nextCheckTime = Time.time + _cooldownAfterStop;
        }
    }

    /// <summary>
    /// Возвращает количество возможных пар.
    /// Использует PassengerRegistry для оптимизации.
    /// </summary>
    private int GetPossiblePairsCount()
    {
        if (PassengerRegistry.Instance != null)
        {
            return PassengerRegistry.Instance.GetPossiblePairsCount();
        }

        // Fallback на старый метод если реестр не инициализирован
        var all = Object.FindObjectsOfType<Passenger>();
        int maleSingles = 0, femaleSingles = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var p = all[i];
            if (p == null || p.IsInCouple) continue;
            if (p.IsFemale) femaleSingles++; else maleSingles++;
        }
        return Mathf.Min(maleSingles, femaleSingles);
    }

    public void RegisterCouple(Couple couple)
    {
        if (couple == null) return;
        if (!_activeCouples.Contains(couple))
            _activeCouples.Add(couple);

        if (_activeCouples.Count >= _stationThreshold)
        {
            Debug.Log("[Pair][Station] Threshold reached: " + _activeCouples.Count + ". Despawning couples.");
            DespawnAllCouples();
        }
    }

    public void UnregisterCouple(Couple couple)
    {
        if (couple == null) return;
        _activeCouples.Remove(couple);
    }

    public void DespawnAllCouples()
    {
        // Copy to avoid modification during iteration
        var snapshot = new List<Couple>(_activeCouples);
        foreach (var couple in snapshot)
        {
            if (couple != null)
                couple.DespawnAtStation();
        }
        _activeCouples.Clear();

        // Trigger station stop using cached reference
        if (_trainManager != null)
        {
            _trainManager.StationStopAndSpawn(1.0f);
        }
    }

    public int ActiveCouplesCount => _activeCouples.Count;
}
