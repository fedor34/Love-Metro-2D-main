# 🚀 Быстрая справка по созданию меню

## 📱 Структура Hierarchy (копировать в Unity)

```
Scene
├── Main Camera
├── Canvas
│   ├── Background (Empty GameObject)
│   │   ├── CityBackground_6 (Image: 6_город_фон.png)
│   │   ├── CityBackground_5 (Image: 5_город_дальний.png)
│   │   ├── CityBackground_4 (Image: 4_город_средний.png)
│   │   ├── CityBackground_3 (Image: 3_город_ближний.png)
│   │   ├── CityBackground_2 (Image: 2_город_деревья.png)
│   │   └── TrainInterior (Image: вагон внутри 1.png)
│   ├── MainMenuPanel (Empty GameObject)
│   │   ├── Title (Text - TextMeshPro: "ИГРАТЬ ЧЕЛОВЕЧКИ НАСТРОЙКИ")
│   │   └── MenuButtons (Empty + Vertical Layout Group)
│   │       ├── PlayButton (Button - TextMeshPro: "ИГРАТЬ")
│   │       ├── CharactersButton (Button - TextMeshPro: "ЧЕЛОВЕЧКИ")
│   │       ├── SettingsButton (Button - TextMeshPro: "НАСТРОЙКИ")
│   │       └── ExitButton (Button - TextMeshPro: "ВЫХОД")
│   ├── CharactersPanel (Empty GameObject, Active: FALSE)
│   │   ├── Background (Image: полупрозрачный черный)
│   │   ├── BackButton (Button - TextMeshPro: "НАЗАД")
│   │   ├── CharacterDetails (Empty GameObject)
│   │   │   ├── SelectedCharacterImage (Image)
│   │   │   ├── SelectedCharacterName (Text - TextMeshPro)
│   │   │   ├── SelectedCharacterDescription (Text - TextMeshPro)
│   │   │   └── SelectedCharacterStats (Text - TextMeshPro)
│   │   └── CharactersScrollView (Scroll View)
│   │       └── Viewport → Content (= CharactersContainer)
│   ├── SettingsPanel (Empty GameObject, Active: FALSE)
│   │   ├── Background (Image: полупрозрачный черный)
│   │   ├── BackButton (Button - TextMeshPro: "НАЗАД")
│   │   ├── AudioSettings (Empty GameObject)
│   │   │   ├── MasterVolumeSlider (Slider)
│   │   │   ├── MusicVolumeSlider (Slider)
│   │   │   └── SFXVolumeSlider (Slider)
│   │   ├── GraphicsSettings (Empty GameObject)
│   │   │   ├── QualityDropdown (Dropdown - TextMeshPro)
│   │   │   ├── FullscreenToggle (Toggle)
│   │   │   └── VSyncToggle (Toggle)
│   │   ├── ApplyButton (Button - TextMeshPro: "ПРИМЕНИТЬ")
│   │   └── DefaultsButton (Button - TextMeshPro: "ПО УМОЛЧАНИЮ")
│   └── EventSystem (автоматически создается с Canvas)
├── MenuManager (Empty GameObject + MenuManager script)
└── SceneManager (Empty GameObject + GameSceneManager script)
```

## ⚙️ Быстрые настройки компонентов

### Canvas:
- **Canvas Scaler**: Scale With Screen Size
- **Reference Resolution**: 1920x1080
- **Screen Match Mode**: Match Width Or Height

### MenuButtons (Layout):
- **Vertical Layout Group**: Spacing = 20, Child Alignment = Middle Center
- **Content Size Fitter**: Vertical Fit = Preferred Size

### Все Image фона:
- **Rect Transform**: Anchors = Stretch, Offsets = (0,0,0,0)
- **Image Type**: Simple, Preserve Aspect = ✓

### Все кнопки:
- **Font**: ThaleahFat_TTF SDF
- **Font Size**: 24-32
- **Color**: белый текст, полупрозрачный фон кнопки

## 🔗 Подключение скриптов (Inspector References)

### MenuManager:
```
UI Элементы:
- Play Button → PlayButton
- Characters Button → CharactersButton  
- Settings Button → SettingsButton
- Exit Button → ExitButton

Панели меню:
- Main Menu Panel → MainMenuPanel
- Characters Panel → CharactersPanel
- Settings Panel → SettingsPanel

Настройки игры:
- Game Scene Name → "Scene2"
```

### SettingsPanel:
```
Звук:
- Master Volume Slider → MasterVolumeSlider
- Music Volume Slider → MusicVolumeSlider
- SFX Volume Slider → SFXVolumeSlider

Графика:
- Quality Dropdown → QualityDropdown
- Fullscreen Toggle → FullscreenToggle
- VSync Toggle → VSyncToggle

Кнопки:
- Apply Button → ApplyButton
- Defaults Button → DefaultsButton
- Back Button → BackButton
```

### CharactersPanel:
```
UI Элементы:
- Characters Container → CharactersContainer (Scroll View Content)
- Back Button → BackButton

Детали персонажа:
- Selected Character Image → SelectedCharacterImage
- Selected Character Name → SelectedCharacterName
- Selected Character Description → SelectedCharacterDescription
- Selected Character Stats → SelectedCharacterStats

Данные персонажей:
- Characters Data → создать массив CharacterData[]
```

### GameSceneManager:
```
Названия сцен:
- Main Menu Scene → "MainMenu"
- Game Scene → "Scene2"
- Loading Scene → "Loading" (опционально)

Настройки загрузки:
- Use Loading Screen → ✓/✗
- Minimum Loading Time → 1.0
```

## 🎨 Цветовая схема (рекомендуемая)

- **Фон кнопок**: #000000AA (полупрозрачный черный)
- **Текст кнопок**: #FFFFFF (белый)
- **Фон панелей**: #000000CC (темнее чем кнопки)  
- **Заголовки**: #FFDD44 (желтый/золотой)
- **Hover кнопок**: #333333AA (серый)
- **Pressed кнопок**: #555555AA (светлее серый)

## ⌨️ Готовые хоткеи для тестирования

- **Enter** - запуск игры
- **ESC** - возврат в главное меню
- **←/→** - навигация персонажей (в панели персонажей)

## 🔧 Build Settings

1. **File → Build Settings**
2. **Add Open Scenes**: MainMenu.unity (индекс 0)
3. **Add Open Scenes**: Scene2.unity (индекс 1)
4. **Player Settings**: Company Name, Product Name

## ✅ Checklist готовности

- [ ] Сцена MainMenu.unity создана
- [ ] Canvas настроен (UI Scale Mode)
- [ ] Фон добавлен (многослойный город + вагон)
- [ ] 4 кнопки меню созданы
- [ ] Панели персонажей и настроек добавлены (неактивные)
- [ ] MenuManager подключен ко всем элементам
- [ ] SettingsPanel подключен к слайдерам/дропдаунам
- [ ] CharactersPanel настроен
- [ ] GameSceneManager добавлен
- [ ] Build Settings настроены
- [ ] Тестирование в Play Mode прошло

Время создания: ~30-45 минут для опытного пользователя Unity. 