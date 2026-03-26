using System.Collections.Generic;
using UnityEngine;

public class PassangerSpawner : MonoBehaviour
{
    private const int MinPassengersPerWave = 5;
    private const int MaxPassengersPerWave = 7;

    [SerializeField] private List<Transform> _spawnLocations;
    [SerializeField] private TrainManager _trainManager;
    [SerializeField] private PassangersContainer _passiveContainer;
    [SerializeField] private List<Passenger> _passangerFemalePrefs;
    [SerializeField] private List<Passenger> _passangerMalePrefs;
    [SerializeField] private SortingLayerEditor _sortingLayerEditor;
    [SerializeField] private ScoreCounter _scoreCounter;
    [Header("Abilities applied to each spawned passenger (optional)")]
    [SerializeField] private List<PassengerAbility> _globalAbilities;

    [SerializeField] private int passangerCount;
    [SerializeField] private Vector3[] _possibleStartMovingDirections;

    [Header("Passenger Limits")]
    [SerializeField] private int _maxPassengersInScene = 20;

    [Header("Auto VIP pair per wave")]
    [SerializeField] private VipAbility _vipAbility;
    [Range(0f, 1f)] [SerializeField] private float _vipPairChance = 1f;

    public void spawnPassangers()
    {
        SpawnPassengers();
    }

    public void SpawnPassengers()
    {
        Diagnostics.Log("========== SPAWN PASSENGERS BEGIN ==========");

        if (!ValidateSpawnConfiguration())
            return;

        if (!TryPrepareContainer(out int currentCount))
            return;

        List<Transform> availableLocations = CollectAvailableLocations();
        if (availableLocations.Count == 0)
            return;

        ShuffleList(availableLocations);

        bool hasTypicalY;
        float typicalY = CalculateTypicalY(availableLocations, out hasTypicalY);
        Diagnostics.Log($"[Spawner] typicalY={typicalY:F2}; minY={MinY(availableLocations):F2}; maxY={MaxY(availableLocations):F2}");

        int spawnCount = CalculateSpawnCount(availableLocations.Count, currentCount);
        Diagnostics.Log($"[Spawner] spawnCount={spawnCount} / available={availableLocations.Count}");

        List<Passenger> femalePool = CreatePrefabPool(_passangerFemalePrefs);
        List<Passenger> malePool = CreatePrefabPool(_passangerMalePrefs);
        List<bool> genderDistribution = BuildGenderDistribution(spawnCount);
        List<Passenger> spawnedThisWave = new List<Passenger>(spawnCount);

        int femalesCreated = 0;
        int malesCreated = 0;

        for (int i = 0; i < spawnCount && availableLocations.Count > 0; i++)
        {
            bool createFemale = i < genderDistribution.Count ? genderDistribution[i] : (i % 2 == 0);
            Passenger prefab = TakeNextPrefab(createFemale, femalePool, malePool);
            if (prefab == null)
                continue;

            Transform spawnPoint = TakeNextLocation(availableLocations);
            if (spawnPoint == null)
                continue;

            Passenger passenger = TrySpawnPassenger(prefab, spawnPoint, createFemale, typicalY, hasTypicalY);
            if (passenger == null)
                continue;

            spawnedThisWave.Add(passenger);
            if (createFemale)
                femalesCreated++;
            else
                malesCreated++;
        }

        TryAssignVipPair(spawnedThisWave);
        Diagnostics.Log($"========== SPAWN DONE females={femalesCreated} males={malesCreated} ==========");
        Diagnostics.Log($"[Spawner] container total={_passiveContainer.Passangers?.Count ?? 0}");
    }

    private void Start()
    {
        if (_passiveContainer != null)
        {
            if (_passiveContainer.Passangers == null)
                _passiveContainer.Passangers = new List<Passenger>();
            else
                _passiveContainer.Passangers.Clear();
        }

        SpawnPassengers();
    }

    private bool ValidateSpawnConfiguration()
    {
        if (_spawnLocations == null || _spawnLocations.Count == 0)
        {
            Debug.LogError("PassangerSpawner: no spawn points assigned.");
            return false;
        }
        Diagnostics.Log($"[Spawner] spawn points: {_spawnLocations.Count}");

        if (_passangerFemalePrefs == null || _passangerFemalePrefs.Count == 0)
        {
            Debug.LogError("PassangerSpawner: no female prefabs assigned.");
            return false;
        }
        Diagnostics.Log($"[Spawner] female prefabs: {_passangerFemalePrefs.Count}");

        if (_passangerMalePrefs == null || _passangerMalePrefs.Count == 0)
        {
            Debug.LogError("PassangerSpawner: no male prefabs assigned.");
            return false;
        }
        Diagnostics.Log($"[Spawner] male prefabs: {_passangerMalePrefs.Count}");

        if (_possibleStartMovingDirections == null || _possibleStartMovingDirections.Length == 0)
        {
            Debug.LogError("PassangerSpawner: no movement directions assigned.");
            return false;
        }
        Diagnostics.Log($"[Spawner] start directions: {_possibleStartMovingDirections.Length}");

        if (_passiveContainer == null)
        {
            Debug.LogError("PassangerSpawner: passenger container is not assigned.");
            return false;
        }

        if (_trainManager == null)
        {
            Debug.LogError("PassangerSpawner: TrainManager is not assigned.");
            return false;
        }
        Diagnostics.Log("[Spawner] TrainManager set");

        return true;
    }

