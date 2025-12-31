# Игровые системы

## TrainManager - Управление поездом

**Файл:** `TrainManager.cs`

### Назначение

TrainManager симулирует движение поезда метро и создаёт инерцию, которая толкает пассажиров. Это центральный класс для игровой механики.

### Основные параметры

```csharp
[Header("Параметры движения")]
[SerializeField] private float _maxSpeed = 480f;      // Максимальная скорость
[SerializeField] private float _minSpeed = 1f;        // Минимальная скорость
[SerializeField] private float _acceleration = 180f;  // Базовое ускорение
[SerializeField] private float _deceleration = 10f;   // Замедление
[SerializeField] private float _brakeDeceleration = 25f; // Торможение

[Header("Импульсы направления")]
[SerializeField] private float _dirImpulseMin = 6f;        // Мин. сила импульса
[SerializeField] private float _dirImpulseScale = 35f;     // Множитель от скорости жеста
[SerializeField] private float _dirImpulseCooldown = 0.15f;// Кулдаун между импульсами
[SerializeField] private float _dirFlickThreshold = 0.95f; // Порог скорости для флика
```

### Делегат инерции

```csharp
public delegate void StartInertia(Vector2 force);
public StartInertia startInertia;
```

Все пассажиры подписываются на этот делегат и получают импульс при его вызове.

### Логика Update

```csharp
private void Update()
{
    bool isAccelerating = ClickDirectionManager.IsMouseHeld;

    if (isAccelerating)
    {
        float x = ClickDirectionManager.HorizontalAxis; // -1..1
        float vx = ClickDirectionManager.HorizontalVelocity;

        // Горизонтальное перетаскивание = ускорение/торможение
        if (x > 0f)
            accelerationValue = x * _acceleration * 4f; // вправо = разгон
        else if (x < 0f)
            accelerationValue = x * _brakeDeceleration * 3f; // влево = тормоз

        // Флики усиливают действие
        if (vx > 0.7f)
            accelerationValue += _acceleration * 3f * Mathf.Clamp01(vx - 0.7f);
        if (vx < -0.7f)
            accelerationValue += -_brakeDeceleration * 4f * Mathf.Clamp01(-0.7f - vx);

        // Импульс при смене направления
        if (DirectionChanged(x, _lastAxis))
        {
            Vector2 impulse = CalculateDirectionImpulse(x, vx);
            startInertia?.Invoke(impulse);
        }
    }

    SetSpeed(_currentSpeed + accelerationValue * Time.deltaTime);
}
```

### Типы импульсов

#### 1. Стартовый импульс

При нажатии мыши после состояния покоя:
```csharp
if (Input.GetMouseButtonDown(0))
{
    bool wasAtRest = _currentSpeed <= _startImpulseSpeedThreshold;
    if (!_accelImpulseGiven && wasAtRest)
    {
        float accelMag = Mathf.Max(4f, boostSpeed * 1.6f + boostSpeed * boostSpeed * 0.18f);
        var impulse = Vector2.left * accelMag;
        impulse = Rotate(impulse, Mathf.Sin(_turnPhase) * _turnAmplitudeDeg);
        startInertia?.Invoke(impulse);
        _accelImpulseGiven = true;
    }
}
```

#### 2. Импульс смены направления

При резкой смене направления жеста:
```csharp
if (Mathf.Sign(x) != Mathf.Sign(_lastAxis))
{
    float v = Mathf.Abs(vx);
    float asym = x > 0f ? 0.75f : 1.35f; // торможение сильнее
    float mag = Mathf.Max(_dirImpulseMin, v * _dirImpulseScale * asym);
    Vector2 impulse = new Vector2(signX * mag, signY * mag * 0.6f);
    startInertia?.Invoke(impulse);
}
```

#### 3. Повторные импульсы (флики)

При быстрых резких движениях:
```csharp
if (Mathf.Abs(vx) > _dirFlickThreshold)
{
    // Аналогичный расчёт импульса
    startInertia?.Invoke(impulse);
}
```

### Симуляция поворотов

Импульсы поворачиваются синусоидально для имитации поворотов поезда:
```csharp
[SerializeField] private float _turnAmplitudeDeg = 45f;
[SerializeField] private float _turnSpeed = 0.6f;
private float _turnPhase = 0f;

private void Update()
{
    _turnPhase += Time.deltaTime * _turnSpeed;
    // ...
    impulse = Rotate(impulse, Mathf.Sin(_turnPhase) * _turnAmplitudeDeg);
}
```

