# Предложения по улучшению кода Love Metro 2D

**Дата анализа:** 2026-01-09
**Проект:** Love Metro 2D (Unity 2021.3 LTS)
**Текущее состояние:** 79 C# файлов, 3145 строк кода, 75+ тестов

---

## Содержание

1. [Краткое резюме](#краткое-резюме)
2. [Критические улучшения](#критические-улучшения)
3. [Архитектурные улучшения](#архитектурные-улучшения)
4. [Оптимизация производительности](#оптимизация-производительности)
5. [Качество кода](#качество-кода)
6. [Тестирование](#тестирование)
7. [Документация](#документация)
8. [План внедрения](#план-внедрения)

---

## Краткое резюме

### Сильные стороны проекта ✅

- ✅ Отличная документация (7 MD-файлов на русском/английском)
- ✅ Хорошее покрытие тестами (75+ тестов, ~85%)
- ✅ Уже внедрен PassengerRegistry для оптимизации
- ✅ Чистое разделение ответственности в менеджерах
- ✅ Активная работа над рефакторингом

### Основные проблемы ⚠️

- 🔴 **Passenger.cs слишком большой** (1412 строк)
- 🔴 **72 вызова FindObjectOfType** в 37 файлах
- 🟡 **Отсутствуют namespaces** (все классы в global scope)
- 🟡 **Опечатки в именах** (`Passanger` вместо `Passenger`)
- 🟡 **12 устаревших файлов** не удалены

---

## Критические улучшения

### 1. Разбить монолитный класс Passenger.cs

**Проблема:** 1412 строк в одном файле - сложно поддерживать и тестировать.

**Решение:** Разделить на логические компоненты:

```csharp
// Основной класс
Assets/Scripts/Passenger/Passenger.cs (200-300 строк)

// Компоненты
Assets/Scripts/Passenger/Components/
├── PassengerMovement.cs      // Логика движения
├── PassengerPhysics.cs       // Физика и импульсы
├── PassengerMagnetism.cs     // Притяжение/отталкивание
└── PassengerStateManager.cs  // Управление состояниями

// Состояния (уже есть в REFACTORING_GUIDE.md)
Assets/Scripts/Passenger/States/
├── WanderingState.cs
├── FallingState.cs
├── FlyingState.cs
└── MatchingState.cs
```

**Приоритет:** 🔴 Критический
**Оценка:** 2-3 дня
**Файлы:** `Assets/Scripts/Passenger.cs`

---

### 2. Заменить все FindObjectOfType на Dependency Injection

**Проблема:** 72 вызова FindObjectOfType замедляют игру при большом количестве объектов.

**Текущие проблемные места:**

```csharp
// ❌ Плохо (найдено 72 раза):
ManualPairingManager.cs:28   _cachedScoreCounter = FindObjectOfType<ScoreCounter>();
ParallaxMaterialDriver.cs:22 _train = FindObjectOfType<TrainManager>();
AutoFixRigidbodyLayers.cs:45 var allRigidbodies = Object.FindObjectsOfType<Rigidbody2D>();
```

**Решение:** Создать ServiceLocator или использовать DI:

```csharp
// ✅ Хорошо:
public class ServiceLocator : MonoBehaviour
{
    public static ServiceLocator Instance { get; private set; }

    [SerializeField] private ScoreCounter _scoreCounter;
    [SerializeField] private TrainManager _trainManager;
    [SerializeField] private CouplesManager _couplesManager;

    public ScoreCounter ScoreCounter => _scoreCounter;
    public TrainManager TrainManager => _trainManager;
    public CouplesManager CouplesManager => _couplesManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find если не назначены
        if (_scoreCounter == null) _scoreCounter = FindObjectOfType<ScoreCounter>();
        if (_trainManager == null) _trainManager = FindObjectOfType<TrainManager>();
        if (_couplesManager == null) _couplesManager = FindObjectOfType<CouplesManager>();
    }
}

// Использование:
var score = ServiceLocator.Instance.ScoreCounter;
```

**Приоритет:** 🔴 Критический
**Оценка:** 1-2 дня
**Файлы:** 37 файлов с FindObjectOfType

---

### 3. Добавить namespaces для всех классов

**Проблема:** Все 79 классов находятся в глобальном пространстве имен.

**Решение:**

```csharp
// Структура namespaces
namespace LoveMetro.Core
{
    public class PassengerRegistry { }
    public class GameBootstrap { }
    public class ManualPairingManager { }
}

namespace LoveMetro.Gameplay
{
    public class Passenger { }
    public class Couple { }
    public class CouplesManager { }
}

namespace LoveMetro.Physics
{
    public class TrainManager { }
    public class AdaptivePhysicsManager { }
}

namespace LoveMetro.Effects
{
    public interface IFieldEffect { }
    public class GravityFieldEffectNew { }
    public class WindFieldEffect { }
}

namespace LoveMetro.UI
{
    public class MenuManager { }
    public class ScoreCounter { }
    public class HealthBarUI { }
}

namespace LoveMetro.Movement
{
    public interface IPassengerMovementStrategy { }
    public class SteeringSmoothMovement { }
}

namespace LoveMetro.Abilities
{
    public class PassengerAbilities { }
    public class VipAbility { }
}
```

**Приоритет:** 🟡 Высокий
**Оценка:** 1 день (автоматизировать)
**Файлы:** Все 79 .cs файлов

---

### 4. Исправить опечатки в именах

**Проблема:** `Passanger` используется везде вместо `Passenger`.

**Файлы с опечками:**
```
PassangerSpawner.cs
PassangerAnimator.cs
PassangersContainer.cs
PassangerState.cs
```

**Решение:** Массовое переименование через рефакторинг IDE.

```bash
# Найти все вхождения
grep -r "Passanger" Assets/Scripts/

# Заменить через IDE:
1. Правый клик на классе → Rename (F2 в Rider)
2. Переименовать файлы
3. Обновить ссылки в префабах
```

**Приоритет:** 🟡 Средний
**Оценка:** 1-2 часа
**Файлы:** ~15 файлов

---

## Архитектурные улучшения

### 5. Уменьшить связанность через Singleton

**Проблема:** 64+ публичных static ссылок создают жесткую связанность.

**Текущая ситуация:**
```csharp
// ❌ Везде прямое обращение:
PassengerRegistry.Instance.FindClosestOpposite(...)
CouplesManager.Instance.CreateCouple(...)
ManualPairingManager.Instance.HandleClick(...)
```

**Улучшенное решение:**

```csharp
// ✅ Через конструктор/свойства
public class Passenger : MonoBehaviour
{
    private PassengerRegistry _registry;
    private CouplesManager _couplesManager;

    private void Awake()
    {
        // Inject dependencies
        _registry = ServiceLocator.Instance.Get<PassengerRegistry>();
        _couplesManager = ServiceLocator.Instance.Get<CouplesManager>();
    }

    private Passenger FindTarget()
    {
        return _registry.FindClosestOpposite(this, radius);
    }
}
```

**Преимущества:**
- Легче тестировать (можно подменить зависимости)
- Видны явные зависимости класса
- Проще поддерживать

**Приоритет:** 🟡 Средний
**Оценка:** 2-3 дня

---

### 6. Внедрить интерфейсы для ключевых систем

**Проблема:** Много конкретных классов без интерфейсов - сложно тестировать.

**Решение:**

```csharp
// Создать интерфейсы
namespace LoveMetro.Core
{
    public interface IPassengerRegistry
    {
        IReadOnlyList<Passenger> AllPassengers { get; }
        Passenger FindClosestOpposite(Passenger self, float radius);
        void Register(Passenger passenger);
        void Unregister(Passenger passenger);
    }

    public interface ICouplesManager
    {
        void CreateCouple(Passenger p1, Passenger p2);
        void BreakCouple(Couple couple);
    }

    public interface IScoreCounter
    {
        void AwardMatchPoints(Vector3 position, int points);
        int GetBasePointsPerCouple();
    }
}

// Реализация
public class PassengerRegistry : MonoBehaviour, IPassengerRegistry
{
    // ... существующий код
}
```

**Преимущества:**
- Легко создавать моки для тестов
- Можно заменять реализации
- Четкие контракты между системами

**Приоритет:** 🟡 Средний
**Оценка:** 1-2 дня

---

## Оптимизация производительности

### 7. Внедрить Object Pooling для пассажиров

**Проблема:** Instantiate/Destroy создает GC pressure и лаги.

**Решение:**

```csharp
namespace LoveMetro.Core
{
    public class PassengerPool : MonoBehaviour
    {
        public static PassengerPool Instance { get; private set; }

        [SerializeField] private Passenger _prefab;
        [SerializeField] private int _initialPoolSize = 20;
        [SerializeField] private int _maxPoolSize = 50;

        private Queue<Passenger> _availablePassengers = new Queue<Passenger>();
        private List<Passenger> _activePassengers = new List<Passenger>();

        private void Awake()
        {
            Instance = this;
            PrewarmPool();
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < _initialPoolSize; i++)
            {
                var p = CreateNewPassenger();
                p.gameObject.SetActive(false);
                _availablePassengers.Enqueue(p);
            }
        }

        public Passenger Spawn(Vector3 position, bool isFemale)
        {
            Passenger passenger;

            if (_availablePassengers.Count > 0)
            {
                passenger = _availablePassengers.Dequeue();
            }
            else if (_activePassengers.Count < _maxPoolSize)
            {
                passenger = CreateNewPassenger();
            }
            else
            {
                Debug.LogWarning("Pool exhausted!");
                return null;
            }

            passenger.transform.position = position;
            passenger.IsFemale = isFemale;
            passenger.gameObject.SetActive(true);
            _activePassengers.Add(passenger);

            return passenger;
        }

        public void Despawn(Passenger passenger)
        {
            if (passenger == null) return;

            passenger.gameObject.SetActive(false);
            _activePassengers.Remove(passenger);
            _availablePassengers.Enqueue(passenger);
        }

        private Passenger CreateNewPassenger()
        {
            return Instantiate(_prefab, transform);
        }
    }
}

// Использование в PassengerSpawner:
public class PassengerSpawner : MonoBehaviour
{
    public void SpawnPassengers()
    {
        for (int i = 0; i < count; i++)
        {
            bool isFemale = Random.value > 0.5f;
            var passenger = PassengerPool.Instance.Spawn(spawnPos, isFemale);
        }
    }
}
```

**Измеряемая польза:**
- Уменьшение GC Alloc на ~80%
- Устранение лагов при спавне
- Стабильный FPS

**Приоритет:** 🟡 Высокий
**Оценка:** 1 день
**Файлы:** `PassengerSpawner.cs`, новый `PassengerPool.cs`

---

### 8. Добавить Spatial Partitioning для поиска соседей

**Проблема:** Поиск ближайших пассажиров - O(n), при 50+ пассажирах тормозит.

**Решение:**

```csharp
namespace LoveMetro.Core
{
    /// <summary>
    /// Пространственное разбиение для быстрого поиска соседних пассажиров.
    /// Разбивает мир на сетку ячеек и хранит пассажиров в соответствующих ячейках.
    /// </summary>
    public class SpatialGrid
    {
        private Dictionary<Vector2Int, List<Passenger>> _cells = new Dictionary<Vector2Int, List<Passenger>>();
        private float _cellSize;

        public SpatialGrid(float cellSize = 3f)
        {
            _cellSize = cellSize;
        }

        public void Clear()
        {
            foreach (var list in _cells.Values)
                list.Clear();
        }

        public void Add(Passenger passenger)
        {
            var cell = WorldToCell(passenger.transform.position);
            if (!_cells.ContainsKey(cell))
                _cells[cell] = new List<Passenger>();
            _cells[cell].Add(passenger);
        }

        public List<Passenger> GetNearby(Vector2 position, float radius)
        {
            List<Passenger> result = new List<Passenger>();
            Vector2Int centerCell = WorldToCell(position);
            int cellRadius = Mathf.CeilToInt(radius / _cellSize);

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    Vector2Int cell = centerCell + new Vector2Int(x, y);
                    if (_cells.TryGetValue(cell, out var list))
                    {
                        result.AddRange(list);
                    }
                }
            }

            return result;
        }

        private Vector2Int WorldToCell(Vector2 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / _cellSize),
                Mathf.FloorToInt(worldPos.y / _cellSize)
            );
        }
    }
}

// Интеграция в PassengerRegistry:
public class PassengerRegistry : MonoBehaviour
{
    private SpatialGrid _spatialGrid = new SpatialGrid(3f);

    private void Update()
    {
        // Пересоздаем сетку каждый кадр (пассажиры двигаются)
        _spatialGrid.Clear();
        foreach (var p in _allPassengers)
        {
            if (p != null)
                _spatialGrid.Add(p);
        }
    }

    public Passenger FindClosestOpposite(Passenger self, float radius)
    {
        // Используем spatial grid вместо перебора всех
        var nearby = _spatialGrid.GetNearby(self.transform.position, radius);

        Passenger best = null;
        float bestDistSq = radius * radius;

        foreach (var p in nearby)
        {
            if (p == null || p == self || p.IsFemale == self.IsFemale || p.IsInCouple)
                continue;

            float distSq = (p.transform.position - self.transform.position).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = p;
            }
        }

        return best;
    }
}
```

**Измеряемая польза:**
- Поиск соседей: O(n) → O(k) где k = количество в области
- При 50 пассажирах: ~10x ускорение
- При 100+ пассажирах: ~50x ускорение

**Приоритет:** 🟢 Низкий (только если >30 пассажиров)
**Оценка:** 1 день

---

### 9. Оптимизировать Passenger.Update()

**Проблема:** Update() вызывается 60 раз в секунду для каждого пассажира.

**Текущий код:**
```csharp
// Passenger.cs
private void Update()
{
    if (!_isInitiated) return;

    _currentState?.UpdateState();  // Вызывается каждый кадр
    ApplyFieldForces();            // Вызывается каждый кадр
}
```

**Оптимизация:**

```csharp
// 1. Используйте FixedUpdate для физики
private void FixedUpdate()
{
    if (!_isInitiated) return;
    ApplyFieldForces();  // Физика 50 раз/сек
}

private void Update()
{
    if (!_isInitiated) return;
    _currentState?.UpdateState();  // Логика 60 раз/сек
}

// 2. Кешируйте повторяющиеся вычисления
private Vector3 _cachedPosition;
private float _positionCacheTime;
private const float CACHE_DURATION = 0.1f;

public Vector3 GetCachedPosition()
{
    if (Time.time - _positionCacheTime > CACHE_DURATION)
    {
        _cachedPosition = transform.position;
        _positionCacheTime = Time.time;
    }
    return _cachedPosition;
}

// 3. Пропускайте обновления для дальних пассажиров
private void Update()
{
    if (!_isInitiated) return;

    // Оптимизация: не обновляем далеких пассажиров так часто
    if (!IsVisibleToPlayer())
    {
        if (Time.frameCount % 3 != 0) // Обновляем каждый 3-й кадр
            return;
    }

    _currentState?.UpdateState();
}

private bool IsVisibleToPlayer()
{
    // Проверка через камеру или простая проверка расстояния
    if (Camera.main == null) return true;

    Vector3 viewportPoint = Camera.main.WorldToViewportPoint(transform.position);
    return viewportPoint.x >= -0.1f && viewportPoint.x <= 1.1f &&
           viewportPoint.y >= -0.1f && viewportPoint.y <= 1.1f;
}
```

**Приоритет:** 🟡 Средний
**Оценка:** 3-4 часа

---

## Качество кода

### 10. Устранить "магические числа"

**Проблема:** Множество hard-coded значений затрудняют настройку.

**Примеры:**
```csharp
// ❌ Магические числа:
if (Time.frameCount % 3 != 0) return;  // Почему 3?
_cleanupInterval = 2f;                 // Почему 2 секунды?
_maxPassengersInScene = 20;            // Почему 20?
```

**Решение:**

```csharp
// ✅ Константы с говорящими именами:
namespace LoveMetro.Config
{
    public static class GameConstants
    {
        // Performance
        public const int DISTANT_PASSENGER_UPDATE_INTERVAL = 3;
        public const float REGISTRY_CLEANUP_INTERVAL_SECONDS = 2f;
        public const int MAX_PASSENGERS_IN_SCENE = 20;

        // Physics
        public const float MAGNET_RADIUS = 3.5f;
        public const float REPEL_RADIUS = 2.0f;
        public const float BOUNCE_ELASTICITY = 0.95f;

        // Gameplay
        public const float MANUAL_PAIRING_MAX_DISTANCE = 3.0f;
        public const float HANDRAIL_GRAB_CHANCE = 0.7f;
    }
}

// Использование:
if (Time.frameCount % GameConstants.DISTANT_PASSENGER_UPDATE_INTERVAL != 0)
    return;
```

**Приоритет:** 🟢 Низкий
**Оценка:** 2-3 часа

---

### 11. Добавить XML-документацию к публичным методам

**Проблема:** Не все публичные API документированы.

**Текущее состояние:**
```csharp
// ❌ Без документации
public void ForceToMatchingState(Passenger other)
{
    // ...
}
```

**Улучшение:**

```csharp
/// <summary>
/// Принудительно переводит пассажира в состояние создания пары с указанным партнером.
/// Используется для ручного создания пар через ManualPairingManager.
/// </summary>
/// <param name="other">Пассажир-партнер для создания пары. Должен быть противоположного пола.</param>
/// <exception cref="ArgumentNullException">Если other == null</exception>
/// <exception cref="InvalidOperationException">Если пассажир уже в паре</exception>
public void ForceToMatchingState(Passenger other)
{
    if (other == null)
        throw new ArgumentNullException(nameof(other));

    if (IsInCouple)
        throw new InvalidOperationException("Passenger already in couple");

    // ...
}
```

**Приоритет:** 🟢 Низкий
**Оценка:** 1 день (можно автоматизировать)

---

### 12. Удалить устаревший код

**Проблема:** 12 файлов в `_Deprecated/` засоряют проект.

**Файлы для удаления:**
```
Assets/Scripts/_Deprecated/
├── FieldEffectsTest/
│   ├── BlackHoleTest.cs
│   ├── InitializeFieldEffectsOnStart.cs
│   ├── SceneSetup.cs
│   └── WindDiagnostic.cs
├── BoundaryCollisionDiagnostic.cs
├── BoundaryDiagnostics.cs
├── DiagnosticToggle.cs
└── ... (еще 5 файлов)
```

**Решение:**

```bash
# 1. Создать архив старого кода
mkdir -p Archives/Deprecated_2026-01
git mv Assets/Scripts/_Deprecated/* Archives/Deprecated_2026-01/

# 2. Зафиксировать в Git
git commit -m "Archive deprecated code from 2025"

# 3. Удалить из проекта
rm -rf Assets/Scripts/_Deprecated/

# Или просто удалить, если код не нужен:
git rm -r Assets/Scripts/_Deprecated/
git commit -m "Remove deprecated test code"
```

**Приоритет:** 🟢 Низкий
**Оценка:** 30 минут

---

## Тестирование

### 13. Добавить Play Mode тесты

**Проблема:** Все тесты в Edit Mode - не проверяется реальная физика.

**Текущее покрытие:**
```
✅ PassengerRegistry (95%)
✅ ManualPairingManager (90%)
✅ ScoreCounter (85%)
✅ Abilities (80%)
❌ TrainManager (0%)
❌ Passenger Physics (0%)
❌ Field Effects (0%)
```

**Решение:**

```csharp
// Assets/Tests/PlayMode/PassengerPhysicsTests.cs
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoveMetro.Tests.PlayMode
{
    public class PassengerPhysicsTests
    {
        private GameObject _testScene;
        private TrainManager _train;
        private Passenger _passenger;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Создаем минимальную сцену
            _testScene = new GameObject("TestScene");

            // Добавляем TrainManager
            var trainObj = new GameObject("Train");
            trainObj.transform.SetParent(_testScene.transform);
            _train = trainObj.AddComponent<TrainManager>();

            // Создаем пассажира
            var passengerObj = new GameObject("Passenger");
            passengerObj.transform.SetParent(_testScene.transform);
            passengerObj.AddComponent<Rigidbody2D>();
            passengerObj.AddComponent<BoxCollider2D>();
            _passenger = passengerObj.AddComponent<Passenger>();

            yield return null; // Ждем 1 кадр для инициализации
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Object.Destroy(_testScene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Passenger_RespondsToTrainImpulse()
        {
            // Arrange
            Vector2 initialPos = _passenger.transform.position;
            Vector2 impulse = new Vector2(5f, 0f);

            // Act
            _train.ApplyImpulse(impulse);
            yield return new WaitForSeconds(0.5f); // Ждем физику

            // Assert
            Vector2 finalPos = _passenger.transform.position;
            Assert.Greater(Vector2.Distance(initialPos, finalPos), 0.1f,
                "Passenger should move after train impulse");
        }

        [UnityTest]
        public IEnumerator Passenger_StopsAfterDeceleration()
        {
            // Проверка, что пассажир останавливается
            _passenger.GetComponent<Rigidbody2D>().velocity = new Vector2(10f, 0f);
            yield return new WaitForSeconds(2f);

            var velocity = _passenger.GetComponent<Rigidbody2D>().velocity;
            Assert.Less(velocity.magnitude, 1f, "Passenger should slow down");
        }
    }
}
```

**Приоритет:** 🟡 Средний
**Оценка:** 2-3 дня

---

### 14. Добавить стресс-тесты производительности

**Проблема:** Неизвестно, как игра работает при большой нагрузке.

**Решение:**

```csharp
// Assets/Tests/Performance/PerformanceTests.cs
using NUnit.Framework;
using System.Diagnostics;
using UnityEngine;

namespace LoveMetro.Tests.Performance
{
    public class PerformanceTests
    {
        [Test]
        public void PassengerRegistry_FindClosest_UnderMillisecond()
        {
            // Arrange
            var registry = CreateTestRegistry(100); // 100 пассажиров
            var testPassenger = registry.AllPassengers[0];
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            for (int i = 0; i < 1000; i++)
            {
                registry.FindClosestOpposite(testPassenger, 5f);
            }
            stopwatch.Stop();

            // Assert
            double avgMs = stopwatch.Elapsed.TotalMilliseconds / 1000.0;
            Assert.Less(avgMs, 1.0,
                $"FindClosestOpposite должен работать <1ms, actual: {avgMs:F3}ms");
        }

        [Test]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        public void Registry_Scales_Linearly(int passengerCount)
        {
            var registry = CreateTestRegistry(passengerCount);
            var stopwatch = Stopwatch.StartNew();

            // Simulate frame update
            for (int i = 0; i < passengerCount; i++)
            {
                registry.FindClosestOpposite(
                    registry.AllPassengers[i], 5f);
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log(
                $"[{passengerCount} passengers] took {stopwatch.ElapsedMilliseconds}ms");
        }

        private PassengerRegistry CreateTestRegistry(int count)
        {
            // Создать тестовый реестр с N пассажирами
            // ...
        }
    }
}
```

**Приоритет:** 🟢 Низкий
**Оценка:** 1 день

---

## Документация

### 15. Создать главный README.md

**Проблема:** Нет общего обзора проекта в корне.

**Решение:**

```markdown
# Love Metro 2D

2D симулятор знакомств в метро на Unity. Управляйте поездом, создавайте инерцию и
соединяйте пассажиров в пары!

## Быстрый старт

1. **Требования**
   - Unity 2021.3 LTS или новее
   - C# 9.0+

2. **Установка**
   ```bash
   git clone <repo>
   cd Love-Metro-2D-main/Love-Metro-2D-main
   ```

3. **Запуск**
   - Откройте проект в Unity
   - Откройте сцену `Assets/Scenes/MainMenu`
   - Нажмите Play

## Архитектура

- **Passenger System** - Управление пассажирами (FSM, физика, способности)
- **Train Manager** - Физика поезда и инерция
- **Couples Manager** - Создание и разрыв пар
- **Field Effects** - Гравитация, ветер, вихри
- **Abilities** - VIP бонусы и особые способности

Подробнее: [Документация](Assets/Scripts/documentation/)

## Тестирование

```bash
# Unity Test Runner
Window → General → Test Runner → Run All

# Или через командную строку
Unity -runTests -testPlatform EditMode
```

Покрытие: ~85% (75+ тестов)

## Производительность

- PassengerRegistry вместо FindObjectsOfType (30x ускорение)
- Кеширование ссылок на синглтоны
- Лимит пассажиров (по умолчанию 20)

## Структура проекта

```
Assets/
├── Scripts/           # C# код (79 файлов)
│   ├── Core/          # Основные системы
│   ├── Passenger/     # Логика пассажиров
│   ├── FieldEffects/  # Эффекты среды
│   ├── UI/            # Интерфейс
│   └── documentation/ # Подробные гайды
├── Tests/             # Тесты (75+)
└── Scenes/            # Игровые сцены
```

## Разработка

См. [CONTRIBUTING.md](CONTRIBUTING.md)

## Лицензия

[Ваша лицензия]
```

**Приоритет:** 🟡 Средний
**Оценка:** 1-2 часа

---

## План внедрения

### Фаза 1: Критические улучшения (1 неделя)

**Неделя 1:**
```
День 1-2: ServiceLocator + замена FindObjectOfType (37 файлов)
День 3-4: Разбиение Passenger.cs на компоненты
День 5:   Добавление namespaces (автоматизировать)
День 6:   Исправление опечаток Passanger → Passenger
День 7:   Тестирование и фиксы
```

**Результат:**
- ✅ Убрано 72 вызова FindObjectOfType
- ✅ Passenger.cs уменьшен с 1412 до ~300 строк
- ✅ Все классы в правильных namespaces
- ✅ Исправлены опечатки

---

### Фаза 2: Производительность (1 неделя)

**Неделя 2:**
```
День 1-2: Object Pooling для пассажиров
День 3:   Оптимизация Passenger.Update()
День 4:   Spatial Partitioning (опционально)
День 5-7: Замеры производительности и оптимизация
```

**Результат:**
- ✅ GC Alloc снижен на 80%
- ✅ Стабильный FPS при 50+ пассажирах
- ✅ Нет лагов при спавне

---

### Фаза 3: Качество кода (3-5 дней)

**Неделя 3:**
```
День 1:   Внедрение интерфейсов
День 2:   Уменьшение Singleton coupling
День 3:   Константы вместо магических чисел
День 4:   XML-документация
День 5:   Удаление deprecated кода
```

**Результат:**
- ✅ Код легче тестировать
- ✅ Меньше coupling между классами
- ✅ Лучшая документация

---

### Фаза 4: Тесты и документация (1 неделя)

**Неделя 4:**
```
День 1-3: Play Mode тесты
День 4:   Performance тесты
День 5:   Главный README.md
День 6-7: Итоговое тестирование
```

**Результат:**
- ✅ Покрытие тестами 90%+
- ✅ Проект хорошо документирован
- ✅ Известны метрики производительности

---

## Метрики успеха

### До улучшений ❌

- ⏱️ **FindObjectOfType вызовы:** 72 раза в 37 файлах
- 📏 **Passenger.cs:** 1412 строк
- 🧪 **Покрытие тестами:** 85% (только Edit Mode)
- 🗂️ **Namespaces:** 0 (все в global)
- 📝 **Опечатки:** 15+ файлов с "Passanger"
- 🗑️ **Deprecated код:** 12 файлов

### После улучшений ✅

- ⏱️ **FindObjectOfType вызовы:** 0 (все через ServiceLocator)
- 📏 **Passenger.cs:** ~300 строк (разбит на компоненты)
- 🧪 **Покрытие тестами:** 90%+ (Edit + Play Mode)
- 🗂️ **Namespaces:** 7 namespaces (LoveMetro.*)
- 📝 **Опечатки:** 0
- 🗑️ **Deprecated код:** Удален
- 🎯 **Object Pooling:** Внедрен
- 📊 **Performance tests:** Добавлены

---

## Дополнительные рекомендации

### Для мобильных платформ

Если планируется мобильная версия:

1. **Unity Job System** - вынести физику в Jobs
2. **Burst Compiler** - компилировать критические участки
3. **IL2CPP** - вместо Mono для лучшей производительности
4. **Texture Atlasing** - объединить спрайты

### Для CI/CD

Автоматизация:

```yaml
# .github/workflows/tests.yml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: game-ci/unity-test-runner@v2
        with:
          unityVersion: 2021.3.15f1
          testMode: all
      - uses: game-ci/unity-builder@v2
        with:
          targetPlatform: StandaloneWindows64
```

### Для командной разработки

1. **Code Review checklist** в CONTRIBUTING.md
2. **Git hooks** для проверки кода перед коммитом
3. **EditorConfig** для единого стиля
4. **Conventional Commits** для понятной истории

---

## Заключение

Проект Love Metro 2D имеет **солидную кодовую базу** с хорошей архитектурой и тестами.
Основные улучшения сфокусированы на:

1. 🔴 **Уменьшении сложности** (Passenger.cs)
2. 🔴 **Оптимизации производительности** (FindObjectOfType, Object Pooling)
3. 🟡 **Улучшении архитектуры** (Namespaces, DI, Interfaces)
4. 🟢 **Повышении качества кода** (Документация, тесты)

**Приоритет:** Начать с Фазы 1 (критические улучшения), затем Фазы 2 (производительность).

**Срок реализации:** 3-4 недели для всех фаз.

**Риски:** Минимальные - большинство улучшений не ломают существующий функционал.

---

**Подготовлено:** 2026-01-09
**Версия:** 1.0
**Статус:** Готов к внедрению