    private bool TryPrepareContainer(out int currentCount)
    {
        _passiveContainer.CleanupNullReferences();
        if (_passiveContainer.Passangers == null)
            _passiveContainer.Passangers = new List<Passenger>();

        currentCount = _passiveContainer.Passangers.Count;
        Diagnostics.Log($"[Spawner] container assigned. count={currentCount}");

        if (currentCount >= _maxPassengersInScene)
        {
            Diagnostics.Log($"[Spawner] SKIP: already {currentCount} passengers (max={_maxPassengersInScene})");
            return false;
        }

        return true;
    }

    private List<Transform> CollectAvailableLocations()
    {
        List<Transform> availableLocations = new List<Transform>(_spawnLocations.Count);
        for (int i = 0; i < _spawnLocations.Count; i++)
        {
            Transform location = _spawnLocations[i];
            if (location != null)
                availableLocations.Add(location);
        }

        return availableLocations;
    }

    private int CalculateSpawnCount(int availableLocationsCount, int currentCount)
    {
        int remainingSlots = _maxPassengersInScene - currentCount;
        int maxPossibleSpawn = Mathf.Min(MaxPassengersPerWave, availableLocationsCount, remainingSlots);
        int minDesired = Mathf.Min(MinPassengersPerWave, maxPossibleSpawn);
        return Random.Range(minDesired, maxPossibleSpawn + 1);
    }

    private static float CalculateTypicalY(List<Transform> availableLocations, out bool hasTypicalY)
    {
        float typicalY = 0f;
        int count = 0;

        for (int i = 0; i < availableLocations.Count; i++)
        {
            Transform location = availableLocations[i];
            if (location == null)
                continue;

            typicalY += location.position.y;
            count++;
        }

        hasTypicalY = count > 0;
        return hasTypicalY ? typicalY / count : 0f;
    }

    private List<Passenger> CreatePrefabPool(List<Passenger> prefabs)
    {
        List<Passenger> pool = new List<Passenger>(prefabs);
        ShuffleList(pool);
        return pool;
    }

    private List<bool> BuildGenderDistribution(int spawnCount)
    {
        List<bool> genderDistribution = new List<bool>(spawnCount);
        int femaleCount = spawnCount / 2;
        int maleCount = spawnCount / 2;

        if (spawnCount % 2 == 1)
        {
            if (Random.value < 0.5f)
                femaleCount++;
            else
                maleCount++;
        }

        for (int i = 0; i < femaleCount; i++)
            genderDistribution.Add(true);
        for (int i = 0; i < maleCount; i++)
            genderDistribution.Add(false);

        ShuffleList(genderDistribution);
        return genderDistribution;
    }

    private Passenger TakeNextPrefab(bool createFemale, List<Passenger> femalePool, List<Passenger> malePool)
    {
        return createFemale
            ? TakeNextPrefabFromPool(femalePool, _passangerFemalePrefs)
            : TakeNextPrefabFromPool(malePool, _passangerMalePrefs);
    }

    private static Passenger TakeNextPrefabFromPool(List<Passenger> pool, List<Passenger> source)
    {
        if (pool == null || pool.Count == 0)
            return null;

        Passenger prefab = pool[0];
        pool.RemoveAt(0);

        if (pool.Count == 0 && source != null && source.Count > 0)
            pool.AddRange(source);

        return prefab;
    }

    private static Transform TakeNextLocation(List<Transform> availableLocations)
    {
        Transform spawnPoint = availableLocations[0];
        availableLocations.RemoveAt(0);
        return spawnPoint;
    }