### Остановка на станции

```csharp
public void StationStopAndSpawn(float pauseSeconds = 1.0f)
{
    StartCoroutine(StationStopRoutine(pauseSeconds));
}

private IEnumerator StationStopRoutine(float pauseSeconds)
{
    _isStopped = true;
    SetSpeed(0f);
    OnBrakeStart?.Invoke();

    yield return new WaitForSeconds(pauseSeconds);

    // Уничтожаем старых пассажиров
    _passangers.DestroyAllPassengers();

    // Спавним новых
    _spawner?.spawnPassangers();

    OnBrakeEnd?.Invoke();
    _isStopped = false;
}
```

---

## PassangerSpawner - Спавн пассажиров

**Файл:** `PassangerSpawner.cs`

### Конфигурация

```csharp
[SerializeField] private List<Transform> _spawnLocations;      // Точки спавна
[SerializeField] private List<Passenger> _passangerFemalePrefs; // Женские префабы
[SerializeField] private List<Passenger> _passangerMalePrefs;   // Мужские префабы
[SerializeField] private int _maxPassengersInScene = 20;        // Лимит
```

### Алгоритм спавна

```csharp
public void spawnPassangers()
{
    // 1. Проверка лимита
    _passiveContainer.CleanupNullReferences();
    int currentCount = _passiveContainer.Passangers?.Count ?? 0;
    if (currentCount >= _maxPassengersInScene)
        return;

    // 2. Перемешивание точек спавна
    List<Transform> availableLocations = new List<Transform>(_spawnLocations);
    ShuffleList(availableLocations);

    // 3. Определение количества (5-7, с учётом лимита)
    int remainingSlots = _maxPassengersInScene - currentCount;
    int maxPossibleSpawn = Mathf.Min(7, availableLocations.Count, remainingSlots);
    int spawnCount = Random.Range(Mathf.Min(5, maxPossibleSpawn), maxPossibleSpawn + 1);

    // 4. Равномерное распределение полов
    List<bool> genderDistribution = CreateGenderDistribution(spawnCount);
    ShuffleList(genderDistribution);

    // 5. Создание пассажиров
    for (int i = 0; i < spawnCount; i++)
    {
        bool createFemale = genderDistribution[i];
        Passenger prefab = createFemale ? GetNextFemale() : GetNextMale();
        Transform spawnPoint = availableLocations[i];

        Passenger passenger = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        passenger.Initiate(randomDirection, _trainManager, _scoreCounter);
        passenger.container = _passiveContainer;
        _passiveContainer.Passangers.Add(passenger);

        // Добавление способностей
        AttachGlobalAbilities(passenger);
    }

    // 6. Назначение VIP паре
    TryAssignVipPair(spawnedThisWave);
}
```

### VIP система

```csharp
[SerializeField] private VipAbility _vipAbility;
[Range(0f, 1f)]
[SerializeField] private float _vipPairChance = 1f; // 100% шанс по умолчанию

private void TryAssignVipPair(List<Passenger> spawned)
{
    if (_vipAbility == null || spawned.Count < 2)
        return;
    if (Random.value > _vipPairChance)
        return;

    // Находим одну женщину и одного мужчину
    Passenger female = spawned.FirstOrDefault(p => p.IsFemale && !p.IsInCouple);
    Passenger male = spawned.FirstOrDefault(p => !p.IsFemale && !p.IsInCouple);

    if (female != null && male != null)
    {
        ApplyVip(female);
        ApplyVip(male);
    }
}

private void ApplyVip(Passenger p)
{
    var runner = p.GetComponent<PassengerAbilities>()
                 ?? p.gameObject.AddComponent<PassengerAbilities>();
    runner.AddAbility(_vipAbility);
    runner.AttachAll();
}
```

---

## Couple - Система пар

**Файл:** `Couple.cs`

### Создание пары

