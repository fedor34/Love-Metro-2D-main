# 🎮 Пошаговое создание сцены главного меню

## 📋 Шаг 1: Создание новой сцены

1. **File → New Scene** или **Ctrl+N**
2. Выберите **2D Template** (если доступно) или **Basic (Built-in)**
3. **File → Save As** → назовите `MainMenu.unity`
4. Сохраните в папку `Assets/Scenes/`

## 📋 Шаг 2: Настройка камеры

1. Выберите **Main Camera** в Hierarchy
2. В Inspector установите:
   - **Projection**: Orthographic
   - **Size**: 5 (можно подстроить позже)
   - **Background**: черный (#000000) или темно-серый

## 📋 Шаг 3: Создание Canvas

1. **Right Click в Hierarchy → UI → Canvas**
2. Выберите созданный Canvas
3. В **Canvas Scaler** компоненте установите:
   - **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920x1080
   - **Screen Match Mode**: Match Width Or Height
   - **Match**: 0.5

## 📋 Шаг 4: Создание фона меню

### 4.1 Создание многослойного фона (как в игре):
```
Canvas
└── Background
    ├── CityBackground_6 (6_город_фон.png)
    ├── CityBackground_5 (5_город_дальний.png)  
    ├── CityBackground_4 (4_город_средний.png)
    ├── CityBackground_3 (3_город_ближний.png)
    ├── CityBackground_2 (2_город_деревья.png)
    └── TrainInterior (вагон внутри 1.png)
```

### 4.2 Настройка каждого слоя:
1. **Right Click на Canvas → UI → Image**
2. Назовите первый Image как **CityBackground_6**
3. В **Image** компоненте:
   - **Source Image**: перетащите `6_город_фон.png`
   - **Image Type**: Simple
   - **Preserve Aspect**: ✓
4. В **Rect Transform**:
   - **Anchors**: Stretch (Alt+Shift при клике на Anchor Presets)
   - **Left, Top, Right, Bottom**: все по 0
5. Повторите для каждого слоя, размещая их в правильном порядке (сверху вниз в Hierarchy)

## 📋 Шаг 5: Создание главной панели меню

```
Canvas
└── MainMenuPanel
    ├── Background (настроенный выше)
    ├── Title
    ├── MenuButtons
    │   ├── PlayButton
    │   ├── CharactersButton  
    │   ├── SettingsButton
    │   └── ExitButton
    └── Logo (опционально)
```

### 5.1 Создание MainMenuPanel:
1. **Right Click на Canvas → Create Empty**
2. Назовите **MainMenuPanel**
3. **Add Component → Rect Transform** (должен быть автоматически)
4. Установите **Anchors**: Stretch

### 5.2 Создание заголовка:
1. **Right Click на MainMenuPanel → UI → Text - TextMeshPro**
2. Назовите **Title**
3. В **TextMeshPro** компоненте:
   - **Text**: "LOVE METRO 2D" (или как на картинке)
   - **Font Asset**: ThaleahFat_TTF SDF (из Assets/Thaleah_PixelFont/Materials/)
   - **Font Size**: 48-60
   - **Alignment**: Center и Middle
   - **Color**: белый или яркий цвет
4. В **Rect Transform**:
   - **Anchors**: Top Center
   - **Pos Y**: -100 (настройте по вкусу)

### 5.3 Создание контейнера кнопок:
1. **Right Click на MainMenuPanel → Create Empty**
2. Назовите **MenuButtons**
3. **Add Component → Vertical Layout Group**
4. В **Vertical Layout Group**:
   - **Spacing**: 20
   - **Child Alignment**: Middle Center
   - **Control Child Size**: Width ✓, Height ✓
   - **Use Child Scale**: ✓
   - **Child Force Expand**: Width ✓, Height ✗
5. **Add Component → Content Size Fitter**
6. В **Content Size Fitter**:
   - **Vertical Fit**: Preferred Size

## 📋 Шаг 6: Создание кнопок меню

### 6.1 Кнопка "ИГРАТЬ":
1. **Right Click на MenuButtons → UI → Button - TextMeshPro**
2. Назовите **PlayButton**
3. В **Button** компоненте:
   - Можете настроить цвета состояний (Normal, Highlighted, Pressed)
4. В **Image** компоненте кнопки:
   - **Color**: полупрозрачный темный (#000000AA) или подходящий цвет
5. Выберите **Text (TMP)** дочерний объект кнопки:
   - **Text**: "ИГРАТЬ"
   - **Font Asset**: ThaleahFat_TTF SDF
   - **Font Size**: 24-32
   - **Color**: белый
   - **Alignment**: Center и Middle

### 6.2 Повторите для остальных кнопок:
- **CharactersButton**: "ЧЕЛОВЕЧКИ"
- **SettingsButton**: "НАСТРОЙКИ"  
- **ExitButton**: "ВЫХОД"

## 📋 Шаг 7: Создание панели персонажей

```
Canvas
└── CharactersPanel (изначально неактивная)
    ├── Background
    ├── BackButton
    ├── CharacterDetails
    │   ├── SelectedCharacterImage
    │   ├── SelectedCharacterName
    │   ├── SelectedCharacterDescription
    │   └── SelectedCharacterStats
    └── CharactersScrollView
        └── CharactersContainer
```

### 7.1 Создание панели:
1. **Right Click на Canvas → Create Empty**
2. Назовите **CharactersPanel**
3. **Снимите галочку Active** в инспекторе (панель должна быть скрыта)
4. Добавьте полупрозрачный фон

### 7.2 Создание области прокрутки персонажей:
1. **Right Click на CharactersPanel → UI → Scroll View**
2. Назовите **CharactersScrollView**
3. Настройте под горизонтальную прокрутку карточек персонажей
4. **Viewport → Content** назовите **CharactersContainer**

## 📋 Шаг 8: Создание панели настроек

```
Canvas
└── SettingsPanel (изначально неактивная)
    ├── Background
    ├── BackButton
    ├── AudioSettings
    │   ├── MasterVolumeSlider
    │   ├── MusicVolumeSlider
    │   └── SFXVolumeSlider
    ├── GraphicsSettings
    │   ├── QualityDropdown
    │   ├── FullscreenToggle
    │   └── VSyncToggle
    └── ApplyButton
```

### 8.1 Создание панели:
1. **Right Click на Canvas → Create Empty**
2. Назовите **SettingsPanel**
3. **Снимите галочку Active** в инспекторе
4. Добавьте фон и разделите на секции

### 8.2 Добавление элементов управления:
- **Слайдеры**: UI → Slider
- **Дропдауны**: UI → Dropdown - TextMeshPro  
- **Тогглы**: UI → Toggle
- **Кнопки**: UI → Button - TextMeshPro

## 📋 Шаг 9: Подключение скриптов

### 9.1 MenuManager:
1. **Right Click в Hierarchy → Create Empty**
2. Назовите **MenuManager**
3. **Add Component → Menu Manager** (скрипт)
4. Перетащите все кнопки и панели в соответствующие поля

### 9.2 SettingsPanel:
1. Выберите **SettingsPanel**
2. **Add Component → Settings Panel**
3. Подключите все UI элементы

### 9.3 CharactersPanel:
1. Выберите **CharactersPanel**  
2. **Add Component → Characters Panel**
3. Создайте префаб карточки персонажа

### 9.4 GameSceneManager:
1. **Right Click в Hierarchy → Create Empty**
2. Назовите **SceneManager**
3. **Add Component → Game Scene Manager**

## 📋 Шаг 10: Настройка Build Settings

1. **File → Build Settings**
2. **Add Open Scenes** (добавить MainMenu.unity)
3. Перетащите **MainMenu** на первое место (индекс 0)
4. Убедитесь, что **Scene2** тоже добавлена

## 📋 Шаг 11: Тестирование

1. **Play** в редакторе
2. Проверьте переходы между панелями
3. Протестируйте кнопку "ИГРАТЬ" (должна загружать Scene2)
4. Проверьте настройки и их сохранение

## 🎨 Дополнительные улучшения

### Анимации:
- **Window → Animation** для создания анимаций появления панелей
- Анимации hover эффектов для кнопок
- Плавные переходы между панелями

### Звуки:
- Звуки нажатий кнопок
- Фоновая музыка в стиле метро
- Звуки поезда для атмосферы

### Эффекты:
- Частицы или анимированные элементы
- Parallax эффект для фона
- Мерцание текста или кнопок

## 🔧 Готовые настройки Rect Transform

### Для Title (заголовок):
- **Anchors**: Top Center
- **Anchor Position**: (0, -100)
- **Size**: (800, 100)

### Для MenuButtons:
- **Anchors**: Middle Center  
- **Anchor Position**: (0, -50)
- **Size**: (300, 240)

### Для кнопок:
- **Height**: 50
- **Preferred Width**: 250

Эта структура создаст красивое и функциональное меню в стиле Love Metro 2D! 