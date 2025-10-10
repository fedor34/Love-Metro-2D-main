using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active couples and triggers a 'station stop' when threshold is reached.
/// </summary>
public class CouplesManager : MonoBehaviour
{
    public static CouplesManager Instance { get; private set; }

    [SerializeField] private int _stationThreshold = 6; // remove couples when this many exist
    [Header("Auto-stop when no pairs possible")]
    [SerializeField] private bool _stopWhenNoPairs = true;
    [SerializeField] private float _checkInterval = 1.0f;
    [SerializeField] private float _cooldownAfterStop = 2.0f;
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

    private void Update()
    {
        if (!_stopWhenNoPairs) return;
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + _checkInterval;

        // If no potential opposite-sex singles remain, trigger station stop
        var all = Object.FindObjectsOfType<Passenger>();
        bool maleFree = false, femaleFree = false;
        for (int i = 0; i < all.Length; i++)
        {
            var p = all[i];
            if (p == null) continue;
            if (p.IsInCouple) continue;
            if (p.IsFemale) femaleFree = true; else maleFree = true;
            if (maleFree && femaleFree) break;
        }
        if (!(maleFree && femaleFree))
        {
            Debug.Log("[Pair][Station] No more opposite-sex singles. Triggering stop.");
            DespawnAllCouples();
            _nextCheckTime = Time.time + _cooldownAfterStop;
        }
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

        // Trigger station stop: freeze movement and spawn new passengers
        var train = Object.FindObjectOfType<TrainManager>();
        if (train != null)
        {
            train.StationStopAndSpawn(1.0f);
        }
    }
}