```csharp
public void init(Passenger lead, Passenger follow)
{
    _male = lead.IsFemale ? follow : lead;
    _female = lead.IsFemale ? lead : follow;

    // Отмечаем обоих как "в паре"
    _male.IsInCouple = true;
    _female.IsInCouple = true;

    // Обновляем реестр
    if (PassengerRegistry.Instance != null)
    {
        PassengerRegistry.Instance.UpdateCoupleStatus(_male);
        PassengerRegistry.Instance.UpdateCoupleStatus(_female);
    }

    // Позиционирование
    transform.position = (_male.transform.position + _female.transform.position) * 0.5f;
    _male.transform.SetParent(transform);
    _female.transform.SetParent(transform);

    // Центрирование пассажиров
    CenterPassengers();

    // Анимация
    StartCoroutine(FadeAndDestroy());
}
```

### Центрирование

```csharp
private void CenterPassengers()
{
    Vector3 center = transform.position;
    float offset = _coupleSpacing * 0.5f;

    _male.transform.position = center + Vector3.left * offset;
    _female.transform.position = center + Vector3.right * offset;

    // Поворот лицом друг к другу
    _male.PassangerAnimator.ChangeFacingDirection(true);  // вправо
    _female.PassangerAnimator.ChangeFacingDirection(false); // влево
}
```

### Разрушение пары

Пара может быть разбита столкновением с летящим пассажиром:

```csharp
public void BreakByHit(Passenger hitter)
{
    if (_isBreaking) return;
    _isBreaking = true;

    // Отвязываем пассажиров
    _male.transform.SetParent(null);
    _female.transform.SetParent(null);

    // Рассчитываем направление разлёта
    Vector2 hitDir = (transform.position - hitter.transform.position).normalized;
    Vector2 kickMale = hitDir + Vector2.left * 0.5f;
    Vector2 kickFemale = hitDir + Vector2.right * 0.5f;

    // Запускаем в полёт
    _male.BreakFromCouple(kickMale.normalized * _breakForce);
    _female.BreakFromCouple(kickFemale.normalized * _breakForce);

    Destroy(gameObject);
}
```

### Анимация исчезновения

```csharp
private IEnumerator FadeAndDestroy()
{
    yield return new WaitForSeconds(_coupleDisplayTime);

    float elapsed = 0f;
    while (elapsed < _fadeDuration)
    {
        elapsed += Time.deltaTime;
        float alpha = 1f - (elapsed / _fadeDuration);
        SetAlpha(alpha);
        yield return null;
    }

    // Удаляем из контейнера и уничтожаем
    _male.RemoveFromContainerAndDestroy();
    _female.RemoveFromContainerAndDestroy();
    Destroy(gameObject);
}
```

---

## CouplesManager - Менеджер пар

**Файл:** `CouplesManager.cs`

### Назначение

Централизованное управление всеми парами в сцене.

### Singleton

```csharp
public static CouplesManager Instance { get; private set; }

private void Awake()
{
    Instance = this;
}
```

### Отслеживание пар

```csharp
private List<Couple> _activeCouples = new List<Couple>();

public void RegisterCouple(Couple couple)
{
    if (!_activeCouples.Contains(couple))
        _activeCouples.Add(couple);
}

public void UnregisterCouple(Couple couple)
{
    _activeCouples.Remove(couple);
}
```

### Проверка возможных пар

Использует PassengerRegistry для быстрого подсчёта:

```csharp
public int GetPossiblePairsCount()
{
    if (PassengerRegistry.Instance != null)
        return PassengerRegistry.Instance.GetPossiblePairsCount();

    // Fallback
    return Mathf.Min(countMaleSingles, countFemaleSingles);
}
```

---

## ScoreCounter - Подсчёт очков

**Файл:** `UI/ScoreCounter.cs`

### Основные методы

```csharp
public int GetBasePointsPerCouple()
{
    return _basePointsPerCouple; // обычно 100
}

public void AwardMatchPoints(Vector3 screenPosition, int points)
{
    _totalScore += points;
    UpdateScoreDisplay();

    // Показываем popup с очками
    ShowScorePopup(screenPosition, points);
}
```

### Интеграция с Abilities

Очки модифицируются через систему способностей:

```csharp
// В Passenger.ForceToMatchingState:
int points = _scoreCounter.GetBasePointsPerCouple();

var aSelf = GetComponent<PassengerAbilities>();
var aPartner = partner.GetComponent<PassengerAbilities>();

aSelf?.InvokeMatched(partner, ref points);   // Может модифицировать points
aPartner?.InvokeMatched(this, ref points);

_scoreCounter.AwardMatchPoints(pos, Mathf.Max(0, points));
```

