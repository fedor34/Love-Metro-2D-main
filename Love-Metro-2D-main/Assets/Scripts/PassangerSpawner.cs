using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassangerSpawner : MonoBehaviour
{
    [SerializeField] private List<Transform> _spawnLocations;
    [SerializeField] private TrainManager _trainManager;
    [SerializeField] private PassangersContainer _passiveContainer;
    [SerializeField] private List<WandererNew> _passangerFemalePrefs;
    [SerializeField] private List<WandererNew> _passangerMalePrefs;
    [SerializeField] private SortingLayerEditor _sortingLayerEditor;
    [SerializeField] private ScoreCounter _scoreCounter;

    [SerializeField] private int passangerCount;
    [SerializeField] private Vector3[] _possibleStartMovingDirections;
    public void spawnPassangers()
    {
        Debug.Log("====== НАЧАЛО СПАВНА ПАССАЖИРОВ ======");
        
        // Проверка и инициализация списков
        if (_spawnLocations == null || _spawnLocations.Count == 0)
        {
            Debug.LogError("Нет точек спавна пассажиров!");
            return;
        }
        
        if (_passangerFemalePrefs == null || _passangerFemalePrefs.Count == 0)
        {
            Debug.LogError("Список префабов женщин пуст!");
            return;
        }
        
        if (_passangerMalePrefs == null || _passangerMalePrefs.Count == 0)
        {
            Debug.LogError("Список префабов мужчин пуст!");
            return;
        }
        
        if (_possibleStartMovingDirections == null || _possibleStartMovingDirections.Length == 0)
        {
            Debug.LogError("Список возможных направлений движения пуст!");
            return;
        }
        
        // Создаем локальную копию списка точек спавна
        List<Transform> availableLocations = new List<Transform>();
        foreach (var loc in _spawnLocations)
        {
            if (loc != null) availableLocations.Add(loc);
        }
        
        if (availableLocations.Count == 0)
        {
            Debug.LogError("Список доступных точек спавна пуст после фильтрации!");
            return;
        }
        
        // Перемешиваем точки спавна для более равномерного распределения
        ShuffleList(availableLocations);
        
        Debug.Log($"Доступно точек спавна: {availableLocations.Count}");
        Debug.Log($"Префабов женщин: {_passangerFemalePrefs.Count}");
        Debug.Log($"Префабов мужчин: {_passangerMalePrefs.Count}");
        
        // Определяем количество пассажиров для спавна
        int maxPossibleSpawn = Mathf.Min(8, availableLocations.Count);
        int spawnCount = UnityEngine.Random.Range(4, maxPossibleSpawn + 1);
        Debug.Log($"Будет создано пассажиров: {spawnCount}");
        
        // Создаем списки перемешанных префабов для женщин и мужчин
        List<WandererNew> femalePrefabsShuffled = new List<WandererNew>(_passangerFemalePrefs);
        List<WandererNew> malePrefabsShuffled = new List<WandererNew>(_passangerMalePrefs);
        ShuffleList(femalePrefabsShuffled);
        ShuffleList(malePrefabsShuffled);
        
        // Переменные для отслеживания созданных пассажиров
        int femalesCreated = 0;
        int malesCreated = 0;
        
        // Создаем список индексов полов для случайного распределения
        List<bool> genderDistribution = new List<bool>();
        int femaleCount = spawnCount / 2;
        int maleCount = spawnCount - femaleCount;
        
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
                Debug.LogWarning("Закончились доступные точки спавна, прерываем создание пассажиров.");
                break;
            }
            
            bool createFemale = i < genderDistribution.Count ? genderDistribution[i] : (i % 2 == 0);
            
            WandererNew prefab = null;
            if (createFemale)
            {
                if (femalePrefabsShuffled.Count > 0)
                {
                    prefab = femalePrefabsShuffled[0];
                    femalePrefabsShuffled.RemoveAt(0);
                    if (femalePrefabsShuffled.Count == 0)
                        femalePrefabsShuffled = new List<WandererNew>(_passangerFemalePrefs);
                }
            }
            else
            {
                if (malePrefabsShuffled.Count > 0)
                {
                    prefab = malePrefabsShuffled[0];
                    malePrefabsShuffled.RemoveAt(0);
                    if (malePrefabsShuffled.Count == 0)
                        malePrefabsShuffled = new List<WandererNew>(_passangerMalePrefs);
                }
            }
            
            if (prefab == null)
            {
                Debug.LogWarning("Не удалось выбрать префаб пассажира, пропускаем.");
                continue;
            }
            
            // Выбираем точку спавна (первую доступную из перемешанного списка)
            Transform spawnPoint = availableLocations[0];
            availableLocations.RemoveAt(0);
            
            if (spawnPoint == null)
            {
                Debug.LogWarning("Точка спавна оказалась null, пропускаем.");
                continue;
            }
            
            // Выбираем направление движения
            int directionIndex = Mathf.Clamp(UnityEngine.Random.Range(0, _possibleStartMovingDirections.Length), 0, _possibleStartMovingDirections.Length - 1);
            Vector3 direction = _possibleStartMovingDirections[directionIndex];
            
            // Создаем пассажира
            try
            {
                WandererNew passenger = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
                passenger.Initiate(direction, _trainManager, _scoreCounter);
                passenger.container = _passiveContainer;
                _passiveContainer.Passangers.Add(passenger);
                
                if (createFemale) femalesCreated++;
                else malesCreated++;
                
                Debug.Log($"Создан пассажир: {(createFemale ? "женщина" : "мужчина")} на позиции {spawnPoint.position}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка при создании пассажира: {e.Message}");
            }
        }
        
        if (_sortingLayerEditor != null)
        {
            _sortingLayerEditor.getPassangerSprites();
        }
        else
        {
            Debug.LogWarning("_sortingLayerEditor не назначен!");
        }
        
        Debug.Log($"Завершено создание пассажиров. Создано женщин: {femalesCreated}, мужчин: {malesCreated}");
        Debug.Log("====== КОНЕЦ СПАВНА ПАССАЖИРОВ ======");
    }

    private void Start()
    {
        spawnPassangers();
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
}
