# 🎮 Система меню Love Metro 2D

## 📁 Структура файлов

```
Assets/Scripts/
├── UI/
│   ├── MenuManager.cs          # Главный менеджер меню
│   ├── SettingsPanel.cs        # Панель настроек
│   ├── CharactersPanel.cs      # Панель персонажей
│   └── README.md              # Этот файл
└── Core/
    └── SceneManager.cs        # Менеджер сцен (переходы между сценами)
```

## 🚀 Быстрый старт

### 1. Создание сцены главного меню

1. **Создайте новую сцену** `MainMenu.unity` в папке `Assets/Scenes/`
2. **Добавьте в Build Settings**: 
   - File → Build Settings
   - Add Open Scenes (MainMenu должно быть первым, индекс 0)

### 2. Настройка UI в сцене MainMenu

#### Базовая структура Canvas:
```
Canvas (Screen Space - Overlay)
├── MainMenuPanel
│   ├── Background (Image - фон метро из скриншота)
│   ├── Title (TMP_Text "LOVE METRO 2D")
│   └── ButtonsContainer
│       ├── PlayButton (Button + TMP_Text "ИГРАТЬ")
│       ├── CharactersButton (Button + TMP_Text "ЧЕЛОВЕЧКИ")
│       ├── SettingsButton (Button + TMP_Text "НАСТРОЙКИ")
│       └── ExitButton (Button + TMP_Text "ВЫХОД")
├── CharactersPanel (изначально неактивная)
│   ├── Background
│   ├── CharactersContainer (для карточек персонажей)
│   ├── CharacterDetails (детали выбранного персонажа)
│   └── BackButton
└── SettingsPanel (изначально неактивная)
    ├── Background
    ├── AudioSettings (слайдеры громкости)
    ├── GraphicsSettings (дропдауны и тогглы)
    └── BackButton
```

### 3. Подключение скриптов

#### MenuManager:
1. Добавьте `MenuManager` компонент на пустой GameObject
2. Настройте все ссылки в инспекторе:
   - **UI Элементы**: кнопки главного меню
   - **Панели меню**: объекты панелей
   - **Настройки игры**: название игровой сцены (по умолчанию "Scene2")

#### SettingsPanel:
1. Добавьте `SettingsPanel` на объект панели настроек
2. Подключите все UI элементы (слайдеры, дропдауны, тогглы)

#### CharactersPanel:
1. Добавьте `CharactersPanel` на объект панели персонажей
2. Создайте префаб карточки персонажа с компонентом `CharacterCard`
3. Настройте массив `CharacterData` с данными персонажей

#### GameSceneManager:
1. Добавьте `GameSceneManager` на любой GameObject в сцене MainMenu
2. Настройте названия сцен в инспекторе

## 🎨 Настройка персонажей

### Создание CharacterData:

```csharp
[System.Serializable]
public class CharacterData
{
    public string characterName = "Девушка №1";
    public Sprite portrait;              // Портрет для карточки
    public Sprite fullBodySprite;        // Полный спрайт для детального просмотра
    public string description = "Описание персонажа";
    
    // Характеристики (1-10)
    public int speed = 5;
    public int attractiveness = 7;
    public int stability = 6;
    
    // Префабы для игры
    public GameObject malePrefab;
    public GameObject femalePrefab;
    
    // Разблокировка
    public bool isUnlocked = true;
    public string unlockCondition = "";
}
```

## 🎮 Управление

### В главном меню:
- **Enter** - быстрый старт игры
- **ESC** - возврат к главному меню (из подменю)

### В панели персонажей:
- **←/→** стрелки - навигация между персонажами
- **ESC** - возврат в главное меню

### В игре:
- **ESC** - возврат в главное меню

## 🔧 Интеграция с существующей игрой

### 1. Добавление возврата в меню из игры:
В любой игровой скрипт добавьте:
```csharp
if (Input.GetKeyDown(KeyCode.Escape))
{
    if (GameSceneManager.Instance != null)
        GameSceneManager.Instance.ReturnToMainMenu();
}
```

### 2. Использование выбранных персонажей:
```csharp
// Получение текущего выбранного персонажа
CharactersPanel panel = FindObjectOfType<CharactersPanel>();
CharacterData selectedCharacter = panel.GetSelectedCharacter();
```

## 📝 Настройки (PlayerPrefs ключи)

Система автоматически сохраняет настройки:
- `MasterVolume` - общая громкость
- `MusicVolume` - громкость музыки  
- `SFXVolume` - громкость звуков
- `Quality` - качество графики
- `Fullscreen` - полноэкранный режим
- `VSync` - вертикальная синхронизация
- `GameSpeed` - скорость игры
- `DebugMode` - режим отладки

## 🎯 Следующие шаги

1. **Создайте MainMenu сцену** с UI элементами
2. **Добавьте красивый фон** в стиле метро из скриншота
3. **Настройте шрифты** (используйте Thaleah_PixelFont для пиксельного стиля)
4. **Создайте анимации** для кнопок и переходов
5. **Добавьте звуки** для кнопок и фоновую музыку
6. **Протестируйте** переходы между сценами

## 🐛 Возможные проблемы

- **Missing references** - проверьте все ссылки в инспекторах
- **Scene not in Build Settings** - добавьте все сцены в Build Settings
- **GameSceneManager not found** - убедитесь, что объект помечен как DontDestroyOnLoad
- **UI не реагирует** - проверьте наличие EventSystem в сцене 