---

## ManualPairingManager - Ручное создание пар

**Файл:** `Core/ManualPairingManager.cs`

### Назначение

Позволяет игроку создавать пары кликом по двум близко стоящим пассажирам.

### Обработка клика

```csharp
public bool HandleClick(Vector2 screenPosition)
{
    Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);

    // Поиск пассажиров в области клика
    Vector2 boxSize = new Vector2(_clickRadius * 2f, _clickRadius * 2f * _verticalSearchFactor);
    Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, boxSize, 0f);

    List<Passenger> clickedPassengers = new List<Passenger>();
    foreach (var hit in hits)
    {
        if (hit.TryGetComponent<Passenger>(out var p))
            clickedPassengers.Add(p);
    }

    // Попытка создать пару из найденных
    return AttemptOverlapPairing(clickedPassengers);
}
```

### Проверка совместимости

```csharp
private bool CanPair(Passenger p1, Passenger p2)
{
    if (p1 == p2) return false;
    if (p1.IsFemale == p2.IsFemale) return false; // Разный пол
    if (p1.IsInCouple || p2.IsInCouple) return false;
    if (!p1.IsMatchable || !p2.IsMatchable) return false;

    float dist = Vector2.Distance(p1.transform.position, p2.transform.position);
    return dist <= _maxPairingDistance;
}
```

---

## PassangersContainer - Контейнер пассажиров

**Файл:** `PassangersContainer.cs`

### Назначение

Хранит ссылки на всех активных пассажиров для управления.

### Методы

```csharp
public class PassangersContainer : MonoBehaviour
{
    [SerializeField] public List<Passenger> Passangers;

    // Безопасное удаление
    public void RemovePassanger(Passenger p)
    {
        if (p != null && Passangers != null && Passangers.Contains(p))
            Passangers.Remove(p);
    }

    // Очистка null-ссылок
    public void CleanupNullReferences()
    {
        Passangers?.RemoveAll(p => p == null);
    }

    // Уничтожение всех
    public void DestroyAllPassengers()
    {
        Passangers.RemoveAll(x => x == null);
        for (int i = Passangers.Count - 1; i >= 0; i--)
        {
            var p = Passangers[i];
            if (p == null) continue;
            p.container = null; // Предотвращаем рекурсию
            Destroy(p.gameObject);
        }
        Passangers.Clear();
    }
}
```

---

## ClickDirectionManager - Обработка ввода

**Файл:** `ClickDirectionManager.cs`

### Статические свойства

```csharp
public static bool IsMouseHeld { get; private set; }
public static float HorizontalAxis { get; private set; } // -1..1
public static float VerticalAxis { get; private set; }   // -1..1
public static float HorizontalVelocity { get; private set; } // скорость жеста
public static float VerticalVelocity { get; private set; }
public static bool HasReleasePoint { get; private set; }
public static Vector2 LastReleaseWorld { get; private set; }
```

### Логика обновления

```csharp
private void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        _startPosition = Input.mousePosition;
        _lastPosition = _startPosition;
        IsMouseHeld = true;
    }

    if (Input.GetMouseButton(0))
    {
        Vector2 current = Input.mousePosition;
        Vector2 delta = current - _startPosition;

        // Нормализация относительно размера экрана
        HorizontalAxis = Mathf.Clamp(delta.x / (Screen.width * 0.5f), -1f, 1f);
        VerticalAxis = Mathf.Clamp(delta.y / (Screen.height * 0.5f), -1f, 1f);

        // Скорость перетаскивания
        Vector2 frameDelta = current - _lastPosition;
        HorizontalVelocity = frameDelta.x / (Screen.width * 0.1f) / Time.deltaTime;
        VerticalVelocity = frameDelta.y / (Screen.height * 0.1f) / Time.deltaTime;

        _lastPosition = current;
    }

    if (Input.GetMouseButtonUp(0))
    {
        IsMouseHeld = false;
        LastReleaseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        HasReleasePoint = true;
    }
}
```

### Использование в TrainManager

```csharp
// Определение направления ускорения/торможения
float x = ClickDirectionManager.HorizontalAxis;
if (x > 0f)
    accelerationValue = x * _acceleration * 4f; // Разгон
else if (x < 0f)
    accelerationValue = x * _brakeDeceleration * 3f; // Торможение
```
