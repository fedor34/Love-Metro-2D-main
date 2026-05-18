using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Централизованный реестр всех пассажиров в сцене.
/// Заменяет дорогие вызовы FindObjectsOfType<Passenger>() на O(1) операции.
/// </summary>
public class PassengerRegistry : MonoBehaviour, LoveMetro.Core.IPassengerRegistry
{
    public static PassengerRegistry Instance { get; private set; }

    private readonly List<Passenger> _allPassengers = new List<Passenger>();
    private readonly List<Passenger> _males = new List<Passenger>();
    private readonly List<Passenger> _females = new List<Passenger>();
    private readonly List<Passenger> _singles = new List<Passenger>();
    private readonly Dictionary<Vector2Int, List<Passenger>> _spatialBuckets = new Dictionary<Vector2Int, List<Passenger>>();
    private readonly Dictionary<Passenger, int> _registrationOrder = new Dictionary<Passenger, int>();

    // Публичные readonly свойства для доступа к спискам
    public IReadOnlyList<Passenger> AllPassengers => _allPassengers;
    public IReadOnlyList<Passenger> Males => _males;
    public IReadOnlyList<Passenger> Females => _females;
    public IReadOnlyList<Passenger> Singles => _singles;

    public int MaleSinglesCount { get; private set; }
    public int FemaleSinglesCount { get; private set; }

