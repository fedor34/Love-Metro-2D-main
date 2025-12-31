# Система эффектов поля (Field Effects)

## Обзор

Система эффектов поля позволяет создавать области на игровом поле, которые воздействуют на пассажиров различными силами: гравитация, ветер, вихри и т.д.

## Архитектура

```
┌────────────────────┐
│ FieldEffectSystem  │ ← Синглтон-менеджер
└─────────┬──────────┘
          │ управляет
          ↓
┌────────────────────┐
│   IFieldEffect     │ ← Интерфейс эффекта
├────────────────────┤
│ GravityFieldEffect │
│ WindFieldEffect    │
│ VortexFieldEffect  │
└─────────┬──────────┘
          │ воздействует на
          ↓
┌────────────────────┐
│ IFieldEffectTarget │ ← Интерфейс цели
├────────────────────┤
│     Passenger      │
└────────────────────┘
```

## FieldEffectSystem - Менеджер эффектов

**Файл:** `FieldEffects/FieldEffectSystem.cs`

### Singleton

```csharp
public class FieldEffectSystem : MonoBehaviour
{
    public static FieldEffectSystem Instance { get; private set; }

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

### Хранение данных

```csharp
private List<IFieldEffect> _activeEffects = new List<IFieldEffect>();
private List<IFieldEffectTarget> _targets = new List<IFieldEffectTarget>();
```

### Регистрация

```csharp
public void RegisterEffect(IFieldEffect effect)
{
    if (!_activeEffects.Contains(effect))
        _activeEffects.Add(effect);
}

public void UnregisterEffect(IFieldEffect effect)
{
    _activeEffects.Remove(effect);
}

public void RegisterTarget(IFieldEffectTarget target)
{
    if (!_targets.Contains(target))
        _targets.Add(target);
}

public void UnregisterTarget(IFieldEffectTarget target)
{
    _targets.Remove(target);
}
```

### Цикл обновления

```csharp
private void FixedUpdate()
{
    // Очистка null-ссылок
    _activeEffects.RemoveAll(e => e == null);
    _targets.RemoveAll(t => t == null);

    foreach (var effect in _activeEffects)
    {
        if (!effect.IsActive) continue;

        var data = effect.GetEffectData();

        foreach (var target in _targets)
        {
            if (!target.CanBeAffectedBy(data.effectType)) continue;

            float distance = Vector3.Distance(target.GetPosition(), data.center);
            if (distance > data.radius) continue;

            // Рассчитываем и применяем силу
            Vector2 force = effect.CalculateForce(target.GetPosition());
            target.ApplyFieldForce(force, data.effectType);

            // Уведомляем о входе/выходе из области
            target.OnEnterFieldEffect(effect);
        }
    }
}
```

### Получение эффектов по типу

```csharp
public List<IFieldEffect> GetEffectsByType(FieldEffectType type)
{
    return _activeEffects.Where(e => e.GetEffectData().effectType == type).ToList();
}
```

---

## IFieldEffect - Интерфейс эффекта

**Файл:** `FieldEffects/IFieldEffect.cs`

```csharp
public interface IFieldEffect
{
    bool IsActive { get; }
    FieldEffectData GetEffectData();
    Vector2 CalculateForce(Vector3 targetPosition);
}
```

### FieldEffectData

```csharp
public struct FieldEffectData
{
    public FieldEffectType effectType;
    public Vector3 center;
    public float radius;
    public float strength;
    public Vector2 direction; // для направленных эффектов
}
```

### FieldEffectType

```csharp
public enum FieldEffectType
{
    Gravity,    // Притяжение к центру
    Repulsion,  // Отталкивание от центра
    Wind,       // Направленный ветер
    Vortex,     // Вихрь (закручивание)
    Magnetic,   // Магнитное поле
    Slowdown,   // Замедление
    Speedup,    // Ускорение
    Friction    // Трение
}
```

---

## IFieldEffectTarget - Интерфейс цели

**Файл:** `FieldEffects/IFieldEffectTarget.cs`

```csharp
public interface IFieldEffectTarget
{
    Vector3 GetPosition();
    Rigidbody2D GetRigidbody();
    bool CanBeAffectedBy(FieldEffectType effectType);
    void ApplyFieldForce(Vector2 force, FieldEffectType effectType);
    void OnEnterFieldEffect(IFieldEffect effect);
    void OnExitFieldEffect(IFieldEffect effect);
}
```

### Реализация в Passenger

```csharp
public class Passenger : MonoBehaviour, IFieldEffectTarget
{
    public Vector3 GetPosition() => transform.position;
    public Rigidbody2D GetRigidbody() => _rigidbody;

