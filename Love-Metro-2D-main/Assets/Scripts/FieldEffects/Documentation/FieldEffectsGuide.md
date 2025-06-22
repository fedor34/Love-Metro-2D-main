# Система эффектов поля - Руководство пользователя

## 📋 Обзор

Система эффектов поля позволяет создавать зоны на игровом поле, которые влияют на движение пассажиров. Первый реализованный эффект - **Гравитация**, который притягивает пассажиров к центру зоны.

## 🏗️ Архитектура системы

### Основные компоненты:
- **IFieldEffect** - базовый интерфейс для всех эффектов
- **IFieldEffectTarget** - интерфейс для объектов, подверженных эффектам
- **FieldEffectZone** - базовый класс для зон эффектов
- **FieldEffectManager** - синглтон для управления всеми эффектами
- **FieldEffectData** - данные эффекта (сила, радиус, тип)

### Структура папок:
```
FieldEffects/
├── Interfaces/
│   ├── IFieldEffect.cs
│   └── IFieldEffectTarget.cs
├── Core/
│   ├── FieldEffectManager.cs
│   ├── FieldEffectZone.cs
│   └── FieldEffectData.cs
├── Effects/
│   ├── GravityFieldEffect.cs
│   └── GravityFieldEffectPrefabCreator.cs
└── Documentation/
    └── FieldEffectsGuide.md
```

## 🎯 Эффект гравитации

### Как создать эффект гравитации:

1. **Через меню Unity:**
   - GameObject → Field Effects → Gravity Field Effect
   - Или GameObject → Field Effects → Gravity Field Effect With Particles

2. **Программно:**
```csharp
GameObject gravityObj = new GameObject("Gravity Effect");
GravityFieldEffect gravity = gravityObj.AddComponent<GravityFieldEffect>();
```

### Настройки эффекта гравитации:

#### Основные параметры (FieldEffectData):
- **Effect Type** - тип эффекта (Gravity)
- **Strength** - сила притяжения (1-10)
- **Radius** - радиус действия (в Unity units)
- **Max Force** - максимальная применимая сила

#### Настройки применения:
- **Affects Falling** - влияет на падающих пассажиров
- **Affects Wandering** - влияет на блуждающих пассажиров
- **Affects Handrail** - влияет на держащихся за поручни
- **Affects Matching** - влияет на пары

#### Дополнительные настройки гравитации:
- **Use Square Distance Falloff** - использовать закон обратных квадратов
- **Minimal Force** - минимальная применимая сила
- **Affect Only Falling** - воздействовать только на падающих

## 🔧 Интеграция с WandererNew

Класс `WandererNew` теперь реализует интерфейс `IFieldEffectTarget`:

### Ключевые методы:
- `ApplyFieldForce(Vector2 force, FieldEffectType effectType)` - применение силы
- `CanBeAffectedBy(FieldEffectType effectType)` - проверка возможности воздействия
- `OnEnterFieldEffect(IFieldEffect effect)` - вход в зону эффекта
- `OnExitFieldEffect(IFieldEffect effect)` - выход из зоны эффекта

### Поведение в разных состояниях:
- **Wandering**: эффект изменяет направление движения
- **Falling**: эффект применяется к Rigidbody2D напрямую
- **StayingOnHandrail**: эффект не применяется (по умолчанию)
- **Matching**: эффект не применяется к парам

## 🎮 Использование в игре

### Размещение эффектов:
1. Добавьте эффект гравитации через меню
2. Настройте параметры в Inspector
3. Разместите в нужном месте на сцене
4. Эффект автоматически зарегистрируется в FieldEffectManager

### Визуальная отладка:
- **Show Debug Gizmos** - показывать зоны в Scene View
- Желтый круг - зона действия
- Красный центр - точка притяжения
- Голубые лучи - направления сил

### Управление эффектами:
```csharp
// Получить менеджер эффектов
FieldEffectManager manager = FieldEffectManager.Instance;

// Отключить все эффекты гравитации
manager.SetEffectsActiveByType(FieldEffectType.Gravity, false);

// Получить количество активных эффектов
int activeCount = manager.GetActiveEffectsCount();
```

## 🚀 Расширение системы

### Создание нового эффекта:
1. Наследуйтесь от `FieldEffectZone`
2. Реализуйте методы `ApplyEffect()` и `RemoveEffect()`
3. Добавьте новый тип в `FieldEffectType`
4. Обновите логику в `WandererNew.CanBeAffectedBy()`

### Пример нового эффекта:
```csharp
public class WindFieldEffect : FieldEffectZone
{
    [SerializeField] private Vector2 _windDirection = Vector2.right;
    
    public override void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        Vector2 windForce = _windDirection.normalized * _effectData.strength;
        target.ApplyFieldForce(windForce, FieldEffectType.Wind);
    }
    
    public override void RemoveEffect(IFieldEffectTarget target)
    {
        // Ветер не оставляет постоянных эффектов
    }
}
```

## ⚙️ Настройки производительности

### Оптимизация:
- Используйте разумные радиусы эффектов
- Не размещайте слишком много эффектов близко друг к другу
- Отключайте неиспользуемые эффекты через `SetActive(false)`

### Debugging:
- Включите Debug Mode в FieldEffectManager
- Используйте Show Debug Gizmos для визуализации
- Следите за сообщениями в Console

## 📝 Примеры использования

### Простая гравитационная яма:
```csharp
var gravity = CreateGravityEffect();
gravity.SetGravityStrength(8f);
gravity.SetGravityRadius(4f);
gravity.ToggleSquareDistanceFalloff(true);
```

### Слабое притяжение к поручням:
```csharp
var magneticHandrail = CreateGravityEffect();
magneticHandrail.SetGravityStrength(2f);
magneticHandrail.SetGravityRadius(1.5f);
magneticHandrail._effectData.affectsWandering = true;
magneticHandrail._effectData.affectsHandrail = false;
```

---

*Система эффектов поля готова к расширению новыми типами эффектов!* 