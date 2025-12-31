# Архитектура проекта Love Metro 2D

## Структура папок

```
Assets/Scripts/
├── Core/                    # Ядро игры и инфраструктура
│   ├── PassengerRegistry.cs # Реестр пассажиров (синглтон)
│   ├── GameBootstrap.cs     # Автоматическая инициализация
│   ├── GameInitializer.cs   # Инициализация компонентов сцены
│   ├── Diagnostics.cs       # Централизованное логирование
│   ├── ManualPairingManager.cs # Ручное создание пар кликом
│   └── VipBoundaryReturnSystem.cs # Возврат VIP в границы
│
├── Passenger/               # Система пассажиров
│   └── PassengerSettings.cs # ScriptableObject с настройками
│
├── FieldEffects/            # Эффекты поля
│   ├── FieldEffectSystem.cs # Менеджер эффектов
│   ├── IFieldEffect.cs      # Интерфейс эффекта
│   ├── IFieldEffectTarget.cs# Интерфейс цели эффекта
│   ├── GravityFieldEffectNew.cs # Гравитация/чёрная дыра
│   ├── WindFieldEffect.cs   # Эффект ветра
│   └── VortexFieldEffect.cs # Эффект вихря
│
├── Abilities/               # Система способностей
│   ├── PassengerAbility.cs  # Базовый класс способности
│   ├── PassengerAbilities.cs# Менеджер способностей пассажира
│   └── VipAbility.cs        # VIP способность (x2 очки)
│
├── Parallax/                # Параллакс и фон
│   ├── ParallaxEffect.cs    # Основной параллакс
│   ├── ParallaxMaterialDriver.cs # Управление материалами
│   └── SimpleBackgroundScroller.cs # Скроллинг фона
│
├── UI/                      # Пользовательский интерфейс
│   ├── ScoreCounter.cs      # Счётчик очков
│   ├── MenuManager.cs       # Управление меню
│   ├── InertiaArrowHUD.cs   # HUD-стрелка инерции
│   └── CharactersPanel.cs   # Панель выбора персонажей
│
├── Endless/                 # Бесконечный режим
│   └── EndlessModeManager.cs# Управление бесконечным режимом
│
├── _Deprecated/             # Устаревший/тестовый код
│   ├── BoundaryDiagnostic.cs
│   ├── UltimateDiagnostic.cs
│   └── ...
│
├── Passenger.cs             # Главный класс пассажира
├── TrainManager.cs          # Управление поездом
├── CouplesManager.cs        # Управление парами
├── Couple.cs                # Класс пары
├── PassangerSpawner.cs      # Спавнер пассажиров
├── PassangersContainer.cs   # Контейнер пассажиров
└── ClickDirectionManager.cs # Обработка ввода
```

## Архитектурные паттерны

### 1. Singleton Pattern

Используется для глобально доступных менеджеров:

```csharp
// Пример: PassengerRegistry
public class PassengerRegistry : MonoBehaviour
{
    public static PassengerRegistry Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
```

**Синглтоны в проекте:**
- `PassengerRegistry.Instance` - реестр пассажиров
- `CouplesManager.Instance` - менеджер пар
- `FieldEffectSystem.Instance` - система эффектов
- `ManualPairingManager.Instance` - ручное создание пар
- `ClickDirectionManager` (статические методы)

### 2. State Machine Pattern

Класс `Passenger` использует конечный автомат для управления поведением:

```
┌─────────────┐
│  Wandering  │ ← Начальное состояние (стоит/ходит)
└──────┬──────┘
       │ получает импульс
       ↓
┌─────────────┐
│   Falling   │ ← Полёт после толчка
└──────┬──────┘
       │ столкновение M+F
       ↓
┌─────────────┐
│  Matching   │ ← В паре
└─────────────┘
```

**Все состояния:**
- `Wandering` - блуждание/ожидание
- `StayingOnHandrail` - держится за поручень
- `Falling` - полёт после толчка (основное игровое состояние)
- `Flying` - полёт от ветра
- `Matching` - в паре
- `BeingAbsorbed` - поглощение чёрной дырой

### 3. Observer Pattern (Делегаты/События)

Используется для связи между компонентами без жёстких зависимостей:

```csharp
// TrainManager объявляет делегат
public delegate void StartInertia(Vector2 force);
public StartInertia startInertia;

// Passenger подписывается
train.startInertia += _currentState.OnTrainSpeedChange;

// TrainManager вызывает
startInertia?.Invoke(impulse);
```

**События в проекте:**
- `TrainManager.startInertia` - импульс инерции для пассажиров
- `TrainManager.OnBrakeStart/OnBrakeEnd` - начало/конец торможения
- `PassengerAbilities.OnMatched` - пассажир вошёл в пару

