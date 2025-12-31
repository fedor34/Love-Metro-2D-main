# Система пассажиров

## Обзор

Система пассажиров - это ядро игрового процесса Love Metro 2D. Каждый пассажир представлен классом `Passenger`, который управляет его поведением, физикой, анимациями и взаимодействиями.

## Класс Passenger

**Файл:** `Passenger.cs` (~1400 строк)

### Основные компоненты

```csharp
[RequireComponent(typeof(Rigidbody2D), typeof(PassangerAnimator), typeof(Collider2D))]
public class Passenger : MonoBehaviour, IFieldEffectTarget
```

Passenger требует наличия:
- `Rigidbody2D` - для физики движения
- `PassangerAnimator` - для управления анимациями
- `Collider2D` - для обнаружения столкновений

### Ключевые свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsFemale` | bool | Пол пассажира (true = женщина) |
| `IsMatchable` | bool | Может ли создавать пары |
| `IsInCouple` | bool | Находится ли в паре |
| `container` | PassangersContainer | Ссылка на контейнер |

### Статические свойства

```csharp
public static float GlobalSpeedMultiplier = 0.7f;
```
Глобальный множитель скорости для всех пассажиров (0.7 = 30% замедление).

## Конечный автомат состояний

### Диаграмма переходов

```
                          ┌─────────────────┐
                          │    Wandering    │
                          │  (начальное)    │
                          └────────┬────────┘
                                   │
            ┌──────────────────────┼──────────────────────┐
            │                      │                      │
            ↓                      ↓                      ↓
   ┌─────────────────┐    ┌──────────────┐    ┌───────────────────┐
   │StayingOnHandrail│    │   Falling    │    │      Flying       │
   │ (на поручне)    │    │   (полёт)    │    │ (от ветра)        │
   └────────┬────────┘    └──────┬───────┘    └─────────┬─────────┘
            │                    │                      │
            │    ┌───────────────┼───────────────┐      │
            │    │               │               │      │
            ↓    ↓               ↓               ↓      ↓
   ┌─────────────────┐    ┌──────────────┐    ┌───────────────────┐
   │    Wandering    │    │   Matching   │    │  BeingAbsorbed    │
   │  (возврат)      │    │   (в паре)   │    │ (поглощение)      │
   └─────────────────┘    └──────────────┘    └───────────────────┘
```

### Состояние Wandering (Блуждание)

**Назначение:** Начальное состояние, пассажир стоит и ждёт импульса.

**Поведение:**
- Rigidbody в режиме Dynamic
- Скорость = 0 (статичен до толчка)
- Реагирует на столкновения со стенами (отражение направления)
- Может схватиться за поручень при триггере

**Переход в Falling:**
```csharp
public override void OnTrainSpeedChange(Vector2 force)
{
    // Рассчитываем импульс на основе позиции клика
    Vector2 delta = CalculateImpulseFromClick(force);

    if (delta.magnitude >= Passanger._minImpulseToLaunch)
    {
        Passanger.ChangeState(Passanger.fallingState);
        ((Falling)Passanger.fallingState).SetInitialFallingSpeed(startV);
    }
}
```

**Переход в StayingOnHandrail:**
```csharp
public override void OnTriggerEnter(Collider2D collision)
{
    if (collision.TryGetComponent<HandRailPosition>(out HandRailPosition handrail)
        && Passanger._timeWithoutHolding > Passanger._handrailCooldown
        && handrail.IsOccupied == false
        && Random.Range(0f, 1f) <= Passanger._grabingHandrailChance)
    {
        // Схватился за поручень
        Passanger.ChangeState(Passanger.stayingOnHandrailState);
    }
}
```

### Состояние Falling (Падение/Полёт)

**Назначение:** Основное игровое состояние - пассажир летит после толчка.

**Физическая модель:**

1. **Нелинейное затухание (ease-out):**
```csharp
float speed = currentFallingSpeed.magnitude;
float t01 = Mathf.InverseLerp(0f, _maxFlightSpeed, speed);
float k = Mathf.Lerp(_easeOutMinK, _easeOutMaxK, t01);
currentFallingSpeed *= Mathf.Pow(k, 60f * Time.deltaTime);
```

