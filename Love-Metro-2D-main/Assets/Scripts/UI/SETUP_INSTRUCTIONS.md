# 🎮 Инструкция по настройке меню

## ✅ Выполненные шаги:

1. **MainMenu добавлено в Build Settings** - сцена установлена как первая
2. **GameSceneManager интеграция** - MenuManager теперь использует правильную систему загрузки
3. **MenuInitializer создан** - автоматически инициализирует GameSceneManager

## 🔧 Настройка в Unity Editor:

### 1. Откройте сцену MainMenu
```
Assets/Scenes/New Folder/MainMenu.unity
```

### 2. Настройка MenuManager
На GameObject с компонентом `MenuManager`:

**UI Элементы:**
- `Play Button` → перетащите кнопку "ИГРАТЬ"
- `Characters Button` → перетащите кнопку "ЧЕЛОВЕЧКИ"
- `Settings Button` → перетащите кнопку "НАСТРОЙКИ"
- `Exit Button` → перетащите кнопку "ВЫХОД"

**Панели меню:**
- `Main Menu Panel` → панель с главным меню
- `Characters Panel` → панель с персонажами (изначально отключена)
- `Settings Panel` → панель настроек (изначально отключена)

**Настройки игры:**
- `Game Scene Name` → "Scene2" (уже установлено)

### 3. Добавьте MenuInitializer
Добавьте компонент `MenuInitializer` на любой GameObject в сцене (например, на Canvas):
- `Main Menu Scene Name` → "MainMenu"
- `Game Scene Name` → "Scene2"
- `Use Loading Screen` → false (для быстрого перехода)

### 4. Настройка кнопок
Для каждой кнопки в Inspector:

**Кнопка "ИГРАТЬ":**
- `On Click ()` → MenuManager.OnPlayButtonClicked

**Кнопка "ЧЕЛОВЕЧКИ":**
- `On Click ()` → MenuManager.OnCharactersButtonClicked

**Кнопка "НАСТРОЙКИ":**
- `On Click ()` → MenuManager.OnSettingsButtonClicked

**Кнопка "ВЫХОД":**
- `On Click ()` → MenuManager.OnExitButtonClicked

### 5. Кнопки "НАЗАД" в панелях
На кнопках "НАЗАД" в Characters и Settings панелях:
- `On Click ()` → MenuManager.BackToMainMenu

### 6. Тестирование (необязательно)
Добавьте компонент `MenuTestUtility` на любой GameObject для быстрого тестирования:
- Клавиша **P** - тест кнопки "ИГРАТЬ"
- Клавиша **M** - тест возврата в меню
- GUI кнопки в левом верхнем углу экрана

## 🚀 Готово!
После этой настройки:
- Кнопка "ИГРАТЬ" будет загружать Scene2
- Остальные кнопки будут переключать панели
- ESC вернёт в главное меню
- Enter запустит игру

## 🐛 Устранение проблем:

**Если кнопки не работают:**
1. Проверьте, что все поля MenuManager заполнены
2. Убедитесь, что MenuInitializer добавлен в сцену
3. Проверьте, что кнопки имеют компонент Button
4. Убедитесь, что Canvas имеет GraphicRaycaster

**Если сцена не загружается:**
1. Проверьте Build Settings - MainMenu должно быть первой сценой
2. Убедитесь, что Scene2.unity существует
3. Проверьте Console на ошибки 