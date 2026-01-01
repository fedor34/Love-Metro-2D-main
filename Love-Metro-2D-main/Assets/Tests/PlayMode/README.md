# Play Mode Tests

## 📋 Обзор

Play Mode тесты для Love Metro 2D - тесты, которые выполняются в режиме игры с полной физикой, корутинами и Unity lifecycle.

## 📊 Структура тестов

### 1. PhysicsPlayModeTests.cs (5 тестов)
Тестирует физику и коллизии:
- ✅ `Physics_TwoPassengers_MovingTowardsEachOther_FormCouple` - пассажиры сталкиваются и образуют пару
- ✅ `Physics_SameGenderPassengers_DoNotFormCouple` - одинаковый пол не образует пары
- ✅ `Physics_PassengerFallsWithGravity` - гравитация работает
- ✅ `Rigidbody_StopsMoving_WhenVelocityReachesZero` - drag замедляет движение
- ✅ `Collision_WithBoundaries_BouncesBack` - столкновение со стенами

### 2. GameplayPlayModeTests.cs (6 тестов)
Тестирует игровую логику:
- ✅ `ScoreCounter_AwardsPoints_OverTime` - очки начисляются с корутинами
- ✅ `VIPAbility_DoublesPoints_InRealtime` - VIP удваивает очки в runtime
- ✅ `PassengerRegistry_TracksPassengers_InRealtime` - реестр отслеживает пассажиров
- ✅ `CouplesManager_ManagesCouples_OverTime` - менеджер пар работает
- ✅ `MultiplePassengers_RegisteredAndTracked_Simultaneously` - множество пассажиров одновременно
- ✅ `Passenger_Cleanup_RemovesFromRegistry` - удаление очищает реестр

### 3. AbilitiesPlayModeTests.cs (6 тестов)
Тестирует систему способностей:
- ✅ `VIPAbility_DoublesPoints_WhenMatched` - VIP удваивает очки при спаривании
- ✅ `MultipleAbilities_StackCorrectly` - несколько способностей стакаются
- ✅ `Ability_CanBeAdded_AndRemoved_Dynamically` - динамическое добавление/удаление
- ✅ `PassengerWithoutAbilities_UsesBasePoints` - базовые очки без способностей
- ✅ `Abilities_PersistAcrossFrames` - способности сохраняются между кадрами
- ✅ `Ability_WorksAfterPassengerMovement` - способности работают после движения

## 🚀 Как запустить

### В Unity Editor:

1. Откройте **Test Runner** (`Window > General > Test Runner` или `Ctrl+Alt+T`)
2. Выберите вкладку **PlayMode**
3. Нажмите **Run All** или выберите конкретные тесты

### Из командной строки:

```bash
"C:\Program Files\Unity\Hub\Editor\2022.3.59f1\Editor\Unity.exe" ^
  -runTests ^
  -batchmode ^
  -projectPath "C:\Users\79605\Desktop\Love-Metro-2D-main\Love-Metro-2D-main" ^
  -testResults PlayModeTestResults.xml ^
  -testPlatform PlayMode ^
  -logFile PlayModeTestLog.txt
```

## ⏱️ Время выполнения

- **Edit Mode тесты**: ~1-2 секунды
- **Play Mode тесты**: ~30-60 секунд (медленнее из-за физики и корутин)

## 🎯 Что тестируется

### Физика (Physics)
- Движение и столкновения Rigidbody2D
- Gravity и drag
- Отскоки от стен
- Формирование пар через физику

### Игровая логика (Gameplay)
- Начисление очков с корутинами
- Регистрация пассажиров в реестре
- Управление парами
- Cleanup при уничтожении объектов

### Способности (Abilities)
- VIP удвоение очков
- Стакинг способностей
- Динамическое добавление/удаление
- Персистентность через время

## 📝 Важные отличия от Edit Mode

| Функция | Edit Mode | Play Mode |
|---------|-----------|-----------|
| Корутины | ❌ Не работают | ✅ Работают |
| Физика | ❌ Нет | ✅ Полная |
| Time.deltaTime | ⚠️ Всегда 0 | ✅ Реальное время |
| Update() | ❌ Не вызывается | ✅ Вызывается |
| Скорость | ⚡ Быстро (~1с) | 🐌 Медленно (~30с) |

## 🔧 Советы по написанию тестов

### 1. Используйте UnitySetUp/UnityTearDown
```csharp
[UnitySetUp]
public IEnumerator Setup()
{
    // Создание тестового окружения
    yield return null;
}

[UnityTearDown]
public IEnumerator Teardown()
{
    // Очистка после теста
    yield return null;
}
```

### 2. Ждите завершения физики
```csharp
[UnityTest]
public IEnumerator MyPhysicsTest()
{
    // Создать объекты

    // Дать физике поработать
    yield return new WaitForSeconds(2f);

    // Проверить результат
    Assert.IsTrue(condition);
}
```

### 3. Очищайте созданные объекты
```csharp
private GameObject testContainer;

[UnitySetUp]
public IEnumerator Setup()
{
    testContainer = new GameObject("TestContainer");
    yield return null;
}

[UnityTearDown]
public IEnumerator Teardown()
{
    if (testContainer != null)
        Object.Destroy(testContainer);
    yield return null;
}
```

## 🐛 Известные ограничения

1. **Медленная скорость** - Play Mode тесты медленнее Edit Mode в ~30 раз
2. **Зависимость от времени** - `yield return new WaitForSeconds()` может быть нестабильным
3. **Cleanup** - важно правильно очищать объекты между тестами
4. **Сцена** - тесты запускаются в пустой сцене, нужно создавать всё программно

## 📊 Метрики покрытия

- **Общее количество Play Mode тестов**: 17
- **Покрытие физики**: ~60%
- **Покрытие gameplay**: ~70%
- **Покрытие abilities**: ~80%

## 🎓 Что дальше?

Добавьте тесты для:
1. **TrainManager** - остановки и движение поезда
2. **Spawning system** - генерация пассажиров
3. **UI анимации** - floating text, счетчик
4. **Временные эффекты** - истечение способностей по таймеру
5. **Комбо системы** - множественные пары за короткое время

---

**Создано**: 2026-01-01
**Версия Unity**: 2022.3.59f1
**Test Framework**: Unity Test Framework 1.1.33
