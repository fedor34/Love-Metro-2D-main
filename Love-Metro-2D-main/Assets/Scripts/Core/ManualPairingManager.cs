using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages manual pairing of passengers via click interaction.
/// Allows players to click two nearby compatible passengers to force them into a couple.
/// supports "One Click" pairing if passengers are overlapping.
/// </summary>
public class ManualPairingManager : MonoBehaviour
{
    public static ManualPairingManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _maxPairingDistance = 3.0f;
    [SerializeField] private float _clickRadius = 0.4f; // Radius to find overlapping passengers
    [SerializeField] private float _verticalSearchFactor = 2.0f; // Множитель для "вытягивания" области поиска по вертикали (Z/Y)

    // Кешируем ScoreCounter для избежания FindObjectOfType каждый раз
    private ScoreCounter _cachedScoreCounter;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _cachedScoreCounter = FindObjectOfType<ScoreCounter>();
    }

    /// <summary>
    /// Handles a click at the given screen position.
    /// Returns true if the click interacted with a passenger (select/pair), false otherwise.
    /// </summary>
    public bool HandleClick(Vector2 screenPosition)
    {
        if (Camera.main == null) return false;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        
        // Используем OverlapBoxAll для создания вытянутой по вертикали области
        Vector2 boxSize = new Vector2(_clickRadius * 2f, _clickRadius * 2f * _verticalSearchFactor);
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, boxSize, 0f);
        
        List<Passenger> clickedPassengers = new List<Passenger>();
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Passenger>(out var p))
            {
                if (!clickedPassengers.Contains(p))
                {
                    clickedPassengers.Add(p);
                }
            }
        }

        if (clickedPassengers.Count == 0)
        {
            return false; // Clicked empty space
        }

        // 1. Try "One Click Pair" - check if the clicked group contains a valid pair
        // useful when characters are overlapping
        if (AttemptOverlapPairing(clickedPassengers))
        {
            return true;
        }

        // If no overlap pairing was possible, we still return true to consume the click
        // and prevent train acceleration, because the player clearly intended to interact with passengers.
        return true;
    }

    private bool AttemptOverlapPairing(List<Passenger> group)
    {
        if (group.Count < 2) return false;

        // Sort or just iterate to find first valid pair
        for (int i = 0; i < group.Count; i++)
        {
            for (int j = i + 1; j < group.Count; j++)
            {
                Passenger p1 = group[i];
                Passenger p2 = group[j];

                if (p1.IsMatchable && !p1.IsInCouple && 
                    p2.IsMatchable && !p2.IsInCouple &&
                    CanPair(p1, p2))
                {
                    PairPassengers(p1, p2);
                    return true;
                }
            }
        }
        return false;
    }

    private bool CanPair(Passenger p1, Passenger p2)
    {
        if (p1 == p2) return false;
        // Must be different gender
        if (p1.IsFemale == p2.IsFemale) return false;
        
        // Must be nearby
        float dist = Vector2.Distance(p1.transform.position, p2.transform.position);
        if (dist > _maxPairingDistance) 
        {
            return false;
        }
        
        return true;
    }

    private void PairPassengers(Passenger p1, Passenger p2)
    {
        Debug.Log($"[ManualPairing] Pairing {p1.name} and {p2.name}");

        // Используем кешированный ScoreCounter
        if (_cachedScoreCounter == null)
            _cachedScoreCounter = FindObjectOfType<ScoreCounter>();

        if (_cachedScoreCounter != null)
        {
            // Calculate screen position for score popup
            Vector3 midPoint = (p1.transform.position + p2.transform.position) * 0.5f;
            Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(midPoint) : Vector3.zero;
            
            // Use base points from score counter or abilities
            int points = _cachedScoreCounter.GetBasePointsPerCouple();

            // Check abilities (similar to Passenger.ForceToMatchingState)
            var a1 = p1.GetComponent<PassengerAbilities>();
            var a2 = p2.GetComponent<PassengerAbilities>();
            a1?.InvokeMatched(p2, ref points);
            a2?.InvokeMatched(p1, ref points);

            _cachedScoreCounter.AwardMatchPoints(screenPos, Mathf.Max(0, points));
        }

        p1.ForceToMatchingState(p2);
        p2.ForceToMatchingState(p1);
    }
}
