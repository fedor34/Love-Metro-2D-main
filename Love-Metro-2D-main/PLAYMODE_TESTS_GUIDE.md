# Play Mode Tests - Руководство

## 🎮 Что такое Play Mode тесты?

В Unity есть два типа тестов:

### 📝 Edit Mode Tests (текущие тесты)
- Выполняются **вне игры**, в режиме редактора
- **Быстрые** - запускаются мгновенно
- **Ограничения**:
  - ❌ Не работают корутины (coroutines)
  - ❌ Нет физики (Physics2D.Simulate() не работает)
  - ❌ Нет времени (Time.deltaTime всегда 0)
  - ❌ Нет Update/FixedUpdate циклов
  - ❌ Нет анимаций

### 🎮 Play Mode Tests (для будущего)
- Выполняются **внутри игры**, как будто играет пользователь
- **Медленные** - запускают полную сцену
- **Возможности**:
  - ✅ Работают корутины
  - ✅ Полная физика
  - ✅ Time.deltaTime реальный
  - ✅ Update/FixedUpdate вызываются
  - ✅ Анимации работают

---

## 📊 Что должны тестировать Play Mode тесты?

### 1. **Физика и коллизии** 🎯
```csharp
[UnityTest]
public IEnumerator Passenger_CollidesWith_OppositeGender_FormsCouple()
{
    // Создать мужчину и женщину
    var male = CreatePassenger(false, new Vector3(0, 0, 0));
    var female = CreatePassenger(true, new Vector3(2, 0, 0));

    // Дать им скорости навстречу друг другу
    male.GetComponent<Rigidbody2D>().velocity = Vector2.right;
    female.GetComponent<Rigidbody2D>().velocity = Vector2.left;

    // Подождать несколько кадров (физика сработает)
    yield return new WaitForSeconds(1f);

    // Проверить что они сформировали пару
    Assert.IsTrue(male.IsInCouple);
    Assert.IsTrue(female.IsInCouple);
}
```

### 2. **Геймплей сценарии** 🎲
```csharp
[UnityTest]
public IEnumerator GameplayLoop_SpawnPassengers_ScoreIncreases()
{
    // Начальный счет
    int initialScore = ScoreCounter.Instance.CurrentScore;

    // Заспавнить пассажиров
    SpawnPassengers(10);

    // Подождать пока они пообразуют пары
    yield return new WaitForSeconds(5f);

    // Счет должен увеличиться
    Assert.Greater(ScoreCounter.Instance.CurrentScore, initialScore);
}
```

### 3. **Анимации и визуал** 🎨
```csharp
[UnityTest]
public IEnumerator Couple_PlaysMergeAnimation_WhenFormed()
{
    var male = CreatePassenger(false, Vector3.zero);
    var female = CreatePassenger(true, Vector3.right);

    // Создать пару вручную
    FormCouple(male, female);

    yield return new WaitForSeconds(0.5f);

    // Проверить что анимация проигралась
    var animator = GetCoupleAnimator(male);
    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("MergeAnimation"));
}
```

### 4. **Временные эффекты** ⏱️
```csharp
[UnityTest]
public IEnumerator VIPAbility_ExpiresAfter_10Seconds()
{
    var passenger = CreatePassenger(true, Vector3.zero);
    passenger.AddAbility(new VipAbility());

    // Подождать 11 секунд
    yield return new WaitForSeconds(11f);

    // Способность должна истечь
    Assert.IsFalse(passenger.HasAbility<VipAbility>());
}
```

### 5. **Интеграция систем** 🔗
```csharp
[UnityTest]
public IEnumerator FullGameplay_TrainStops_PassengersMatch_ScoreAwarded()
{
    // 1. Поезд останавливается
    TrainManager.Instance.StopTrain();
    yield return new WaitForSeconds(0.5f);

    // 2. Спавним пассажиров
    SpawnPassengers(5);

    // 3. Ждем формирования пар
    yield return new WaitForSeconds(3f);

    // 4. Поезд едет дальше
    TrainManager.Instance.StartTrain();
    yield return new WaitForSeconds(1f);

    // 5. Проверяем результаты
    Assert.Greater(CouplesManager.Instance.TotalCouplesFormed, 0);
    Assert.Greater(ScoreCounter.Instance.CurrentScore, 0);
}
```

