# 🚀 Гайд разработчика системы эффектов поля

## 📋 Обзор новой архитектуры

### Проблемы старой системы:
1. **FieldEffectManager создавался слишком поздно** - эффекты не регистрировались
2. **DontDestroyOnLoad вызывал конфликты** между сценами
3. **Отсутствие системы приоритетов** - эффекты перекрывали друг друга
4. **Жесткая связанность компонентов** - сложно добавлять новые типы
5. **Нет кэширования и оптимизации** производительности

### Новая архитектура решает:
✅ **Автоматическая инициализация** через `[RuntimeInitializeOnLoadMethod]`  
✅ **Система категорий и приоритетов** эффектов  
✅ **Базовый класс BaseFieldEffect** упрощает создание  
✅ **Пространственное кэширование** для производительности  
✅ **Композиция эффектов** - несколько эффектов одновременно  
✅ **Расширенная конфигурация** через FieldEffectData  

---

## 🏗️ Структура системы

```
FieldEffects/
├── Core/
│   ├── FieldEffectSystem.cs          # Центральная система (замена Manager)
│   ├── BaseFieldEffect.cs           # Базовый класс для всех эффектов
│   ├── FieldEffectData.cs           # Расширенные данные эффектов
│   └── FieldEffectZone.cs           # Старый базовый класс (deprecated)
├── Effects/
│   ├── GravityFieldEffectNew.cs     # Новая гравитация
│   ├── WindFieldEffect.cs           # Эффект ветра
│   ├── VortexFieldEffect.cs         # Эффект вихря
│   └── [Ваши новые эффекты]
├── Interfaces/
│   ├── IFieldEffect.cs              # Базовый интерфейс
│   └── IFieldEffectTarget.cs        # Интерфейс для целей
└── Documentation/
    └── EffectDeveloperGuide.md      # Этот файл
```

---

## 🎯 Создание нового эффекта - пошаговый гайд

### Шаг 1: Добавьте тип эффекта

В `FieldEffectData.cs` добавьте новый тип в enum:

```csharp
public enum FieldEffectType
{
    // Существующие типы...
    YourNewEffect,  // Ваш новый тип
}
```

И в методе `SetDefaultsForEffectType()`:

```csharp
case FieldEffectType.YourNewEffect:
    forceMode = ForceMode2D.Force;
    respectMass = true;
    effectColor = Color.magenta;
    break;
```

### Шаг 2: Создайте класс эффекта

```csharp
using UnityEngine;

/// <summary>
/// Описание вашего эффекта
/// </summary>
public class YourNewFieldEffect : BaseFieldEffect
{
    [Header("Настройки эффекта")]
    [SerializeField] private float _yourParameter = 1f;
    [SerializeField] private bool _yourBoolParameter = false;
    
    protected override FieldEffectData CreateDefaultEffectData()
    {
        return new FieldEffectData(FieldEffectType.YourNewEffect, 3f, 6f, transform.position);
    }
    
    protected override void InitializeEffectData()
    {
        base.InitializeEffectData();
        
        // Настройки специфичные для вашего эффекта
        _gizmoColor = Color.magenta;
        _effectData.effectColor = Color.magenta;
    }
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        if (!_isActive || target == null) return;
        
        // Проверки расстояния и возможности применения
        Vector3 targetPos = target.GetPosition();
        float distance = Vector3.Distance(transform.position, targetPos);
        
        if (distance > _effectData.radius) return;
        if (!target.CanBeAffectedBy(_effectData.effectType)) return;
        
        // Ваша логика расчета силы
        Vector3 force = CalculateYourForce(target, distance);
        target.ApplyFieldForce(force, _effectData.forceMode);
    }
    
    private Vector3 CalculateYourForce(IFieldEffectTarget target, float distance)
    {
        float strength = _effectData.GetEffectiveStrength(distance);
        // Ваша логика...
        return Vector3.zero;
    }
}
```

### Шаг 3: Добавьте в фабрику

В `BaseFieldEffect.cs` в методе `CreateEffect()`:

```csharp
return type switch
{
    FieldEffectType.Gravity => CreateEffect<GravityFieldEffectNew>(position, data),
    FieldEffectType.Wind => CreateEffect<WindFieldEffect>(position, data),
    FieldEffectType.Vortex => CreateEffect<VortexFieldEffect>(position, data),
    FieldEffectType.YourNewEffect => CreateEffect<YourNewFieldEffect>(position, data), // Добавьте эту строку
    _ => throw new System.NotImplementedException($"Тип эффекта {type} не реализован")
};
```

### Шаг 4: Создайте меню редактора (опционально)

```csharp
#if UNITY_EDITOR
[UnityEditor.MenuItem("GameObject/Field Effects/Your New Effect", false, 10)]
public static void CreateYourEffect()
{
    var effect = FieldEffectFactory.CreateEffect<YourNewFieldEffect>(Vector3.zero);
    UnityEditor.Selection.activeGameObject = effect.gameObject;
}
#endif
```

---

## 🔧 Продвинутые возможности

### Переопределение методов базового класса