    // Периодическая очистка null-ссылок
    [SerializeField] private float _cleanupInterval = 2f;
    [SerializeField] private float _spatialCellSize = 2f;
    private float _nextCleanupTime = 0f;
    private int _lastSpatialRebuildFrame = -1;
    private int _nextRegistrationOrder;
    private bool _spatialIndexDirty = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoveMetro.Core.RuntimeServices.Instance.RegisterPassengerRegistry(this);
    }

    private void OnDestroy()
    {
        LoveMetro.Core.RuntimeServices.Instance.UnregisterPassengerRegistry(this);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        // Периодическая очистка null-ссылок
        if (Time.time >= _nextCleanupTime)
        {
            _nextCleanupTime = Time.time + _cleanupInterval;
            CleanupNullReferences();
        }
    }

    /// <summary>
    /// Регистрирует пассажира в реестре. Вызывается из Passenger.Start()
    /// </summary>
    public void Register(Passenger passenger)
    {
        if (passenger == null || _allPassengers.Contains(passenger))
            return;

        _allPassengers.Add(passenger);
        _registrationOrder[passenger] = _nextRegistrationOrder++;
        GetGenderList(passenger).Add(passenger);
        SetSingleMembership(passenger, !passenger.IsInCouple);
        MarkSpatialIndexDirty();
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
        _registrationOrder.Remove(passenger);
        SetSingleMembership(passenger, false);
        MarkSpatialIndexDirty();
    }

    /// <summary>
    /// Обновляет статус пассажира (в паре/одинокий)
    /// Вызывается когда IsInCouple меняется
    /// </summary>
    public void UpdateCoupleStatus(Passenger passenger)
    {
        if (passenger == null)
            return;

        SetSingleMembership(passenger, !passenger.IsInCouple);
    }

    private List<Passenger> GetGenderList(Passenger passenger)
    {
        return passenger.IsFemale ? _females : _males;
    }

    private void SetSingleMembership(Passenger passenger, bool shouldBeSingle)
    {
        if (passenger == null)
            return;

        if (shouldBeSingle)
        {
            if (_singles.Contains(passenger))
                return;

            _singles.Add(passenger);
            AdjustSinglesCount(passenger, 1);
            return;
        }

        if (_singles.Remove(passenger))
            AdjustSinglesCount(passenger, -1);
    }

    private void AdjustSinglesCount(Passenger passenger, int delta)
    {
        if (passenger.IsFemale)
            FemaleSinglesCount = Mathf.Max(0, FemaleSinglesCount + delta);
        else
            MaleSinglesCount = Mathf.Max(0, MaleSinglesCount + delta);
    }

    private void RecalculateSinglesCounts()
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

        EnsureSpatialIndexCurrent();

        Passenger best = null;
        float bestDistSq = radius * radius;
        float searchRadius = Mathf.Abs(radius);
        Vector3 selfPos = self.transform.position;

        Vector2Int minCell = PositionToCell(selfPos - new Vector3(searchRadius, searchRadius, 0f));
        Vector2Int maxCell = PositionToCell(selfPos + new Vector3(searchRadius, searchRadius, 0f));
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                if (!_spatialBuckets.TryGetValue(new Vector2Int(x, y), out List<Passenger> bucket))
                    continue;

                for (int i = 0; i < bucket.Count; i++)
                {
                    Passenger p = bucket[i];
                    if (p == null || p == self || p.IsInCouple || p.IsFemale == self.IsFemale)
                        continue;

                    float distSq = (p.transform.position - selfPos).sqrMagnitude;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = p;
                    }
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Находит всех пассажиров того же пола в указанном радиусе (для отталкивания).
    /// </summary>
    public void GetSameGenderInRadius(Passenger self, float radius, List<Passenger> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (self == null) return;

        EnsureSpatialIndexCurrent();

        float radiusSq = radius * radius;
        float searchRadius = Mathf.Abs(radius);
        Vector3 selfPos = self.transform.position;

        Vector2Int minCell = PositionToCell(selfPos - new Vector3(searchRadius, searchRadius, 0f));
        Vector2Int maxCell = PositionToCell(selfPos + new Vector3(searchRadius, searchRadius, 0f));
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                if (!_spatialBuckets.TryGetValue(new Vector2Int(x, y), out List<Passenger> bucket))
                    continue;

                for (int i = 0; i < bucket.Count; i++)
                {
                    Passenger p = bucket[i];
                    if (p == null || p == self || p.IsFemale != self.IsFemale)
                        continue;

                    float distSq = (p.transform.position - selfPos).sqrMagnitude;
                    if (distSq < radiusSq)
                        results.Add(p);
                }
            }
        }

        results.Sort(CompareRegistrationOrder);
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
        // В Unity уничтоженные объекты == null через перегруженный оператор
        // Но для надёжности используем также !p (неявное преобразование в bool)
        _allPassengers.RemoveAll(IsMissingPassenger);
        _males.RemoveAll(IsMissingPassenger);
        _females.RemoveAll(IsMissingPassenger);
        _singles.RemoveAll(IsMissingPassenger);
        RemoveMissingRegistrationOrderEntries();
        RecalculateSinglesCounts();
        MarkSpatialIndexDirty();
    }

    /// <summary>
    /// Полностью очищает реестр (вызывать при смене сцены)
    /// </summary>
    public void ClearAll()
    {
        _allPassengers.Clear();
        _males.Clear();
        _females.Clear();
        _singles.Clear();
        _spatialBuckets.Clear();
        _registrationOrder.Clear();
        MaleSinglesCount = 0;
        FemaleSinglesCount = 0;
        _nextRegistrationOrder = 0;
        MarkSpatialIndexDirty();
    }

    private static bool IsMissingPassenger(Passenger passenger)
    {
        return passenger == null || !passenger;
    }

    private void EnsureSpatialIndexCurrent()
    {
        if (!Application.isPlaying)
        {
            RebuildSpatialIndex();
            return;
        }

        if (!_spatialIndexDirty && _lastSpatialRebuildFrame == Time.frameCount)
            return;

        RebuildSpatialIndex();
    }

    private void RebuildSpatialIndex()
    {
        _spatialBuckets.Clear();
        float safeCellSize = GetSafeSpatialCellSize();

        for (int i = 0; i < _allPassengers.Count; i++)
        {
            Passenger passenger = _allPassengers[i];
            if (IsMissingPassenger(passenger))
                continue;

            Vector2Int cell = PositionToCell(passenger.transform.position, safeCellSize);
            if (!_spatialBuckets.TryGetValue(cell, out List<Passenger> bucket))
            {
                bucket = new List<Passenger>();
                _spatialBuckets[cell] = bucket;
            }

            bucket.Add(passenger);
        }

        _spatialIndexDirty = false;
        _lastSpatialRebuildFrame = Time.frameCount;
    }

    private Vector2Int PositionToCell(Vector3 position)
    {
        return PositionToCell(position, GetSafeSpatialCellSize());
    }

    private static Vector2Int PositionToCell(Vector3 position, float cellSize)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize));
    }

    private float GetSafeSpatialCellSize()
    {
        return Mathf.Max(0.1f, _spatialCellSize);
    }

    private void MarkSpatialIndexDirty()
    {
        _spatialIndexDirty = true;
    }

    private int CompareRegistrationOrder(Passenger first, Passenger second)
    {
        int firstOrder = first != null && _registrationOrder.TryGetValue(first, out int orderA)
            ? orderA
            : int.MaxValue;
        int secondOrder = second != null && _registrationOrder.TryGetValue(second, out int orderB)
            ? orderB
            : int.MaxValue;
        return firstOrder.CompareTo(secondOrder);
    }

    private void RemoveMissingRegistrationOrderEntries()
    {
        List<Passenger> missingPassengers = null;
        foreach (Passenger passenger in _registrationOrder.Keys)
        {
            if (!IsMissingPassenger(passenger))
                continue;

            missingPassengers ??= new List<Passenger>();
            missingPassengers.Add(passenger);
        }

        if (missingPassengers == null)
            return;

        for (int i = 0; i < missingPassengers.Count; i++)
            _registrationOrder.Remove(missingPassengers[i]);
    }
}
