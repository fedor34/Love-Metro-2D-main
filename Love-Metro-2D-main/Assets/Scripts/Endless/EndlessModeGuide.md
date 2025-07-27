# 📚 Руководство: бесконечный режим

> Система работает поверх существующей *FieldEffectSystem* и автоматически генерирует «катаклизмы» в вагоне.

---
## 1. Быстрая установка
1. Откройте **Scene2.unity** (или любую игровую сцену).
2. Создайте пустой объект `EndlessMode` и добавьте компонент **EndlessModeManager**.
3. Сконфигурируйте:
   * **Difficulty Profile** – создайте `Assets → Create → Endless → Difficulty Profile` и укажите кривые роста.
   * **Effect Pool** – добавьте элементы:
     | EffectType | BaseWeight | CooldownRounds |
     |------------|-----------|---------------|
     | Gravity    | 1         | 0             |
     | Wind       | 1         | 0             |
     | Vortex     | 0.4       | 2             |
     | Repulsion  | 0.5       | 1             |
4. Задайте **Round Duration** (сколько секунд эффекты активны) и **Break Duration** (передышка).
5. Запустите сцену – каждые N секунд будут спавниться эффекты поля.

---
## 2. Как это работает
1. **EndlessModeManager** ведёт счёт раундам, хранит таймеры.
2. На каждый раунд:
   * С помощью кривой *effectsPerRound* решает, сколько эффектов создать.
   * По кривой *strengthMultiplier* усиливает их «силу».
   * Случайно выбирает эффекты из пула (с учётом cooldown и веса).
   * Создаёт их методом `BaseFieldEffect.CreateEffect()` в случайной точке вагона.
3. После `roundDuration` ждёт `breakDuration` и запускает следующий раунд.

---
## 3. Тонкая настройка
* **spawnRangeX / Y** – диапазон координат, куда ставится центр эффекта (подберите под размеры вагона).
* **CooldownRounds** – защита от спама одного и того же эффекта.
* **AnimationCurve** в Difficulty Profile
  * *strengthMultiplier* – X=
    минуты; Y=множитель силы (1 = базовая). Пример: (0,1) → (5,2) → (10,3).
  * *effectsPerRound* – X=минуты; Y=кол-во одновременных эффектов.

---
## 4. Расширение
* Добавьте новые FieldEffect’ы
  * Реализуйте класс, зарегистрируйте в `BaseFieldEffect.CreateEffect()`.
* Сигналы-предупреждения
  * Создайте скрипт *AnnouncementUI*; подпишитесь на публичное событие `EndlessModeManager.OnRoundStart` (добавьте по необходимости).
* Баланс очков
  * Вызовите из *EndlessModeManager* `ScoreMultiplierSystem.SetMultiplier(strengthMultiplier);` – написать класс-обёртку.

---
## 5. Быстрый тест
В качестве теста:
```csharp
// Вначале сцены
EndlessModeManager mgr = FindObjectOfType<EndlessModeManager>();
Debug.Log($"Раунд {mgr.CurrentRound}, всего эффектов: {FindObjectsOfType<BaseFieldEffect>().Length}");
```
Запустите сцену на 2–3 минуты – должны чередоваться разные силы ветра, гравитации и вихри.

---
Enjoy! 🎢 