```csharp
public class AdvancedEffect : BaseFieldEffect
{
    // Настройка компонентов
    protected override void InitializeComponents()
    {
        base.InitializeComponents();
        // Ваша логика инициализации
    }
    
    // Обновление каждый кадр
    protected override void OnUpdateEffect()
    {
        base.OnUpdateEffect();
        // Ваша логика обновления
    }
    
    // Удаление эффекта с цели
    public override void RemoveEffect(IFieldEffectTarget target)
    {
        // Ваша логика очистки
        base.RemoveEffect(target);
    }
    
    // Кастомная отрисовка Gizmos
    protected override void DrawEffectRadius()
    {
        base.DrawEffectRadius();
        // Ваша визуализация
    }
}
```

### Использование событий системы

```csharp
void Start()
{
    // Подписка на события системы
    FieldEffectSystem.OnEffectAppliedToTarget += OnEffectApplied;
    FieldEffectSystem.OnEffectRemovedFromTarget += OnEffectRemoved;
}

private void OnEffectApplied(IFieldEffectTarget target, IFieldEffect effect)
{
    Debug.Log($"Эффект {effect.GetEffectData().effectType} применен к {target}");
}
```

### Работа с приоритетами и композицией

```csharp
protected override void InitializeEffectData()
{
    base.InitializeEffectData();
    
    // Высокий приоритет - применяется первым
    _priority = 10;
    
    // Не складывается с другими эффектами
    _effectData.stackable = false;
    
    // Переопределяет эффекты с меньшим приоритетом
    _effectData.overrideOtherEffects = true;
}
```

---

## 📊 Типы эффектов и их назначение

### 🌪️ Эффекты движения (Movement)
- **Gravity** - притяжение к центру
- **Repulsion** - отталкивание от центра  
- **Wind** - постоянная сила в направлении
- **Magnetic** - притяжение определенных объектов
- **Vortex** - закручивающая сила

### ⚡ Модификаторы (Modifier)
- **Slowdown** - замедление движения
- **Speedup** - ускорение движения
- **Friction** - трение/сопротивление
- **Bounce** - упругость/отскок

### 🎯 Триггерные эффекты (Trigger)
- **Teleport** - мгновенное перемещение
- **Checkpoint** - точки сохранения
- **Activator** - активация других объектов

### 🛡️ Специальные эффекты
- **Shield** - защита от других эффектов
- **Multiplier** - усиление эффектов

---

## 🚀 Примеры реализации

### Простой эффект замедления

```csharp
public class SlowdownFieldEffect : BaseFieldEffect
{
    [Header("Замедление")]
    [SerializeField] private float _slowdownFactor = 0.5f;
    
    protected override FieldEffectData CreateDefaultEffectData()
    {
        return new FieldEffectData(FieldEffectType.Slowdown, 2f, 5f, transform.position);
    }
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        if (!_isActive || target == null) return;
        
        Vector3 targetPos = target.GetPosition();
        float distance = Vector3.Distance(transform.position, targetPos);
        
        if (distance > _effectData.radius) return;
        if (!target.CanBeAffectedBy(_effectData.effectType)) return;
        
        // Применяем замедление через изменение drag
        if (target is MonoBehaviour mb)
        {
            var rb = mb.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float slowStrength = _effectData.GetEffectiveStrength(distance);
                rb.drag = Mathf.Lerp(rb.drag, _slowdownFactor, slowStrength * deltaTime);
            }
        }
    }
    
    public override void RemoveEffect(IFieldEffectTarget target)
    {
        // Восстанавливаем нормальный drag
        if (target is MonoBehaviour mb)
        {
            var rb = mb.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.drag = 0f; // или исходное значение
            }
        }
    }
}
```

### Эффект телепортации

```csharp
public class TeleportFieldEffect : BaseFieldEffect
{
    [Header("Телепортация")]
    [SerializeField] private Transform _destination;
    [SerializeField] private float _cooldown = 2f;
    
    private Dictionary<IFieldEffectTarget, float> _lastTeleportTime = new();
    
    protected override FieldEffectData CreateDefaultEffectData()
    {
        var data = new FieldEffectData(FieldEffectType.Teleport, 1f, 2f, transform.position);
        data.stackable = false; // Телепортация не складывается
        return data;
    }
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        if (!_isActive || target == null || _destination == null) return;
        
        // Проверяем кулдаун
        if (_lastTeleportTime.ContainsKey(target))
        {
            if (Time.time - _lastTeleportTime[target] < _cooldown)
                return;
        }
        
        Vector3 targetPos = target.GetPosition();
        float distance = Vector3.Distance(transform.position, targetPos);
        
        if (distance > _effectData.radius) return;
        if (!target.CanBeAffectedBy(_effectData.effectType)) return;
        
        // Телепортируем
        if (target is MonoBehaviour mb)
        {
            mb.transform.position = _destination.position;
            _lastTeleportTime[target] = Time.time;
            
            // Визуальный эффект
            CreateTeleportEffect(targetPos, _destination.position);
        }
    }
    
    private void CreateTeleportEffect(Vector3 from, Vector3 to)
    {
        // Создание частиц, звука, etc.
        Debug.Log($"Телепортация с {from} в {to}");
    }
}
```

