using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassangerSpawner : MonoBehaviour
{
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
    public void spawnPassangers()
    {
        Diagnostics.Log("========== SPAWN PASSENGERS BEGIN ==========");
        
        // Проверка и инициализация списков
        if (_spawnLocations == null || _spawnLocations.Count == 0)
        {
            Debug.LogError("PassangerSpawner: Нет точек спавна!");
            return;
        }
        Diagnostics.Log($"[Spawner] spawn points: {_spawnLocations.Count}");
        
        if (_passangerFemalePrefs == null || _passangerFemalePrefs.Count == 0)
        {
            Debug.LogError("PassangerSpawner: Нет женских префабов!");
            return;
        }
        Diagnostics.Log($"[Spawner] female prefabs: {_passangerFemalePrefs.Count}");
        
        if (_passangerMalePrefs == null || _passangerMalePrefs.Count == 0)
        {
            Debug.LogError("PassangerSpawner: Нет мужских префабов!");
            return;
        }
        Diagnostics.Log($"[Spawner] male prefabs: {_passangerMalePrefs.Count}");
        
        if (_possibleStartMovingDirections == null || _possibleStartMovingDirections.Length == 0)
        {
            Debug.LogError("PassangerSpawner: Нет направлений движения!");
            return;
        }
        Diagnostics.Log($"[Spawner] start directions: {_possibleStartMovingDirections.Length}");
        
        if (_passiveContainer == null)
        {
            Debug.LogError("PassangerSpawner: Контейнер пассажиров не назначен!");
            return;
        }
        Diagnostics.Log($"[Spawner] container assigned. count={_passiveContainer.Passangers?.Count ?? 0}");
        
        if (_trainManager == null)
        {
            Debug.LogError("PassangerSpawner: TrainManager не назначен!");
            return;
        }
        Diagnostics.Log("[Spawner] TrainManager set");
        
        // Создаем локальную копию списка точек спавна
        List<Transform> availableLocations = new List<Transform>();
        foreach (var loc in _spawnLocations)
        {
            if (loc != null) availableLocations.Add(loc);
        }
        
        if (availableLocations.Count == 0)
        {
            return;
        }
        
        // Перемешиваем точки спавна для более равномерного распределения
        ShuffleList(availableLocations);

        // Вычисляем типичный уровень Y (средний по точкам), чтобы не спавнить под полом
        float typicalY = 0f; int yCnt = 0;
        foreach (var t in availableLocations)
        {
            if (t == null) continue; typicalY += t.position.y; yCnt++;
        }
        if (yCnt > 0) typicalY /= yCnt;
        Diagnostics.Log($"[Spawner] typicalY={typicalY:F2}; minY={MinY(availableLocations):F2}; maxY={MaxY(availableLocations):F2}");
        
        // Определяем количество пассажиров для спавна (от 5 до 7, но не больше доступных точек)
        int maxPossibleSpawn = Mathf.Min(7, availableLocations.Count);
        int minDesired = Mathf.Min(5, maxPossibleSpawn);
        int spawnCount = UnityEngine.Random.Range(minDesired, maxPossibleSpawn + 1);
        
        Diagnostics.Log($"[Spawner] spawnCount={spawnCount} / available={availableLocations.Count}");
        
        // Создаем списки перемешанных префабов для женщин и мужчин
        List<Passenger> femalePrefabsShuffled = new List<Passenger>(_passangerFemalePrefs);
        List<Passenger> malePrefabsShuffled = new List<Passenger>(_passangerMalePrefs);
        ShuffleList(femalePrefabsShuffled);
        ShuffleList(malePrefabsShuffled);
        
        // Переменные для отслеживания созданных пассажиров
        int femalesCreated = 0;
        int malesCreated = 0;
        
        // Создаем список индексов полов для случайного распределения
        List<bool> genderDistribution = new List<bool>();
        int femaleCount = spawnCount / 2;
        int maleCount = spawnCount / 2;
        // Если нужно распределить нечетное количество пассажиров, случайно добавляем лишнего
        if (spawnCount % 2 == 1)
        {
            if (UnityEngine.Random.value < 0.5f)
                femaleCount++;
            else
                maleCount++;
        }
        
        // Заполняем список распределения полов
        for (int i = 0; i < femaleCount; i++) genderDistribution.Add(true);  // true = женщина
        for (int i = 0; i < maleCount; i++) genderDistribution.Add(false);   // false = мужчина
        
        // Перемешиваем распределение полов
        ShuffleList(genderDistribution);
        
        // Спавним пассажиров в соответствии с перемешанным распределением
        var spawnedThisWave = new List<Passenger>();
        for (int i = 0; i < spawnCount; i++)
        {
            if (availableLocations.Count == 0)
            {
                break;
            }
            
            bool createFemale = i < genderDistribution.Count ? genderDistribution[i] : (i % 2 == 0);
            
            Passenger prefab = null;
            if (createFemale)
            {
                if (femalePrefabsShuffled.Count > 0)
                {
                    prefab = femalePrefabsShuffled[0];
                    femalePrefabsShuffled.RemoveAt(0);
                    if (femalePrefabsShuffled.Count == 0)
                        femalePrefabsShuffled = new List<Passenger>(_passangerFemalePrefs);
                }
            }
            else
            {
                if (malePrefabsShuffled.Count > 0)
                {
                    prefab = malePrefabsShuffled[0];
                    malePrefabsShuffled.RemoveAt(0);
                    if (malePrefabsShuffled.Count == 0)
                        malePrefabsShuffled = new List<Passenger>(_passangerMalePrefs);
                }
            }
            
            if (prefab == null)
            {
                continue;
            }
            
            // Выбираем точку спавна (первую доступную из перемешанного списка)
            Transform spawnPoint = availableLocations[0];
            availableLocations.RemoveAt(0);
            
            if (spawnPoint == null)
            {
                continue;
            }
            
            // Выбираем направление движения
            int directionIndex = Mathf.Clamp(UnityEngine.Random.Range(0, _possibleStartMovingDirections.Length), 0, _possibleStartMovingDirections.Length - 1);
            Vector3 direction = _possibleStartMovingDirections[directionIndex];
            
            // Создаем пассажира
            try
            {
                Vector3 pos = spawnPoint.position;
                if (yCnt > 0 && pos.y < typicalY - 0.5f) pos.y = typicalY; // поднимем слишком низкие точки
                Diagnostics.Log($"[Spawner] spawn { (createFemale?"F":"M") } at {pos} (origY={spawnPoint.position.y:F2}) dir={direction}");
                Passenger passenger = Instantiate(prefab, pos, Quaternion.identity);
                passenger.Initiate(direction, _trainManager, _scoreCounter);
                passenger.container = _passiveContainer;
                _passiveContainer.Passangers.Add(passenger);
                // Attach optional global abilities
                if (_globalAbilities != null && _globalAbilities.Count > 0)
                {
                    var runner = passenger.GetComponent<PassengerAbilities>();
                    if (runner == null) runner = passenger.gameObject.AddComponent<PassengerAbilities>();
                    foreach (var ability in _globalAbilities) runner.AddAbility(ability);
                    runner.AttachAll();
                }
                LogPassengerSummary(passenger, "spawned");
                spawnedThisWave.Add(passenger);
                
                if (createFemale) femalesCreated++;
                else malesCreated++;
                
                // summary above
                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PassangerSpawner: Ошибка при создании пассажира: {e.Message}");
                continue;
            }
        }
        
        // Авто-VIP: назначаем способность VIP случайной паре М/Ж в этой волне
        TryAssignVipPair(spawnedThisWave);

        Diagnostics.Log($"========== SPAWN DONE females={femalesCreated} males={malesCreated} ==========");
        Diagnostics.Log($"[Spawner] container total={_passiveContainer.Passangers?.Count ?? 0}");
        
        // Временно отключаем SortingLayerEditor чтобы избежать ошибок
        /*if (_sortingLayerEditor != null)
        {
            _sortingLayerEditor.getPassangerSprites();
        }*/
    }

    private void Start()
    {
        spawnPassangers();
    }

    [Header("Auto VIP pair per wave")]
    [SerializeField] private VipAbility _vipAbility;
    [Range(0f,1f)] [SerializeField] private float _vipPairChance = 1f;

    private void TryAssignVipPair(List<Passenger> spawned)
    {
        if (_vipAbility == null) { Diagnostics.Log("[Spawner][VIP] ability is null — skip"); return; }
        if (spawned == null || spawned.Count < 2) return;
        if (UnityEngine.Random.value > _vipPairChance) return;

        Passenger female = null, male = null;
        for (int i = 0; i < spawned.Count; i++)
        {
            var p = spawned[i]; if (p == null || p.IsInCouple) continue;
            if (p.IsFemale && female == null) female = p;
            if (!p.IsFemale && male == null) male = p;
            if (female != null && male != null) break;
        }
        if (female == null || male == null) { Diagnostics.Log("[Spawner][VIP] not found both genders in wave"); return; }

        ApplyVip(female); ApplyVip(male);
        Diagnostics.Log($"[Spawner][VIP] Assigned to pair: F={female.name} M={male.name}");
    }

    private void ApplyVip(Passenger p)
    {
        var runner = p.GetComponent<PassengerAbilities>();
        if (runner == null) runner = p.gameObject.AddComponent<PassengerAbilities>();
        runner.AddAbility(_vipAbility);
        runner.AttachAll();
    }

    // Вспомогательный метод для перемешивания списка
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            int r = i + UnityEngine.Random.Range(0, n - i);
            T temp = list[i];
            list[i] = list[r];
            list[r] = temp;
        }
    }

    // Helpers for diagnostics
    private static float MinY(List<Transform> t)
    {
        float m = float.PositiveInfinity; foreach (var x in t) if (x != null) m = Mathf.Min(m, x.position.y); return float.IsInfinity(m) ? 0f : m;
    }
    private static float MaxY(List<Transform> t)
    {
        float m = float.NegativeInfinity; foreach (var x in t) if (x != null) m = Mathf.Max(m, x.position.y); return float.IsNegativeInfinity(m) ? 0f : m;
    }
    private static void LogPassengerSummary(Passenger p, string tag)
    {
        if (p == null) return;
        var sr = p.GetComponent<SpriteRenderer>();
        var anim = p.GetComponent<Animator>();
        string ctrlName = anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "<null>";
        string spriteName = sr != null && sr.sprite != null ? sr.sprite.name : "<null>";
        Diagnostics.Log($"[Spawner][{tag}] name={p.name} female={p.IsFemale} layer={p.gameObject.layer} sprite='{spriteName}' ctrl='{ctrlName}' pos={p.transform.position}");
    }


}