---

## 🏗️ Как создать Play Mode тесты для Love Metro 2D

### Шаг 1: Создать assembly definition
Создайте файл `Assets/Tests/PlayMode/Tests.PlayMode.asmdef`:
```json
{
    "name": "Tests.PlayMode",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "LoveMetro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### Шаг 2: Создать тестовую сцену
1. Создайте сцену `Assets/Tests/PlayMode/TestScene.unity`
2. Добавьте минимальные объекты:
   - Camera
   - PassengerRegistry (пустой GameObject с компонентом)
   - CouplesManager
   - ScoreCounter
   - Canvas для UI

### Шаг 3: Написать тесты
Создайте файл `Assets/Tests/PlayMode/GameplayPlayModeTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class GameplayPlayModeTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        // Загрузить тестовую сцену перед каждым тестом
        yield return SceneManager.LoadSceneAsync("Assets/Tests/PlayMode/TestScene.unity");
    }

    [UnityTest]
    public IEnumerator PhysicsCollision_CreatesCouple()
    {
        // Создать пассажиров
        var male = CreateTestPassenger(false, new Vector3(-2, 0, 0));
        var female = CreateTestPassenger(true, new Vector3(2, 0, 0));

        // Дать им скорости навстречу
        male.GetComponent<Rigidbody2D>().velocity = Vector2.right * 5f;
        female.GetComponent<Rigidbody2D>().velocity = Vector2.left * 5f;

        // Подождать столкновения
        yield return new WaitForSeconds(1f);

        // Проверить результат
        Assert.IsTrue(male.IsInCouple || female.IsInCouple);
    }

    private Passenger CreateTestPassenger(bool isFemale, Vector3 position)
    {
        var go = new GameObject($"TestPassenger_{(isFemale ? "F" : "M")}");
        go.transform.position = position;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.5f, 1f);

        go.AddComponent<PassangerAnimator>();

        var passenger = go.AddComponent<Passenger>();
        passenger.IsFemale = isFemale;
        passenger.IsMatchable = true;

        return passenger;
    }
}
```

---

## 📋 Приоритеты тестирования

### Высокий приоритет (обязательно для Play Mode)
1. ✅ **Физика столкновений** - основа геймплея
2. ✅ **Формирование пар** - core механика
3. ✅ **Система очков** - награды за пары
4. ✅ **Способности (Abilities)** - VIP, множители

### Средний приоритет
5. **Train Manager** - остановки поезда
6. **Spawning** - появление пассажиров
7. **Анимации** - визуальные эффекты
8. **UI обновления** - счетчик, текст

### Низкий приоритет
9. **Звуки** - аудио эффекты
10. **Партиклы** - визуальные эффекты

---

## ⚡ Производительность

**Edit Mode тесты**: ~0.1-1 секунда на весь набор
**Play Mode тесты**: ~10-60 секунд на набор (медленнее!)

**Рекомендация**:
- Используйте **Edit Mode** для логики и юнит-тестов (то что мы уже сделали ✅)
- Используйте **Play Mode** только для интеграционных тестов с физикой

---

## 🎯 Текущий статус

**Сделано**:
- ✅ 75+ Edit Mode тестов
- ✅ Покрытие всех основных систем
- ✅ Найдено и исправлено несколько багов

**Следующий шаг**:
- 📝 Добавить 10-15 Play Mode тестов для физики и геймплея
- 🎮 Создать тестовую сцену
- ⚡ Настроить CI/CD для автозапуска тестов

---

## 📚 Полезные ссылки

- [Unity Testing Documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit Framework](https://nunit.org/)
- [Play Mode vs Edit Mode](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/reference-tests-playmode.html)
