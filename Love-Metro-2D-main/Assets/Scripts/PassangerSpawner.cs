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
    [Header("Special pair (VIP)")]
    [Tooltip("Prefab GameObjects (with Passenger component)")]
    [SerializeField] private GameObject _specialFemalePrefab;
    [SerializeField] private GameObject _specialMalePrefab;
    private bool _specialPairSpawned = false;

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
                LogPassengerSummary(passenger, "spawned");
                
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

    // Спавн особой пары (м/ж) с альтернативными моделями и VIP-флагом
    public void SpawnSpecialPair()
    {
        if (_specialPairSpawned) return;
        if (_spawnLocations == null || _spawnLocations.Count == 0) return;
        // Берём точки спавна так же, как для обычных пассажиров
        var available = new List<Transform>();
        foreach (var loc in _spawnLocations) if (loc != null) available.Add(loc);
        if (available.Count == 0) return;
        ShuffleList(available);
        Transform spotA = available[0];
        Transform spotB = available.Count > 1 ? available[1] : available[0];

        // Подстрахуем Y: если сильно ниже «платформы», поднимем к среднему Y обычных
        // Выставляем Y строго по среднему Y точек спавна (как для обычных)
        float typicalY = 0f; int cnt = 0;
        foreach (var t in _spawnLocations)
        {
            if (t == null) continue; typicalY += t.position.y; cnt++;
        }
        if (cnt > 0) typicalY /= cnt; else typicalY = spotA.position.y;
        Vector3 posA = spotA.position; Vector3 posB = spotB.position;
        if (posA.y < typicalY - 0.5f) posA.y = typicalY;
        if (posB.y < typicalY - 0.5f) posB.y = typicalY;
        Diagnostics.Log($"[Spawner][VIP] typicalY={typicalY:F2}; posA={spotA.position.y:F2}->{posA.y:F2} posB={spotB.position.y:F2}->{posB.y:F2}");

        // Выбираем префабы, с запасным вариантом
        GameObject femaleGO = _specialFemalePrefab != null ? _specialFemalePrefab :
            (_passangerFemalePrefs.Count > 0 ? _passangerFemalePrefs[0]?.gameObject : null);
        GameObject maleGO   = _specialMalePrefab != null ? _specialMalePrefab :
            (_passangerMalePrefs.Count > 0 ? _passangerMalePrefs[0]?.gameObject : null);
        if (femaleGO == null || maleGO == null) return;

        // Направления старта — используем первое допустимое
        Vector3 dirA = (_possibleStartMovingDirections != null && _possibleStartMovingDirections.Length > 0) ? _possibleStartMovingDirections[0] : Vector3.right;
        Vector3 dirB = (_possibleStartMovingDirections != null && _possibleStartMovingDirections.Length > 0) ? _possibleStartMovingDirections[0] : Vector3.left;

        // Создаём
        var pfObj = Instantiate(femaleGO, posA, Quaternion.identity);
        Passenger pf = pfObj.GetComponent<Passenger>();
        if (pf == null) return;
        pf.IsVIP = true;
        pf.IsFemale = true; // гарантируем пол
        // На всякий случай поправим Z-слой
        pf.transform.position = new Vector3(pf.transform.position.x, pf.transform.position.y, 0f);
        pf.Initiate(dirA, _trainManager, _scoreCounter);
        pf.container = _passiveContainer;
        _passiveContainer.Passangers.Add(pf);
        LogPassengerSummary(pf, "VIP-F");

        var pmObj = Instantiate(maleGO, posB, Quaternion.identity);
        Passenger pm = pmObj.GetComponent<Passenger>();
        if (pm == null) return;
        pm.IsVIP = true;
        pm.IsFemale = false; // гарантируем пол
        pm.transform.position = new Vector3(pm.transform.position.x, pm.transform.position.y, 0f);
        pm.Initiate(dirB, _trainManager, _scoreCounter);
        pm.container = _passiveContainer;
        _passiveContainer.Passangers.Add(pm);

        // Страховка: если по какой-то причине оба одного пола — пересоздадим одного правильным префабом
        if (pf.IsFemale == pm.IsFemale)
        {
            // Уничтожаем мужскую/женскую и создаём противоположную
            if (pf.IsFemale)
            {
                // оба female -> заменим pm на male
                if (pmObj != null) Destroy(pmObj);
                GameObject maleFallback = _specialMalePrefab != null ? _specialMalePrefab :
                    (_passangerMalePrefs != null && _passangerMalePrefs.Count > 0 ? _passangerMalePrefs[0]?.gameObject : null);
                if (maleFallback != null)
                {
                    pmObj = Instantiate(maleFallback, posB, Quaternion.identity);
                    pm = pmObj.GetComponent<Passenger>();
                    pm.IsVIP = true; pm.IsFemale = false;
                    pm.transform.position = new Vector3(pm.transform.position.x, pm.transform.position.y, 0f);
                    pm.Initiate(dirB, _trainManager, _scoreCounter);
                    pm.container = _passiveContainer; _passiveContainer.Passangers.Add(pm);
                }
            }
            else
            {
                // оба male -> заменим pf на female
                if (pfObj != null) Destroy(pfObj);
                GameObject femaleFallback = _specialFemalePrefab != null ? _specialFemalePrefab :
                    (_passangerFemalePrefs != null && _passangerFemalePrefs.Count > 0 ? _passangerFemalePrefs[0]?.gameObject : null);
                if (femaleFallback != null)
                {
                    pfObj = Instantiate(femaleFallback, posA, Quaternion.identity);
                    pf = pfObj.GetComponent<Passenger>();
                    pf.IsVIP = true; pf.IsFemale = true;
                    pf.transform.position = new Vector3(pf.transform.position.x, pf.transform.position.y, 0f);
                    pf.Initiate(dirA, _trainManager, _scoreCounter);
                    pf.container = _passiveContainer; _passiveContainer.Passangers.Add(pf);
                }
            }
        }

        Diagnostics.Log($"[Spawner][VIP] Female:{pfObj.name} IsFemale={pf.IsFemale} y={pf.transform.position.y:F2}  Male:{pmObj.name} IsFemale={pm.IsFemale} y={pm.transform.position.y:F2} targetY={typicalY:F2}");

        _specialPairSpawned = true;
        Diagnostics.Log("[Spawner] Special VIP pair spawned");
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
        Diagnostics.Log($"[Spawner][{tag}] name={p.name} VIP={p.IsVIP} female={p.IsFemale} layer={p.gameObject.layer} sprite='{spriteName}' ctrl='{ctrlName}' pos={p.transform.position}");
    }


}