---

## 🎨 Продвинутая визуализация

### Кастомные Gizmos

```csharp
protected override void DrawEffectRadius()
{
    base.DrawEffectRadius();
    
    // Рисуем дополнительную информацию
    Gizmos.color = Color.white;
    
    // Анимированное кольцо
    float animatedRadius = _effectData.radius + Mathf.Sin(Time.time * 2f) * 0.5f;
    Gizmos.DrawWireCircle(transform.position, animatedRadius);
    
    // Лучи воздействия
    for (int i = 0; i < 8; i++)
    {
        float angle = i * 45f * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        Vector3 rayEnd = transform.position + direction * _effectData.radius;
        Gizmos.DrawLine(transform.position, rayEnd);
    }
}
```

### Информационные лейблы

```csharp
protected override void DrawEffectInfo()
{
    base.DrawEffectInfo();
    
#if UNITY_EDITOR
    if (_effectData != null)
    {
        var labelPos = transform.position + Vector3.up * (_effectData.radius + 1f);
        
        string info = $"{_effectData.effectType} Effect\n";
        info += $"Strength: {_effectData.strength:F1}\n";
        info += $"Radius: {_effectData.radius:F1}\n";
        info += $"Targets: {_currentTargets.Count}\n";
        info += $"Active: {(_isActive ? "YES" : "NO")}";
        
        UnityEditor.Handles.Label(labelPos, info);
    }
#endif
}
```

---

## ⚡ Оптимизация производительности

### Использование кэширования

```csharp
private Vector3 _cachedForce;
private float _cacheTime;
private const float CACHE_DURATION = 0.1f;

private Vector3 GetCachedForce(IFieldEffectTarget target, float distance)
{
    if (Time.time - _cacheTime > CACHE_DURATION)
    {
        _cachedForce = CalculateForce(target, distance);
        _cacheTime = Time.time;
    }
    return _cachedForce;
}
```

### Ограничение частоты обновления

```csharp
private float _lastUpdateTime;
private const float UPDATE_INTERVAL = 1f / 30f; // 30 FPS

protected override void OnUpdateEffect()
{
    if (Time.time - _lastUpdateTime < UPDATE_INTERVAL)
        return;
        
    base.OnUpdateEffect();
    // Ваша логика обновления
    _lastUpdateTime = Time.time;
}
```

### Оптимизация расстояний

```csharp
protected override bool IsTargetInRange(IFieldEffectTarget target)
{
    // Используем sqrMagnitude для избежания sqrt
    float sqrDistance = (target.GetPosition() - transform.position).sqrMagnitude;
    return sqrDistance <= _effectData.radius * _effectData.radius;
}
```

---

## 🐛 Отладка и диагностика

### Логирование эффектов

```csharp
protected override void RegisterInSystem()
{
    base.RegisterInSystem();
    
    if (Application.isPlaying)
    {
        Debug.Log($"[{GetType().Name}] Зарегистрирован в системе: {_effectData.effectType}");
    }
}

public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
{
    if (_effectData.showDebugInfo)
    {
        Debug.Log($"[{name}] Применяю эффект к {target} с силой {_effectData.strength}");
    }
    
    // Основная логика...
}
```

### Проверка состояния системы

```csharp
[ContextMenu("Debug Effect Info")]
private void DebugEffectInfo()
{
    Debug.Log($"Эффект: {_effectData.effectType}");
    Debug.Log($"Активен: {_isActive}");
    Debug.Log($"Целей в зоне: {_currentTargets.Count}");
    Debug.Log($"Система инициализирована: {FieldEffectSystem.Instance != null}");
}
```

---

## 📝 Чеклист создания эффекта

- [ ] Добавлен тип в `FieldEffectType`
- [ ] Созданы настройки по умолчанию в `SetDefaultsForEffectType()`
- [ ] Класс наследует от `BaseFieldEffect`
- [ ] Реализован `CreateDefaultEffectData()`
- [ ] Реализован `ApplyEffect()`
- [ ] Добавлена фабрика в `FieldEffectFactory`
- [ ] Создано меню редактора (опционально)
- [ ] Добавлена кастомная визуализация
- [ ] Проведено тестирование
- [ ] Написана документация

---

## 🚀 Заключение

Новая система эффектов поля предоставляет мощную и гибкую архитектуру для создания любых типов воздействий на движение объектов. Следуйте этому гайду, и вы сможете легко создавать собственные эффекты, которые будут интегрироваться в систему без конфликтов.

**Помните:** Система автоматически управляет инициализацией, регистрацией и применением эффектов - вам нужно только определить логику воздействия!

### 📞 Поддержка
Если у вас возникли вопросы или проблемы при создании эффектов, проверьте:
1. Правильность наследования от `BaseFieldEffect`
2. Корректность реализации `ApplyEffect()`
3. Инициализацию `FieldEffectSystem`
4. Логи в консоли Unity