2. **Магнитное притяжение к противоположному полу:**
```csharp
Passenger target = FindClosestOpposite(Passanger, _magnetRadius);
if (target != null)
{
    Vector2 to = target.transform.position - Passanger.transform.position;
    float w = Mathf.InverseLerp(_magnetRadius, 0f, to.magnitude);
    Vector2 accel = to.normalized * (_magnetForce * w) * Time.deltaTime;
    // Проецируем только вдоль текущего направления
    currentFallingSpeed += forward * Vector2.Dot(accel, forward);
}
```

3. **Отталкивание от своего пола:**
```csharp
var sameGender = Passanger.IsFemale
    ? PassengerRegistry.Instance.Females
    : PassengerRegistry.Instance.Males;

foreach (var other in sameGender)
{
    Vector2 toOther = other.transform.position - Passanger.transform.position;
    float d = toOther.magnitude;
    if (d < _repelRadius)
    {
        float w = Mathf.InverseLerp(_repelRadius, 0f, d);
        currentFallingSpeed -= toOther.normalized * (_repelForce * w) * Time.deltaTime;
    }
}
```

**Обработка столкновений:**

```csharp
public override void OnCollision(Collision2D collision)
{
    // Столкновение с другим пассажиром
    if (collision.transform.TryGetComponent<Passenger>(out Passenger other))
    {
        // Проверка на создание пары
        if (other.IsFemale != Passanger.IsFemale
            && !other.IsInCouple && !Passanger.IsInCouple
            && other.IsMatchable && Passanger.IsMatchable)
        {
            Passanger.ForceToMatchingState(other);
            other.ForceToMatchingState(Passanger);
            return;
        }

        // Иначе - рикошет как бильярдные шары
        currentFallingSpeed = Vector2.Reflect(vA, normal) * _bounceElasticity;
    }
    else
    {
        // Столкновение со стеной - отскок
        currentFallingSpeed = Vector2.Reflect(currentFallingSpeed, normal) * _bounceElasticity;
        _bounceCount++;

        if (_bounceCount >= _maxBounces)
        {
            Passanger.ChangeState(Passanger.wanderingState);
        }
    }
}
```

**Выход из состояния:**
- При низкой скорости (`< _minFallingSpeed`) → Wandering
- При превышении лимита отскоков (`_maxBounces`) → Wandering
- При создании пары → Matching

### Состояние StayingOnHandrail (На поручне)

**Назначение:** Пассажир держится за поручень определённое время.

**Поведение:**
- Rigidbody переключается в Static
- Запускается анимация удержания
- Таймер отсчитывает случайное время

```csharp
public override void Enter()
{
    resetTimer();
    Passanger._rigidbody.bodyType = RigidbodyType2D.Static;
    Passanger.PassangerAnimator.SetHoldingState(true);
    Passanger._timeWithoutHolding = 0;
}

private void resetTimer()
{
    _expiredTime = 0;
    _stayingTime = Random.Range(
        Passanger.HandrailStandingTimeInterval.x,
        Passanger.HandrailStandingTimeInterval.y
    );
}
```

**Выход:** По истечении таймера → Wandering

### Состояние Flying (Полёт от ветра)

**Назначение:** Пассажир летит под воздействием сильного ветра.

**Отличие от Falling:**
- Скорость задаётся внешним источником (ветром)
- Есть лимит времени полёта
- При столкновении переходит в Falling

```csharp
public void SetFlyingParameters(Vector2 windVelocity, float windStrength)
{
    _flyingVelocity = windVelocity * (_flightSpeedMultiplier * _globalImpulseScale);
    _windStrength = windStrength;
}

public override void UpdateState()
{
    _flyingTime += Time.deltaTime;

    // Замедление
    if (_flyingVelocity.sqrMagnitude > 0.0001f)
    {
        float speed = Mathf.Max(0f, _flyingVelocity.magnitude - _flightDeceleration * Time.deltaTime);
        _flyingVelocity = _flyingVelocity.normalized * speed;
    }

    // Выход при ослаблении ветра или таймауте
    if (_windStrength < MIN_WIND_STRENGTH_FOR_FLYING || _flyingTime > MAX_FLYING_TIME)
    {
        Passanger.ChangeState(Passanger.fallingState);
        ((Falling)Passanger.fallingState).SetInitialFallingSpeed(_flyingVelocity);
    }
}
```