### 4. Strategy Pattern

Для движения пассажиров используются сменяемые стратегии:

```csharp
public interface IPassengerMovementStrategy
{
    Vector2 CalculateMovement(Passenger passenger, Vector2 currentDirection);
}

// Реализации:
- LegacyMovementStrategy    // Старое поведение
- SteeringSmoothStrategy    // Плавное рулевое
- LaneBasedStrategy         // По полосам
- BoidsLiteStrategy         // Поведение стаи (по умолчанию)
```

### 5. Component Pattern

Unity-стиль композиции через компоненты:

```
GameObject "Passenger"
├── Passenger (основная логика)
├── PassangerAnimator (анимации)
├── Rigidbody2D (физика)
├── Collider2D (коллизии)
├── SpriteRenderer (отрисовка)
└── PassengerAbilities (способности) [опционально]
```

## Поток данных

### Инициализация игры

```
1. GameInitializer.Awake()
   └── Создаёт недостающие менеджеры (ClickDirectionManager, etc.)

2. PassengerRegistry.Awake()
   └── Устанавливает Instance

3. TrainManager.Start()
   └── CacheReferences() - кеширует ссылки

4. PassangerSpawner.Start()
   └── spawnPassangers() - создаёт начальную волну

5. Passenger.Awake() [для каждого]
   └── Настройка Rigidbody, коллайдеров

6. Passenger.Start() [для каждого]
   └── Регистрация в PassengerRegistry

7. Passenger.Initiate() [вызывается Spawner'ом]
   └── Инициализация состояний, подписка на события
```

### Игровой цикл (каждый кадр)

```
1. ClickDirectionManager.Update()
   └── Обновляет HorizontalAxis, VerticalAxis, IsMouseHeld

2. TrainManager.Update()
   ├── Рассчитывает accelerationValue на основе ввода
   ├── Обновляет _currentSpeed
   ├── Вызывает startInertia при смене направления/фликах
   └── Обновляет камеру и параллакс

3. Passenger.Update() [для каждого]
   └── _currentState.UpdateState()
       ├── Wandering: ждёт импульс
       ├── Falling: применяет физику, магнит, отталкивание
       └── и т.д.

4. PassengerRegistry.Update()
   └── Периодическая очистка null-ссылок (каждые 2 сек)

5. Physics2D (Unity)
   └── Обрабатывает коллизии → OnCollisionEnter2D
```

### Создание пары

```
1. Falling.OnCollision() определяет столкновение M+F
   └── passenger.IsFemale != Passanger.IsFemale

2. Вызывается ForceToMatchingState(partner) для обоих

3. ForceToMatchingState():
   ├── Создаёт Couple объект (если x <= partner.x)
   ├── Рассчитывает очки через abilities
   ├── Вызывает ScoreCounter.AwardMatchPoints()
   └── Переключает состояние на Matching

4. Couple.init():
   ├── Устанавливает IsInCouple = true для обоих
   ├── Позиционирует пассажиров
   └── Запускает анимацию
```

## Зависимости между классами

```
                    ┌──────────────────┐
                    │   TrainManager   │
                    └────────┬─────────┘
                             │ startInertia
                             ↓
┌──────────────┐    ┌──────────────────┐    ┌─────────────────┐
│PassangerSpawn│───→│    Passenger     │←───│PassengerRegistry│
└──────────────┘    └────────┬─────────┘    └─────────────────┘
                             │
              ┌──────────────┼──────────────┐
              ↓              ↓              ↓
      ┌───────────┐  ┌───────────────┐  ┌────────┐
      │  Couple   │  │FieldEffectSys │  │Abilities│
      └─────┬─────┘  └───────────────┘  └────────┘
            │
            ↓
      ┌───────────┐
      │ScoreCounter│
      └───────────┘
```

## Обработка ошибок

### Защита от null-ссылок

```csharp
// Безопасные проверки перед использованием
if (PassengerRegistry.Instance != null)
{
    PassengerRegistry.Instance.FindClosestOpposite(...);
}

// Fallback на старый метод если Registry недоступен
else
{
    foreach (var p in FindObjectsOfType<Passenger>()) { ... }
}
```

### Очистка уничтоженных объектов

```csharp
// PassengerRegistry периодически чистит null-ы
private void Update()
{
    if (Time.time >= _nextCleanupTime)
    {
        CleanupNullReferences();
        _nextCleanupTime = Time.time + _cleanupInterval; // 2 сек
    }
}
```

### Отписка от событий в OnDestroy

```csharp
private void OnDestroy()
{
    // Предотвращает MissingReferenceException
    if (_train != null && _currentState != null)
        _train.startInertia -= _currentState.OnTrainSpeedChange;
}
```
