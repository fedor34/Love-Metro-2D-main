# Руководство по настройке

## PassengerSettings - Централизованные настройки пассажиров

**Файл:** `Passenger/PassengerSettings.cs`

### Создание ассета настроек

1. В Unity: `Assets → Create → Love Metro → Passenger Settings`
2. Назовите файл (например, `DefaultPassengerSettings`)
3. Настройте параметры в инспекторе
4. Перетащите ассет в поле `Settings` на префабах пассажиров

### Категории настроек

#### Базовое движение

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `globalSpeedMultiplier` | 0.7 | Глобальный множитель скорости (0.7 = 30% замедление) |
| `baseSpeed` | 2.0 | Базовая скорость ходьбы |
| `minFallingSpeed` | 0.5 | Минимальная скорость для остановки полёта |

#### Поручни

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `handrailGrabChance` | 0.3 | Шанс схватиться за поручень (0-1) |
| `handrailMinGrabbingSpeed` | 1.0 | Мин. скорость для схватывания |
| `handrailCooldown` | 0.5 | Кулдаун между схватываниями (сек) |
| `handrailStandingTimeInterval` | (1, 3) | Время удержания (min, max) |

#### Импульс от поезда

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `launchSensitivity` | 1.0 | Чувствительность к инерции поезда |
| `minImpulseToLaunch` | 3.0 | Мин. импульс для старта полёта |
| `impulseToVelocityScale` | 0.45 | Конвертация импульса в скорость |
| `globalImpulseScale` | 0.8 | Доп. множитель импульсов |

#### Полёт

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `maxFlightSpeed` | 18 | Максимальная скорость полёта |
| `flightSpeedMultiplier` | 0.7 | Множитель скорости полёта |
| `flightDeceleration` | 0.65 | Замедление полёта |
| `maxBounces` | 3 | Макс. количество отскоков |
| `bounceElasticity` | 0.95 | Упругость при отскоке (0-1) |
| `wallBounceBoost` | 1.0 | Ускорение при ударе о стену |

#### Магнитное притяжение

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `magnetRadius` | 3.5 | Радиус притяжения к противоположному полу |
| `magnetForce` | 5.0 | Сила притяжения |
| `repelRadius` | 2.0 | Радиус отталкивания от своего пола |
| `repelForce` | 4.0 | Сила отталкивания |

---

## TrainManager - Настройки поезда

### Параметры движения

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_maxSpeed` | 480 | Максимальная скорость |
| `_minSpeed` | 1 | Минимальная скорость |
| `_acceleration` | 180 | Базовое ускорение |
| `_deceleration` | 10 | Естественное замедление |
| `_brakeDeceleration` | 25 | Торможение при свайпе влево |

### Импульсы направления

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_dirImpulseMin` | 6 | Минимальная сила импульса |
| `_dirImpulseScale` | 35 | Множитель от скорости жеста |
| `_dirImpulseCooldown` | 0.15 | Кулдаун между импульсами (сек) |
| `_dirFlickThreshold` | 0.95 | Порог скорости для флика |

### Симуляция поворотов

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_turnAmplitudeDeg` | 45 | Амплитуда "поворотов" (градусы) |
| `_turnSpeed` | 0.6 | Скорость изменения угла |

---

## PassangerSpawner - Настройки спавна

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_maxPassengersInScene` | 20 | Максимум пассажиров в сцене |
| `_vipPairChance` | 1.0 | Шанс назначения VIP паре (0-1) |

### Настройка префабов

1. Создайте префабы пассажиров с компонентом `Passenger`
2. Добавьте их в списки `_passangerFemalePrefs` и `_passangerMalePrefs`
3. Настройте точки спавна в `_spawnLocations`

---

## FieldEffects - Настройки эффектов поля

### GravityFieldEffectNew

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_radius` | 5 | Радиус действия |
| `_strength` | 10 | Сила притяжения |
| `_isRepulsive` | false | Отталкивание вместо притяжения |
| `_createBlackHoleEffect` | false | Режим чёрной дыры |
| `_eventHorizonRadius` | 1 | Радиус поглощения (для чёрной дыры) |

### WindFieldEffect

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_direction` | (1, 0) | Направление ветра |
| `_strength` | 10 | Сила ветра |
| `_radius` | 5 | Радиус действия |
| `_gustFrequency` | 1 | Частота порывов |
| `_gustStrength` | 0.3 | Сила порывов (0-1) |

---

## ManualPairingManager - Ручное создание пар

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_maxPairingDistance` | 3.0 | Макс. расстояние между пассажирами |
| `_clickRadius` | 0.4 | Радиус области клика |
| `_verticalSearchFactor` | 2.0 | Вертикальное растяжение области |

---

## Диагностика

### Diagnostics.cs

```csharp
// Включение/выключение логов
Diagnostics.Enabled = true;  // В редакторе
Diagnostics.Enabled = false; // Отключить логи
```

**Важно:** В Release сборках методы `Log()`, `Warn()`, `LogCategory()` автоматически вырезаются компилятором через атрибут `[Conditional]`.

Для принудительного включения логов в Release добавьте символ `DIAGNOSTICS_ENABLED` в `Player Settings → Scripting Define Symbols`.

---

## Рекомендуемые конфигурации

### Для мобильных устройств

```
PassengerSettings:
  globalSpeedMultiplier: 0.6
  maxFlightSpeed: 15
  maxBounces: 2

PassangerSpawner:
  _maxPassengersInScene: 15

TrainManager:
  _maxSpeed: 400
```

### Для "хаотичного" геймплея

```
PassengerSettings:
  globalSpeedMultiplier: 1.0
  maxFlightSpeed: 25
  bounceElasticity: 1.0
  magnetForce: 8.0
  repelForce: 6.0

PassangerSpawner:
  _maxPassengersInScene: 30
```

### Для "спокойного" геймплея

```
PassengerSettings:
  globalSpeedMultiplier: 0.5
  maxFlightSpeed: 12
  flightDeceleration: 1.0
  maxBounces: 1
  magnetForce: 3.0

PassangerSpawner:
  _maxPassengersInScene: 12
```