### Состояние Matching (В паре)

**Назначение:** Пассажир вошёл в пару и неактивен.

**Поведение:**
- Rigidbody = Static
- Коллайдер отключён
- Не реагирует на импульсы

```csharp
public override void Enter()
{
    Passanger.IsMatchable = false;
    Passanger._rigidbody.bodyType = RigidbodyType2D.Static;
    Passanger.PassangerAnimator.ActivateBumping(); // Анимация "bumping"
    Passanger._collider.enabled = false;
}
```

**Выход:** Только при разбивании пары внешним столкновением (через `BreakFromCouple`)

### Состояние BeingAbsorbed (Поглощение)

**Назначение:** Пассажир затягивается в чёрную дыру.

**Поведение:**
```csharp
public override void UpdateState()
{
    _timeInAbsorption += Time.deltaTime;

    Vector3 direction = (_absorptionCenter - Passanger.transform.position).normalized;
    float distance = Vector3.Distance(Passanger.transform.position, _absorptionCenter);

    if (distance < 0.5f || _timeInAbsorption > _maxAbsorptionTime)
    {
        // Уничтожение пассажира
        Passanger.RemoveFromContainerAndDestroy();
        return;
    }

    // Сила увеличивается при приближении
    Vector2 force = direction * _absorptionForce / Mathf.Max(distance, 0.1f);
    Passanger._rigidbody.AddForce(force, ForceMode2D.Force);
}
```

## Параметры полёта

### Импульсы (Impulse Tuning)

| Параметр | Значение | Описание |
|----------|----------|----------|
| `_launchSensitivity` | 1.0 | Множитель силы инерции поезда |
| `_minImpulseToLaunch` | 3.0 | Минимальный импульс для старта полёта |
| `_impulseToVelocityScale` | 0.45 | Конвертация импульса в скорость |
| `_globalImpulseScale` | 0.8 | Дополнительное замедление импульсов |

### Параметры полёта (Billiards-style)

| Параметр | Значение | Описание |
|----------|----------|----------|
| `_magnetRadius` | 3.5 | Радиус притяжения к противоположному полу |
| `_magnetForce` | 5.0 | Сила притяжения |
| `_repelRadius` | 2.0 | Радиус отталкивания от своего пола |
| `_repelForce` | 4.0 | Сила отталкивания |
| `_maxFlightSpeed` | 18 | Максимальная скорость полёта |
| `_flightDeceleration` | 0.65 | Замедление полёта |
| `_bounceElasticity` | 0.95 | Упругость при отскоке |
| `_maxBounces` | 3 | Максимум отскоков до остановки |

### Затухание (Ease-out)

| Параметр | Значение | Описание |
|----------|----------|----------|
| `_easeOutMinK` | 0.985 | Коэф. затухания при низкой скорости |
| `_easeOutMaxK` | 0.9985 | Коэф. затухания при высокой скорости |

Формула затухания:
```
speedNew = speedOld * pow(k, 60 * deltaTime)
```

При высокой скорости k ближе к 1 (медленнее затухает), при низкой - быстрее останавливается.

### Aim Assist

| Параметр | Значение | Описание |
|----------|----------|----------|
| `_aimAssistRadius` | 5.0 | Радиус поиска цели |
| `_aimAssistMaxStrength` | 1.2 | Макс. корректировка в сторону цели |
| `_turbulenceStrength` | 0.8 | Случайный шум для вариативности |
| `_angleSnapDeg` | 10 | Привязка угла |

## PassengerRegistry

**Файл:** `Core/PassengerRegistry.cs`

Оптимизированный реестр для быстрого поиска пассажиров.

