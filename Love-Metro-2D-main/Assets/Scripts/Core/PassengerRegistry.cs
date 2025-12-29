using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Централизованный реестр всех пассажиров в сцене.
/// Заменяет дорогие вызовы FindObjectsOfType<Passenger>() на O(1) операции.
/// </summary>
public class PassengerRegistry : MonoBehaviour
{
    public static PassengerRegistry Instance { get; private set; }

    private readonly List<Passenger> _allPassengers = new List<Passenger>();
    private readonly List<Passenger> _males = new List<Passenger>();
    private readonly List<Passenger> _females = new List<Passenger>();
    private readonly List<Passenger> _singles = new List<Passenger>();

    // Публичные readonly свойства для доступа к спискам
    public IReadOnlyList<Passenger> AllPassengers => _allPassengers;
    public IReadOnlyList<Passenger> Males => _males;
    public IReadOnlyList<Passenger> Females => _females;
    public IReadOnlyList<Passenger> Singles => _singles;

    public int MaleSinglesCount { get; private set; }
    public int FemaleSinglesCount { get; private set; }

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
    /// Регистрирует пассажира в реестре. Вызывается из Passenger.Start()
    /// </summary>
    public void Register(Passenger passenger)
    {
        if (passenger == null || _allPassengers.Contains(passenger))
            return;

        _allPassengers.Add(passenger);

        if (passenger.IsFemale)
            _females.Add(passenger);
        else
            _males.Add(passenger);

        if (!passenger.IsInCouple)
            _singles.Add(passenger);

        UpdateSinglesCounts();
    }

    /// <summary>
    /// Удаляет пассажира из реестра. Вызывается из Passenger.OnDestroy()
    /// </summary>
    public void Unregister(Passenger passenger)
    {
        if (passenger == null)
            return;

        _allPassengers.Remove(passenger);
        _males.Remove(passenger);
        _females.Remove(passenger);
        _singles.Remove(passenger);

        UpdateSinglesCounts();
    }

    /// <summary>
    /// Обновляет статус пассажира (в паре/одинокий)
    /// Вызывается когда IsInCouple меняется
    /// </summary>
    public void UpdateCoupleStatus(Passenger passenger)
    {
        if (passenger == null)
            return;

        if (passenger.IsInCouple)
        {
            _singles.Remove(passenger);
        }
        else if (!_singles.Contains(passenger))
        {
            _singles.Add(passenger);
        }

        UpdateSinglesCounts();
    }

    private void UpdateSinglesCounts()
    {
        MaleSinglesCount = 0;
        FemaleSinglesCount = 0;

        for (int i = 0; i < _singles.Count; i++)
        {
            var p = _singles[i];
            if (p == null) continue;
            if (p.IsFemale) FemaleSinglesCount++;
            else MaleSinglesCount++;
        }
    }

    /// <summary>
    /// Находит ближайшего пассажира противоположного пола в указанном радиусе.
    /// Оптимизированная версия без FindObjectsOfType.
    /// </summary>
    public Passenger FindClosestOpposite(Passenger self, float radius)
    {
        if (self == null) return null;

        var targetList = self.IsFemale ? _males : _females;
        Passenger best = null;
        float bestDistSq = radius * radius;
        Vector3 selfPos = self.transform.position;

        for (int i = 0; i < targetList.Count; i++)
        {
            var p = targetList[i];
            if (p == null || p == self || p.IsInCouple) continue;

            float distSq = (p.transform.position - selfPos).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = p;
            }
        }

        return best;
    }

    /// <summary>
    /// Находит всех пассажиров того же пола в указанном радиусе (для отталкивания).
    /// </summary>
    public void GetSameGenderInRadius(Passenger self, float radius, List<Passenger> results)
    {
        results.Clear();
        if (self == null) return;

        var targetList = self.IsFemale ? _females : _males;
        float radiusSq = radius * radius;
        Vector3 selfPos = self.transform.position;

        for (int i = 0; i < targetList.Count; i++)
        {
            var p = targetList[i];
            if (p == null || p == self) continue;

            float distSq = (p.transform.position - selfPos).sqrMagnitude;
            if (distSq < radiusSq)
            {
                results.Add(p);
            }
        }
    }

    /// <summary>
    /// Проверяет, возможны ли ещё пары (для CouplesManager)
    /// </summary>
    public int GetPossiblePairsCount()
    {
        return Mathf.Min(MaleSinglesCount, FemaleSinglesCount);
    }

    /// <summary>
    /// Очищает все null-ссылки из списков (вызывать периодически при необходимости)
    /// </summary>
    public void CleanupNullReferences()
    {
        _allPassengers.RemoveAll(p => p == null);
        _males.RemoveAll(p => p == null);
        _females.RemoveAll(p => p == null);
        _singles.RemoveAll(p => p == null);
        UpdateSinglesCounts();
    }
}
