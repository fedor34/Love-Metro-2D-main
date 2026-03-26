using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages manual pairing of passengers via click interaction.
/// Allows players to click two nearby compatible passengers to force them into a couple.
/// Supports one-click pairing if passengers overlap.
/// </summary>
public class ManualPairingManager : MonoBehaviour
{
    public static ManualPairingManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _maxPairingDistance = 3.0f;
    [SerializeField] private float _clickRadius = 0.4f;
    [SerializeField] private float _verticalSearchFactor = 2.0f;

    private readonly List<Passenger> _clickedPassengers = new List<Passenger>(4);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
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

        Vector2 boxSize = new Vector2(_clickRadius * 2f, _clickRadius * 2f * _verticalSearchFactor);
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, boxSize, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (!hit.TryGetComponent<Passenger>(out var passenger))
                continue;

            if (!clickedPassengers.Contains(passenger))
                clickedPassengers.Add(passenger);
        }
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
        if (p1 == null || !p1.CanMatchWith(p2))
            return false;

        float dist = Vector2.Distance(p1.transform.position, p2.transform.position);
        return dist <= _maxPairingDistance;
    }

    private void PairPassengers(Passenger p1, Passenger p2)
    {
        Debug.Log($"[ManualPairing] Pairing {p1.name} and {p2.name}");

        p1.ForceToMatchingState(p2);
        p2.ForceToMatchingState(p1);
    }
}