### Зачем нужен

Без Registry каждый пассажир в Update вызывал бы:
```csharp
FindObjectsOfType<Passenger>() // O(n) на каждого = O(n²) total
```

С Registry:
```csharp
PassengerRegistry.Instance.Males // O(1) доступ к списку
```

### Структура данных

```csharp
private readonly List<Passenger> _allPassengers = new List<Passenger>();
private readonly List<Passenger> _males = new List<Passenger>();
private readonly List<Passenger> _females = new List<Passenger>();
private readonly List<Passenger> _singles = new List<Passenger>(); // не в паре
```

### Регистрация

Пассажир регистрируется в `Start()`:
```csharp
private void Start()
{
    if (PassengerRegistry.Instance != null)
        PassengerRegistry.Instance.Register(this);
}
```

И отменяет регистрацию в `OnDestroy()`:
```csharp
private void OnDestroy()
{
    if (PassengerRegistry.Instance != null)
        PassengerRegistry.Instance.Unregister(this);
}
```

### Оптимизированные методы

```csharp
// Поиск ближайшего противоположного пола - O(n) вместо FindObjectsOfType
public Passenger FindClosestOpposite(Passenger self, float radius)

// Получение всех своего пола в радиусе - для отталкивания
public void GetSameGenderInRadius(Passenger self, float radius, List<Passenger> results)

// Проверка возможных пар
public int GetPossiblePairsCount()
```

### Автоочистка

Каждые 2 секунды удаляются null-ссылки на уничтоженных пассажиров:
```csharp
private void Update()
{
    if (Time.time >= _nextCleanupTime)
    {
        _nextCleanupTime = Time.time + _cleanupInterval;
        CleanupNullReferences();
    }
}
```

## PassengerSettings (ScriptableObject)

**Файл:** `Passenger/PassengerSettings.cs`

Позволяет хранить все параметры пассажира в одном ассете.

### Создание

`Assets → Create → Love Metro → Passenger Settings`

### Использование

1. Создать ассет настроек
2. Настроить значения в инспекторе
3. Перетащить ассет в поле `Settings` на префабе пассажира

```csharp
[Header("Настройки (опционально)")]
[SerializeField] private PassengerSettings _settings;

private void ApplySettingsFromAsset()
{
    if (_settings == null) return;

    GlobalSpeedMultiplier = _settings.globalSpeedMultiplier;
    _speed = _settings.baseSpeed;
    // ... и остальные параметры
}
```

### Обратная совместимость

Если `_settings` не задан, используются значения из сериализованных полей на префабе (старое поведение).

## Анимации (PassangerAnimator)

**Файл:** `PassangerAnimator.cs`

Управляет анимациями пассажира.

### Методы

| Метод | Описание |
|-------|----------|
| `ChangeFacingDirection(bool right)` | Поворот спрайта влево/вправо |
| `SetHoldingState(bool holding)` | Анимация удержания поручня |
| `SetFallingState(bool falling)` | Анимация падения/полёта |
| `ActivateBumping()` | Анимация "bumping" при создании пары |
| `ForceWalkingState(bool walking)` | Принудительно вкл/выкл ходьбу |
| `EnableAutomaticWalkingAnimation()` | Авто-переключение ходьбы по скорости |
| `ResetAfterPairBreak()` | Сброс после разрыва пары |

## Способности (Abilities)

### PassengerAbilities

Компонент для хранения и выполнения способностей:

```csharp
public class PassengerAbilities : MonoBehaviour
{
    private List<PassengerAbility> _abilities = new();

    public void AddAbility(PassengerAbility ability);
    public void AttachAll();
    public void InvokeMatched(Passenger partner, ref int points);
    public void InvokePairBroken(Passenger formerPartner);
}
```

### VipAbility

Удваивает очки при создании пары:

```csharp
public class VipAbility : PassengerAbility
{
    public override void OnMatched(Passenger self, Passenger partner, ref int points)
    {
        points *= 2;
    }
}
```

Назначается случайной паре при спавне через `PassangerSpawner.TryAssignVipPair()`.
