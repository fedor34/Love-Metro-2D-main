# Руководство по рефакторингу Love Metro 2D

## Что было сделано

### 1. PassengerRegistry (Core/PassengerRegistry.cs)
Централизованный реестр всех пассажиров, заменяющий дорогие вызовы `FindObjectsOfType<Passenger>()`.

**Преимущества:**
- O(1) доступ к спискам пассажиров вместо O(n) сканирования сцены
- Отдельные списки для мужчин, женщин и одиноких
- Методы `FindClosestOpposite()` и `GetSameGenderInRadius()` для быстрого поиска

**Использование:**
```csharp
// Вместо FindObjectsOfType<Passenger>()
var allPassengers = PassengerRegistry.Instance.AllPassengers;

// Поиск ближайшего противоположного пола
Passenger target = PassengerRegistry.Instance.FindClosestOpposite(self, radius);

// Получить количество возможных пар
int pairs = PassengerRegistry.Instance.GetPossiblePairsCount();
```

### 2. PassengerSettings (Passenger/PassengerSettings.cs)
ScriptableObject со всеми настройками пассажира. Заменяет "magic numbers" в коде.

**Создание ассета:**
1. Правый клик в Project → Create → Love Metro → Passenger Settings
2. Назовите файл "PassengerSettings" и положите в папку Resources
3. Настройте параметры в инспекторе

**Категории настроек:**
- Базовое движение (скорость, множители)
- Поручни (шанс схватиться, время удержания)
- Импульс от поезда (чувствительность, пороги)
- Полёт (скорость, затухание, отскоки)
- Магнитное притяжение/отталкивание
- Aim Assist

### 3. Состояния Passenger (Passenger/States/)
Все состояния вынесены в отдельные файлы:

- `PassengerState.cs` — базовый абстрактный класс
- `WanderingState.cs` — ожидание/прогулка
- `FallingState.cs` — падение после импульса
- `FlyingState.cs` — полёт под действием ветра
- `StayingOnHandrailState.cs` — удержание за поручень
- `MatchingState.cs` — создание пары
- `BeingAbsorbedState.cs` — поглощение чёрной дырой

### 4. PassengerRefactored (Passenger/PassengerRefactored.cs)
Новый рефакторированный класс пассажира, использующий все новые компоненты.

**Миграция:**
1. Переименуйте `Passenger.cs` в `PassengerLegacy.cs`
2. Переименуйте `PassengerRefactored.cs` в `Passenger.cs`
3. Обновите префабы

### 5. Оптимизированные классы
- `CouplesManager.cs` — использует PassengerRegistry
- `Couple.cs` — кеширует ScoreCounter

## Структура папок

```
Assets/Scripts/
├── Core/
│   ├── PassengerRegistry.cs    ← НОВЫЙ
│   ├── GameBootstrap.cs        ← НОВЫЙ
│   └── ...
├── Passenger/                   ← НОВАЯ ПАПКА
│   ├── PassengerSettings.cs    ← НОВЫЙ
│   ├── PassengerRefactored.cs  ← НОВЫЙ
│   └── States/                 ← НОВЫЕ
│       ├── PassengerState.cs
│       ├── WanderingState.cs
│       ├── FallingState.cs
│       ├── FlyingState.cs
│       ├── StayingOnHandrailState.cs
│       ├── MatchingState.cs
│       └── BeingAbsorbedState.cs
├── _Deprecated/                 ← Перемещённые тестовые скрипты
└── ...
```

## Шаги миграции

### Минимальная миграция (рекомендуется)
1. Добавьте в сцену пустой GameObject с компонентом `GameBootstrap`
2. Старый `Passenger.cs` будет работать, но использовать PassengerRegistry для оптимизации

### Полная миграция
1. Создайте ассет PassengerSettings в Resources
2. Замените `Passenger.cs` на `PassengerRefactored.cs`
3. Обновите все префабы пассажиров:
   - Добавьте ссылку на PassengerSettings
   - Удалите неиспользуемые SerializedField

## Важные изменения API

### Passenger
```csharp
// Старый API (работает через fallback)
passenger.GetRigidbody()

// Новый API
passenger.Rigidbody  // Публичное свойство
passenger.Settings   // Доступ к настройкам
passenger.WanderingState  // Доступ к состояниям
```

### Couple
```csharp
// При смене сцены вызывайте
Couple.ClearCache();
```

## Производительность

### До рефакторинга
- `FindObjectsOfType<Passenger>()` вызывался каждый кадр в:
  - `Passenger.UpdateState()` (2 раза — магнит и отталкивание)
  - `CouplesManager.Update()`
  - `Couple.init()`

### После рефакторинга
- Все поиски через `PassengerRegistry.Instance`
- O(1) доступ к кешированным спискам
- При 30 пассажирах: ~30x ускорение поиска

## Обратная совместимость

- Старый `Passenger.cs` продолжает работать
- `CouplesManager` и `Couple` имеют fallback на старое поведение
- Новые компоненты опциональны

## Что НЕ менять

- Префабы пассажиров (без полной миграции)
- Анимации и контроллеры
- Спрайты и материалы
- Сцены (только добавить GameBootstrap)
