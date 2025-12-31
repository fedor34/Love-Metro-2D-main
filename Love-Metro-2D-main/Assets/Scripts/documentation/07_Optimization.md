# Оптимизации производительности

## Выполненные оптимизации

### 1. PassengerRegistry - Кеширование пассажиров

**Проблема:** Многократные вызовы `FindObjectsOfType<Passenger>()` в Update циклах создавали O(n²) сложность.

**Решение:** Централизованный реестр с O(1) доступом.

```csharp
// Было (в каждом Update каждого пассажира):
foreach (var p in FindObjectsOfType<Passenger>())
{
    // O(n) на каждого пассажира = O(n²) всего
}

// Стало:
var sameGender = PassengerRegistry.Instance.Females; // O(1)
foreach (var p in sameGender)
{
    // O(n) всего
}
```

**Файл:** `Core/PassengerRegistry.cs`

### 2. Кеширование ссылок

**Проблема:** Повторные вызовы `FindObjectOfType<T>()` в методах.

**Решение:** Кеширование в Start() или при первом использовании.

```csharp
// ManualPairingManager.cs
private ScoreCounter _cachedScoreCounter;

private void Start()
{
    _cachedScoreCounter = FindObjectOfType<ScoreCounter>();
}

private void PairPassengers(...)
{
    // Ленивая инициализация как fallback
    if (_cachedScoreCounter == null)
        _cachedScoreCounter = FindObjectOfType<ScoreCounter>();
}
```

**Оптимизированные классы:**
- `ManualPairingManager` - кеширует ScoreCounter
- `TrainManager` - кеширует Spawner, ParallaxEffect, Container
- `Couple` - статический кеш ScoreCounter
- `CouplesManager` - кеширует TrainManager

### 3. Периодическая очистка null-ссылок

**Проблема:** При уничтожении пассажиров списки содержали null-ы, вызывая ошибки.

**Решение:** Автоматическая очистка каждые 2 секунды.

```csharp
// PassengerRegistry.cs
private float _cleanupInterval = 2f;
private float _nextCleanupTime = 0f;

private void Update()
{
    if (Time.time >= _nextCleanupTime)
    {
        _nextCleanupTime = Time.time + _cleanupInterval;
        CleanupNullReferences();
    }
}

public void CleanupNullReferences()
{
    _allPassengers.RemoveAll(p => p == null || !p);
    _males.RemoveAll(p => p == null || !p);
    _females.RemoveAll(p => p == null || !p);
    _singles.RemoveAll(p => p == null || !p);
}
```

### 4. Условная компиляция логов

**Проблема:** Debug.Log() создаёт накладные расходы даже когда отключён.

**Решение:** Атрибут `[Conditional]` полностью вырезает вызовы в Release.

```csharp
// Diagnostics.cs
[Conditional("UNITY_EDITOR")]
[Conditional("DEVELOPMENT_BUILD")]
[Conditional("DIAGNOSTICS_ENABLED")]
public static void Log(string message)
{
    if (Enabled) Debug.Log(message);
}
```

В Release сборке компилятор удаляет все вызовы `Diagnostics.Log()`.

### 5. Лимит пассажиров в сцене

**Проблема:** Бесконтрольный спавн перегружал сцену.

**Решение:** Жёсткий лимит с проверкой перед спавном.

```csharp
// PassangerSpawner.cs
[SerializeField] private int _maxPassengersInScene = 20;

public void spawnPassangers()
{
    _passiveContainer.CleanupNullReferences();
    int currentCount = _passiveContainer.Passangers?.Count ?? 0;

    if (currentCount >= _maxPassengersInScene)
        return;

    int remainingSlots = _maxPassengersInScene - currentCount;
    int spawnCount = Mathf.Min(7, remainingSlots);
    // ...
}
```

### 6. Правильная отписка от событий

**Проблема:** MissingReferenceException при обращении к уничтоженным объектам.

**Решение:** Отписка в OnDestroy().

```csharp
// Passenger.cs
private void OnDestroy()
{
    // Отписка от делегата поезда
    if (_train != null && _currentState != null)
        _train.startInertia -= _currentState.OnTrainSpeedChange;

    // Удаление из контейнера
    if (container != null)
        container.RemovePassanger(this);

    // Отмена регистрации
    if (PassengerRegistry.Instance != null)
        PassengerRegistry.Instance.Unregister(this);
}
```

---

## Рекомендации по дальнейшей оптимизации

### Object Pooling

Вместо Instantiate/Destroy использовать пул объектов:

```csharp
public class PassengerPool : MonoBehaviour
{
    private Queue<Passenger> _pool = new Queue<Passenger>();

    public Passenger Get()
    {
        if (_pool.Count > 0)
        {
            var p = _pool.Dequeue();
            p.gameObject.SetActive(true);
            return p;
        }
        return Instantiate(_prefab);
    }

    public void Return(Passenger p)
    {
        p.gameObject.SetActive(false);
        _pool.Enqueue(p);
    }
}
```

### Spatial Partitioning

Для очень большого количества пассажиров использовать пространственное разбиение:

```csharp
public class SpatialGrid
{
    private Dictionary<Vector2Int, List<Passenger>> _cells;
    private float _cellSize = 2f;

    public List<Passenger> GetNearby(Vector2 position, float radius)
    {
        // Проверяем только соседние ячейки
        Vector2Int cell = WorldToCell(position);
        int cellRadius = Mathf.CeilToInt(radius / _cellSize);

        List<Passenger> result = new List<Passenger>();
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                var key = cell + new Vector2Int(x, y);
                if (_cells.TryGetValue(key, out var list))
                    result.AddRange(list);
            }
        }
        return result;
    }
}
```

### Job System (для мобильных)

Вынести расчёт физики в Jobs:

```csharp
[BurstCompile]
public struct PassengerPhysicsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float2> Positions;
    [ReadOnly] public NativeArray<bool> IsFemale;
    public NativeArray<float2> Forces;

    public void Execute(int index)
    {
        // Расчёт притяжения/отталкивания
    }
}
```

### Профилирование

Используйте Unity Profiler для поиска узких мест:

1. `Window → Analysis → Profiler`
2. Запустите игру
3. Ищите пики в CPU Usage
4. Фокусируйтесь на методах с высоким Self ms

Ключевые метрики:
- **Update:** < 5ms на 60 FPS
- **Physics2D:** < 3ms
- **GC Alloc:** минимизировать (< 1KB/frame)

---

## Checklist производительности

- [x] Заменены FindObjectsOfType на Registry
- [x] Кешированы ссылки на синглтоны
- [x] Добавлена периодическая очистка null
- [x] Логи отключаются в Release
- [x] Установлен лимит пассажиров
- [x] Правильная отписка от событий
- [ ] Object Pooling (рекомендуется)
- [ ] Spatial Partitioning (для 50+ пассажиров)
- [ ] Job System (для мобильных)
