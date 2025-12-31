# Как запустить тесты / How to Run Tests

## Русский

### Запуск в Unity Editor

1. **Откройте проект** в Unity Editor (версия 2021.3 или выше)

2. **Откройте Test Runner**:
   - Меню: `Window > General > Test Runner`
   - Или используйте горячую клавишу `Ctrl+Alt+T` (Windows) / `Cmd+Alt+T` (Mac)

3. **Выберите режим тестирования**:
   - Нажмите на вкладку **EditMode** (для юнит-тестов)

4. **Запустите тесты**:
   - Нажмите кнопку **Run All** для запуска всех тестов
   - Или раскройте список и запустите отдельные тесты/группы

5. **Просмотр результатов**:
   - ✅ Зеленая галочка = тест пройден
   - ❌ Красный крестик = тест не прошел
   - Нажмите на тест чтобы увидеть детали ошибки

### Список тестов

- **PassengerRegistryTests** (20+ тестов) - Регистрация пассажиров
- **ManualPairingManagerTests** (10+ тестов) - Ручное создание пар
- **ScoreCounterTests** (8+ тестов) - Подсчет очков
- **PassengerAbilitiesTests** (10+ тестов) - Система способностей
- **CoupleSystemTests** (8+ тестов) - Система пар
- **GameplayIntegrationTests** (10+ тестов) - Интеграционные тесты

**Всего: 75+ тестов**

### Запуск через командную строку

```bash
# Windows
"C:\Program Files\Unity\Hub\Editor\2021.3.x\Editor\Unity.exe" -runTests -batchmode -projectPath "C:\путь\к\проекту" -testResults "результаты.xml" -testPlatform EditMode

# Mac/Linux
/Applications/Unity/Hub/Editor/2021.3.x/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath "/путь/к/проекту" -testResults "результаты.xml" -testPlatform EditMode
```

### Устранение неполадок

**Тесты не появляются:**
- Убедитесь, что файлы `.asmdef` существуют в папках `Tests/Editor` и `Tests/PlayMode`
- Переимпортируйте папку Tests (ПКМ > Reimport)
- Перезапустите Unity Editor

**Ошибки компиляции:**
- Проверьте, что проект компилируется без ошибок
- Убедитесь, что все зависимости установлены

**Тесты падают:**
- Проверьте Console для детальных сообщений об ошибках
- Убедитесь, что тестируемые компоненты существуют в проекте

---

## English

### Running in Unity Editor

1. **Open the project** in Unity Editor (version 2021.3 or higher)

2. **Open Test Runner**:
   - Menu: `Window > General > Test Runner`
   - Or use hotkey `Ctrl+Alt+T` (Windows) / `Cmd+Alt+T` (Mac)

3. **Select test mode**:
   - Click on **EditMode** tab (for unit tests)

4. **Run tests**:
   - Click **Run All** button to run all tests
   - Or expand the list and run individual tests/groups

5. **View results**:
   - ✅ Green checkmark = test passed
   - ❌ Red cross = test failed
   - Click on a test to see error details

### Test List

- **PassengerRegistryTests** (20+ tests) - Passenger registration
- **ManualPairingManagerTests** (10+ tests) - Manual pairing
- **ScoreCounterTests** (8+ tests) - Score counting
- **PassengerAbilitiesTests** (10+ tests) - Ability system
- **CoupleSystemTests** (8+ tests) - Couple system
- **GameplayIntegrationTests** (10+ tests) - Integration tests

**Total: 75+ tests**

### Running via Command Line

```bash
# Windows
"C:\Program Files\Unity\Hub\Editor\2021.3.x\Editor\Unity.exe" -runTests -batchmode -projectPath "C:\path\to\project" -testResults "results.xml" -testPlatform EditMode

# Mac/Linux
/Applications/Unity/Hub/Editor/2021.3.x/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath "/path/to/project" -testResults "results.xml" -testPlatform EditMode
```

### Troubleshooting

**Tests not showing:**
- Ensure `.asmdef` files exist in `Tests/Editor` and `Tests/PlayMode` folders
- Re-import the Tests folder (Right-click > Reimport)
- Restart Unity Editor

**Compilation errors:**
- Check that the project compiles without errors
- Ensure all dependencies are installed

**Tests failing:**
- Check Console for detailed error messages
- Ensure tested components exist in the project

---

## Быстрый старт / Quick Start

1. Open Unity Editor
2. `Window > General > Test Runner`
3. Click **EditMode** tab
4. Click **Run All**
5. Wait for results

✅ All tests should pass!