    public bool CanBeAffectedBy(FieldEffectType effectType)
    {
        if (!_isInitiated || IsInCouple) return false;
        if (_currentState is BeingAbsorbed) return false;

        switch (effectType)
        {
            case FieldEffectType.Gravity:
            case FieldEffectType.Repulsion:
            case FieldEffectType.Wind:
            case FieldEffectType.Vortex:
            case FieldEffectType.Magnetic:
                return _currentState is Wandering
                    || _currentState is Falling
                    || _currentState is Flying;

            case FieldEffectType.Slowdown:
            case FieldEffectType.Speedup:
            case FieldEffectType.Friction:
                return _currentState is Wandering;

            default:
                return true;
        }
    }

    public void ApplyFieldForce(Vector2 force, FieldEffectType effectType)
    {
        // Специальная обработка ветра
        if (effectType == FieldEffectType.Wind)
        {
            float windStrength = force.magnitude;

            // Сильный ветер переводит в полёт
            if (windStrength >= 8f && !(_currentState is Flying))
            {
                ChangeState(flyingState);
                ((Flying)flyingState).SetFlyingParameters(force, windStrength);
                return;
            }

            // Слабый ветер просто толкает
            if (_currentState is Wandering)
            {
                _rigidbody.AddForce(force * 0.5f, ForceMode2D.Force);
            }
        }
        else
        {
            // Обычные эффекты
            _rigidbody.AddForce(force, ForceMode2D.Force);
        }
    }
}
```

---

## GravityFieldEffectNew - Гравитация/Чёрная дыра

**Файл:** `FieldEffects/GravityFieldEffectNew.cs`

### Параметры

```csharp
[Header("Gravity Settings")]
[SerializeField] private float _radius = 5f;
[SerializeField] private float _strength = 10f;
[SerializeField] private bool _isRepulsive = false; // Отталкивание вместо притяжения

[Header("Black Hole Mode")]
public bool _createBlackHoleEffect = false;
public float _eventHorizonRadius = 1f; // Радиус поглощения
```

### Расчёт силы

```csharp
public Vector2 CalculateForce(Vector3 targetPosition)
{
    Vector2 direction = (Vector2)(transform.position - targetPosition);
    float distance = direction.magnitude;

    if (distance < 0.01f || distance > _radius)
        return Vector2.zero;

    // Сила увеличивается при приближении (обратно пропорциональна расстоянию)
    float normalizedDistance = distance / _radius;
    float forceMagnitude = _strength * (1f - normalizedDistance);

    // Режим чёрной дыры: ещё сильнее вблизи
    if (_createBlackHoleEffect && distance < _eventHorizonRadius * 2f)
    {
        forceMagnitude *= 3f;
    }

    Vector2 force = direction.normalized * forceMagnitude;

    // Инвертируем для отталкивания
    if (_isRepulsive)
        force = -force;

    return force;
}
```

### Поглощение

Когда пассажир входит в `_eventHorizonRadius`:

```csharp
// В Passenger.OnEnterFieldEffect:
if (effectData.effectType == FieldEffectType.Gravity
    && effect is GravityFieldEffectNew gravity
    && gravity._createBlackHoleEffect)
{
    float dist = Vector3.Distance(transform.position, effectData.center);
    if (dist <= gravity._eventHorizonRadius)
    {
        ForceToAbsorptionState(effectData.center, effectData.strength);
    }
}
```

---

## WindFieldEffect - Ветер

**Файл:** `FieldEffects/WindFieldEffect.cs`

### Параметры

```csharp
[Header("Wind Settings")]
[SerializeField] private Vector2 _direction = Vector2.right;
[SerializeField] private float _strength = 10f;
[SerializeField] private float _radius = 5f;