    private Passenger TrySpawnPassenger(Passenger prefab, Transform spawnPoint, bool createFemale, float typicalY, bool hasTypicalY)
    {
        try
        {
            Vector3 position = BuildSpawnPosition(spawnPoint, typicalY, hasTypicalY);
            Vector3 direction = PickStartDirection();

            Diagnostics.Log($"[Spawner] spawn {(createFemale ? "F" : "M")} at {position} (origY={spawnPoint.position.y:F2}) dir={direction}");

            Passenger passenger = Instantiate(prefab, position, Quaternion.identity);
            passenger.Initiate(direction, _trainManager, _scoreCounter);
            passenger.container = _passiveContainer;
            _passiveContainer.Passangers.Add(passenger);

            ApplyGlobalAbilities(passenger);
            LogPassengerSummary(passenger, "spawned");
            return passenger;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"PassangerSpawner: failed to spawn passenger: {exception.Message}");
            return null;
        }
    }

    private Vector3 BuildSpawnPosition(Transform spawnPoint, float typicalY, bool hasTypicalY)
    {
        Vector3 position = spawnPoint.position;
        if (hasTypicalY && position.y < typicalY - 0.5f)
            position.y = typicalY;

        return position;
    }

    private Vector3 PickStartDirection()
    {
        int directionIndex = Mathf.Clamp(Random.Range(0, _possibleStartMovingDirections.Length), 0, _possibleStartMovingDirections.Length - 1);
        return _possibleStartMovingDirections[directionIndex];
    }

    private void ApplyGlobalAbilities(Passenger passenger)
    {
        if (_globalAbilities == null || _globalAbilities.Count == 0)
            return;

        PassengerAbilities runner = GetOrAddAbilities(passenger);
        for (int i = 0; i < _globalAbilities.Count; i++)
        {
            PassengerAbility ability = _globalAbilities[i];
            if (ability != null)
                runner.AddAbility(ability);
        }

        runner.AttachAll();
    }

    private void TryAssignVipPair(List<Passenger> spawned)
    {
        if (_vipAbility == null)
        {
            Diagnostics.Log("[Spawner][VIP] ability is null - skip");
            return;
        }

        if (spawned == null || spawned.Count < 2)
            return;

        if (Random.value > _vipPairChance)
            return;

        Passenger female = null;
        Passenger male = null;
        for (int i = 0; i < spawned.Count; i++)
        {
            Passenger passenger = spawned[i];
            if (passenger == null || passenger.IsInCouple)
                continue;

            if (passenger.IsFemale && female == null)
                female = passenger;
            if (!passenger.IsFemale && male == null)
                male = passenger;
            if (female != null && male != null)
                break;
        }

        if (female == null || male == null)
        {
            Diagnostics.Log("[Spawner][VIP] not found both genders in wave");
            return;
        }

        ApplyVip(female);
        ApplyVip(male);
        Diagnostics.Log($"[Spawner][VIP] Assigned to pair: F={female.name} M={male.name}");
    }

    private void ApplyVip(Passenger passenger)
    {
        PassengerAbilities runner = GetOrAddAbilities(passenger);
        runner.AddAbility(_vipAbility);
        runner.AttachAll();
    }

    private static PassengerAbilities GetOrAddAbilities(Passenger passenger)
    {
        PassengerAbilities runner = passenger.GetComponent<PassengerAbilities>();
        if (runner == null)
            runner = passenger.gameObject.AddComponent<PassengerAbilities>();

        return runner;
    }

    private static void ShuffleList<T>(List<T> list)
    {
        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            int swapIndex = i + Random.Range(0, count - i);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private static float MinY(List<Transform> transforms)
    {
        float min = float.PositiveInfinity;
        for (int i = 0; i < transforms.Count; i++)
        {
            Transform transformItem = transforms[i];
            if (transformItem != null)
                min = Mathf.Min(min, transformItem.position.y);
        }

        return float.IsInfinity(min) ? 0f : min;
    }

    private static float MaxY(List<Transform> transforms)
    {
        float max = float.NegativeInfinity;
        for (int i = 0; i < transforms.Count; i++)
        {
            Transform transformItem = transforms[i];
            if (transformItem != null)
                max = Mathf.Max(max, transformItem.position.y);
        }

        return float.IsNegativeInfinity(max) ? 0f : max;
    }

    private static void LogPassengerSummary(Passenger passenger, string tag)
    {
        if (passenger == null)
            return;

        SpriteRenderer spriteRenderer = passenger.GetComponent<SpriteRenderer>();
        Animator animator = passenger.GetComponent<Animator>();
        string controllerName = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.name
            : "<null>";
        string spriteName = spriteRenderer != null && spriteRenderer.sprite != null
            ? spriteRenderer.sprite.name
            : "<null>";

        Diagnostics.Log($"[Spawner][{tag}] name={passenger.name} female={passenger.IsFemale} layer={passenger.gameObject.layer} sprite='{spriteName}' ctrl='{controllerName}' pos={passenger.transform.position}");
    }
}
