using System.Collections.Generic;
using LoveMetro.Spawning;
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

    private ISpawnRandom _random;
    private ISpawnPlanner _spawnPlanner;
    private ISpawnLocationProvider _locationProvider;
    private VipPairAssigner _vipPairAssigner;

    public void spawnPassangers()
    {
        SpawnPassengers();
    }

    public void SpawnPassengers()
    {
        EnsureRuntimeServices();
        Diagnostics.Log("========== SPAWN PASSENGERS BEGIN ==========");

        if (!ValidateSpawnConfiguration())
            return;

        if (!TryPrepareContainer(out int currentCount))
            return;

        List<Transform> availableLocations = _locationProvider.CollectAvailableLocations(_spawnLocations);
        if (availableLocations.Count == 0)
            return;

        PrefabPool.ShuffleList(availableLocations, _random);
        SpawnLocationMetrics locationMetrics = _locationProvider.CalculateMetrics(availableLocations);
        Diagnostics.Log($"[Spawner] typicalY={locationMetrics.TypicalY:F2}; minY={locationMetrics.MinY:F2}; maxY={locationMetrics.MaxY:F2}");

        int spawnCount = _spawnPlanner.CalculateSpawnCount(CreateSpawnRequest(availableLocations.Count, currentCount));
        Diagnostics.Log($"[Spawner] spawnCount={spawnCount} / available={availableLocations.Count}");

        if (spawnCount <= 0)
            return;

        List<Passenger> femalePool = PrefabPool.Create(_passangerFemalePrefs, _random);
        List<Passenger> malePool = PrefabPool.Create(_passangerMalePrefs, _random);
        List<bool> genderDistribution = _spawnPlanner.BuildGenderDistribution(spawnCount);
        List<Passenger> spawnedThisWave = new List<Passenger>(spawnCount);

        int femalesCreated = 0;
        int malesCreated = 0;

        for (int i = 0; i < spawnCount && availableLocations.Count > 0; i++)
        {
            bool createFemale = i < genderDistribution.Count ? genderDistribution[i] : (i % 2 == 0);
            Passenger prefab = TakeNextPrefab(createFemale, femalePool, malePool);
            if (prefab == null)
                continue;

            Transform spawnPoint = _locationProvider.TakeNextLocation(availableLocations);
            if (spawnPoint == null)
                continue;

            Passenger passenger = TrySpawnPassenger(prefab, spawnPoint, createFemale, locationMetrics);
            if (passenger == null)
                continue;

            spawnedThisWave.Add(passenger);
            if (createFemale)
                femalesCreated++;
            else
                malesCreated++;
        }

        _vipPairAssigner.TryAssignVipPair(spawnedThisWave, _vipAbility, _vipPairChance);
        SpawnResult result = new SpawnResult(
            spawnedThisWave.Count,
            femalesCreated,
            malesCreated,
            _passiveContainer.Passangers?.Count ?? 0);

        Diagnostics.Log($"========== SPAWN DONE females={result.FemalesCreated} males={result.MalesCreated} ==========");
        Diagnostics.Log($"[Spawner] container total={result.ContainerCount}");
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

    private void EnsureRuntimeServices()
    {
        _random ??= new UnitySpawnRandom();
        _spawnPlanner ??= new SpawnPlanner(_random);
        _locationProvider ??= new SpawnLocationProvider();
        _vipPairAssigner ??= new VipPairAssigner(_random);
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

    private SpawnRequest CreateSpawnRequest(int availableLocationsCount, int currentCount)
    {
        return new SpawnRequest(
            availableLocationsCount,
            currentCount,
            _maxPassengersInScene,
            MinPassengersPerWave,
            MaxPassengersPerWave);
    }

    private Passenger TakeNextPrefab(bool createFemale, List<Passenger> femalePool, List<Passenger> malePool)
    {
        return createFemale
            ? PrefabPool.TakeNext(femalePool, _passangerFemalePrefs)
            : PrefabPool.TakeNext(malePool, _passangerMalePrefs);
    }

    private Passenger TrySpawnPassenger(
        Passenger prefab,
        Transform spawnPoint,
        bool createFemale,
        SpawnLocationMetrics locationMetrics)
    {
        try
        {
            Vector3 position = _locationProvider.BuildSpawnPosition(spawnPoint, locationMetrics);
            Vector3 direction = PickStartDirection();

            Diagnostics.Log($"[Spawner] spawn {(createFemale ? "F" : "M")} at {position} (origY={spawnPoint.position.y:F2}) dir={direction}");

            Passenger passenger = Instantiate(prefab, position, Quaternion.identity);
            passenger.Initiate(direction, _trainManager, _scoreCounter);
            _passiveContainer.AddPassenger(passenger);

            ApplyGlobalAbilities(passenger);
            return passenger;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"PassangerSpawner: failed to spawn passenger: {exception.Message}");
            return null;
        }
    }

    private Vector3 PickStartDirection()
    {
        int directionIndex = Mathf.Clamp(
            _random.Range(0, _possibleStartMovingDirections.Length),
            0,
            _possibleStartMovingDirections.Length - 1);
        return _possibleStartMovingDirections[directionIndex];
    }

    private void ApplyGlobalAbilities(Passenger passenger)
    {
        if (_globalAbilities == null || _globalAbilities.Count == 0)
            return;

        PassengerAbilities runner = VipPairAssigner.GetOrAddAbilities(passenger);
        for (int i = 0; i < _globalAbilities.Count; i++)
        {
            PassengerAbility ability = _globalAbilities[i];
            if (ability != null)
                runner.AddAbility(ability);
        }

        runner.AttachAll();
    }
}
