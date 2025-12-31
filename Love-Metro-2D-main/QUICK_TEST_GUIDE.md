# Быстрое руководство по запуску тестов

## ✅ Проблема исправлена

**Что было сделано:**
1. Удален несовместимый пакет `Unity Splines 2.7.2`
2. Проект теперь должен компилироваться без ошибок
3. Тесты готовы к запуску

## 🚀 Как запустить тесты в Unity Editor

### Шаг 1: Откройте проект
```
Unity Hub → Open → C:\Users\79605\Desktop\Love-Metro-2D-main\Love-Metro-2D-main
```

### Шаг 2: Дождитесь компиляции
Unity автоматически скомпилирует проект и установит пакеты.
Дождитесь завершения (обычно 1-2 минуты).

### Шаг 3: Откройте Test Runner
```
Меню: Window > General > Test Runner
Или: Ctrl+Alt+T
```

### Шаг 4: Запустите тесты
1. В окне Test Runner выберите вкладку **EditMode**
2. Нажмите кнопку **Run All**
3. Дождитесь выполнения всех тестов

## 📊 Ожидаемый результат

Вы должны увидеть **75+ тестов**:
- ✅ PassengerRegistryTests (20+)
- ✅ ManualPairingManagerTests (10+)
- ✅ ScoreCounterTests (8+)
- ✅ PassengerAbilitiesTests (10+)
- ✅ CoupleSystemTests (8+)
- ✅ GameplayIntegrationTests (10+)

**Все тесты должны пройти** (зеленые галочки).

## ⚠️ Если тесты не появляются

1. Проверьте что нет ошибок компиляции в Console
2. Переимпортируйте папку Tests:
   - ПКМ на `Assets/Tests` → Reimport
3. Перезапустите Unity Editor

## 📁 Где находятся тесты

```
Assets/
└── Tests/
    ├── Editor/
    │   ├── PassengerRegistryTests.cs
    │   ├── ManualPairingManagerTests.cs
    │   ├── ScoreCounterTests.cs
    │   ├── PassengerAbilitiesTests.cs
    │   ├── CoupleSystemTests.cs
    │   ├── GameplayIntegrationTests.cs
    │   └── Tests.Editor.asmdef
    ├── PlayMode/
    │   └── Tests.PlayMode.asmdef
    ├── README.md
    ├── HOW_TO_RUN_TESTS.md
    └── TEST_SUMMARY.md
```

## 🔧 Что было исправлено

**Проблема:** Unity Splines 2.7.2 несовместим с Unity 2022.3.59f1
```
error CS0117: 'PrefabUtility' does not contain a definition for 'prefabInstanceReverting'
```

**Решение:** Пакет удален из `Packages/manifest.json`

**Коммит:** `52e4973 - Fix: remove incompatible Unity Splines package`

## ✅ Статус

- [x] Проблема с компиляцией исправлена
- [x] Тесты готовы к запуску
- [x] Изменения закоммичены
- [x] Запушено на GitHub (source-only)

**Готово к работе!** 🎉
