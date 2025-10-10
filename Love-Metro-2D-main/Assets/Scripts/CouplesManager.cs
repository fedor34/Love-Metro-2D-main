using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active couples and triggers a 'station stop' when threshold is reached.
/// </summary>
public class CouplesManager : MonoBehaviour
{
    public static CouplesManager Instance { get; private set; }

    [SerializeField] private int _stationThreshold = 6; // remove couples when this many exist
    private readonly List<Couple> _activeCouples = new List<Couple>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

