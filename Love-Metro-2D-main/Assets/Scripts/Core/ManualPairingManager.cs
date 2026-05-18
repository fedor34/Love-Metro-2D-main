using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages manual pairing of passengers via click interaction.
/// Allows players to click two nearby compatible passengers to force them into a couple.
/// Supports one-click pairing if passengers overlap.
/// </summary>
public class ManualPairingManager : MonoBehaviour, LoveMetro.Input.IManualPairingService
{
    public static ManualPairingManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _maxPairingDistance = 3.0f;
    [SerializeField] private float _clickRadius = 0.4f;
    [SerializeField] private float _verticalSearchFactor = 2.0f;

    private readonly List<Passenger> _clickedPassengers = new List<Passenger>(4);
    private readonly HashSet<Passenger> _clickedPassengerSet = new HashSet<Passenger>();
    private Collider2D[] _overlapHits = new Collider2D[16];

    private const int MaxOverlapHits = 128;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoveMetro.Core.RuntimeServices.Instance.RegisterManualPairingService(this);
    }

    private void OnDestroy()
    {
        LoveMetro.Core.RuntimeServices.Instance.UnregisterManualPairingService(this);

        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Handles a click at the given screen position.
    /// Returns true if the click interacted with a passenger (select/pair), false otherwise.
    /// </summary>
    public bool HandleClick(Vector2 screenPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        CollectClickedPassengers(worldPos, _clickedPassengers);
        if (_clickedPassengers.Count == 0)
            return false;

        AttemptOverlapPairing(_clickedPassengers);
        return true;
    }

    private void CollectClickedPassengers(Vector2 worldPos, List<Passenger> clickedPassengers)
    {
        clickedPassengers.Clear();
        _clickedPassengerSet.Clear();

        Vector2 boxSize = new Vector2(_clickRadius * 2f, _clickRadius * 2f * _verticalSearchFactor);
        int hitCount = CollectOverlapHits(worldPos, boxSize);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _overlapHits[i];
            if (hit == null || !hit.TryGetComponent<Passenger>(out var passenger))
                continue;

            if (_clickedPassengerSet.Add(passenger))
                clickedPassengers.Add(passenger);
        }
    }

    private int CollectOverlapHits(Vector2 worldPos, Vector2 boxSize)
    {
        int hitCount;
        do
        {
            hitCount = Physics2D.OverlapBoxNonAlloc(worldPos, boxSize, 0f, _overlapHits);
            if (hitCount < _overlapHits.Length || _overlapHits.Length >= MaxOverlapHits)
                return hitCount;

            int nextSize = Mathf.Min(_overlapHits.Length * 2, MaxOverlapHits);
            if (nextSize == _overlapHits.Length)
                return hitCount;

            _overlapHits = new Collider2D[nextSize];
        }
        while (true);
    }

    private bool AttemptOverlapPairing(List<Passenger> group)
    {
        if (group == null || group.Count < 2)
            return false;

        for (int i = 0; i < group.Count; i++)
        {
            for (int j = i + 1; j < group.Count; j++)
            {
                Passenger p1 = group[i];
                Passenger p2 = group[j];
                if (!CanPair(p1, p2))
                    continue;

                PairPassengers(p1, p2);
                return true;
            }
        }

        return false;
    }

    private bool CanPair(Passenger p1, Passenger p2)
    {
        LoveMetro.Pairing.IPairingService service = LoveMetro.Core.RuntimeServices.Instance.PairingService;
        if (service != null)
        {
            var result = service.Evaluate(new LoveMetro.Pairing.PairingRequest(p1, p2, _maxPairingDistance, "manual"));
            return result.Success;
        }

        if (p1 == null || !p1.CanMatchWith(p2))
            return false;

        float maxPairingDistanceSq = _maxPairingDistance * _maxPairingDistance;
        return ((Vector2)(p1.transform.position - p2.transform.position)).sqrMagnitude <= maxPairingDistanceSq;
    }

    private void PairPassengers(Passenger p1, Passenger p2)
    {
        Debug.Log($"[ManualPairing] Pairing {p1.name} and {p2.name}");

        LoveMetro.Pairing.IPairingService service = LoveMetro.Core.RuntimeServices.Instance.PairingService;
        if (service != null && service.TryPair(new LoveMetro.Pairing.PairingRequest(p1, p2, _maxPairingDistance, "manual"), out _))
            return;

        p1.ForceToMatchingState(p2);
        p2.ForceToMatchingState(p1);
    }
}