[Header("Variation")]
[SerializeField] private float _gustFrequency = 1f;  // Частота порывов
[SerializeField] private float _gustStrength = 0.3f; // Сила порывов (0-1)
```

### Расчёт силы

```csharp
public Vector2 CalculateForce(Vector3 targetPosition)
{
    float distance = Vector3.Distance(transform.position, targetPosition);
    if (distance > _radius) return Vector2.zero;

    // Сила уменьшается к краям области
    float falloff = 1f - (distance / _radius);

    // Добавляем порывы
    float gust = 1f + Mathf.Sin(Time.time * _gustFrequency * Mathf.PI * 2f) * _gustStrength;

    return _direction.normalized * _strength * falloff * gust;
}
```

### Переход в Flying

Если сила ветра >= 8, пассажир переходит в состояние Flying:

```csharp
// В Passenger.ApplyFieldForce:
if (effectType == FieldEffectType.Wind && windStrength >= 8f)
{
    ChangeState(flyingState);
    ((Flying)flyingState).SetFlyingParameters(force, windStrength);
}
```

---

## VortexFieldEffect - Вихрь

**Файл:** `FieldEffects/VortexFieldEffect.cs`

### Параметры

```csharp
[Header("Vortex Settings")]
[SerializeField] private float _radius = 5f;
[SerializeField] private float _tangentialStrength = 10f; // Закручивание
[SerializeField] private float _radialStrength = 3f;      // Притяжение к центру
[SerializeField] private bool _clockwise = true;
```

### Расчёт силы

```csharp
public Vector2 CalculateForce(Vector3 targetPosition)
{
    Vector2 toCenter = (Vector2)(transform.position - targetPosition);
    float distance = toCenter.magnitude;

    if (distance < 0.01f || distance > _radius)
        return Vector2.zero;

    float falloff = 1f - (distance / _radius);

    // Тангенциальная сила (перпендикулярно к центру)
    Vector2 tangent = _clockwise
        ? new Vector2(toCenter.y, -toCenter.x)
        : new Vector2(-toCenter.y, toCenter.x);
    tangent = tangent.normalized;

    // Радиальная сила (к центру)
    Vector2 radial = toCenter.normalized;

    Vector2 force = tangent * _tangentialStrength + radial * _radialStrength;
    return force * falloff;
}
```

---

## Создание нового эффекта

### Шаг 1: Создать класс

```csharp
public class MyCustomEffect : MonoBehaviour, IFieldEffect
{
    [SerializeField] private float _radius = 5f;
    [SerializeField] private float _strength = 10f;

    public bool IsActive => gameObject.activeInHierarchy;

    private void OnEnable()
    {
        FieldEffectSystem.Instance?.RegisterEffect(this);
    }

    private void OnDisable()
    {
        FieldEffectSystem.Instance?.UnregisterEffect(this);
    }

    public FieldEffectData GetEffectData()
    {
        return new FieldEffectData
        {
            effectType = FieldEffectType.Custom, // Добавить в enum
            center = transform.position,
            radius = _radius,
            strength = _strength
        };
    }

    public Vector2 CalculateForce(Vector3 targetPosition)
    {
        // Ваша логика расчёта силы
        return Vector2.zero;
    }
}
```

### Шаг 2: Добавить тип в enum

```csharp
// В FieldEffectType:
public enum FieldEffectType
{
    // ... существующие
    Custom
}
```

### Шаг 3: Обработать в Passenger (опционально)

```csharp
// В Passenger.CanBeAffectedBy:
case FieldEffectType.Custom:
    return _currentState is Wandering;

// В Passenger.ApplyFieldForce:
if (effectType == FieldEffectType.Custom)
{
    // Специальная обработка
}
```

---

## Визуализация эффектов

### Gizmos в редакторе

```csharp
private void OnDrawGizmos()
{
    // Радиус действия
    Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
    Gizmos.DrawWireSphere(transform.position, _radius);

    // Для чёрной дыры - горизонт событий
    if (_createBlackHoleEffect)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _eventHorizonRadius);
    }

    // Направление для ветра
    Gizmos.color = Color.cyan;
    Gizmos.DrawRay(transform.position, _direction.normalized * 2f);
}
```

### Runtime визуализация

Можно добавить LineRenderer или ParticleSystem для отображения эффекта во время игры.

---

## Пример настройки сцены

### Чёрная дыра

1. Создать пустой GameObject
2. Добавить `GravityFieldEffectNew`
3. Настроить:
   - `_radius = 8`
   - `_strength = 15`
   - `_createBlackHoleEffect = true`
   - `_eventHorizonRadius = 1.5`

### Ветряной коридор

1. Создать пустой GameObject (растянуть по ширине коридора)
2. Добавить `WindFieldEffect`
3. Настроить:
   - `_direction = (1, 0)` - вправо
   - `_strength = 12`
   - `_radius = 3`
   - `_gustFrequency = 2`

### Вихрь

1. Создать пустой GameObject
2. Добавить `VortexFieldEffect`
3. Настроить:
   - `_radius = 6`
   - `_tangentialStrength = 8`
   - `_radialStrength = 2`
   - `_clockwise = true`
