# UI и управление

## Система управления

### ClickDirectionManager

**Файл:** `ClickDirectionManager.cs`

Центральный обработчик пользовательского ввода. Преобразует действия мыши/тача в игровые команды.

#### Статические свойства (доступны из любого скрипта)

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsMouseHeld` | bool | Удерживается ли кнопка мыши |
| `HorizontalAxis` | float | Горизонтальное смещение от точки нажатия (-1..1) |
| `VerticalAxis` | float | Вертикальное смещение (-1..1) |
| `HorizontalVelocity` | float | Скорость горизонтального движения |
| `VerticalVelocity` | float | Скорость вертикального движения |
| `HasReleasePoint` | bool | Есть ли сохранённая точка отпускания |
| `LastReleaseWorld` | Vector2 | Мировые координаты последнего отпускания |

#### Жизненный цикл ввода

```
[MouseButtonDown]
     │
     ├── Сохраняем startPosition
     ├── IsMouseHeld = true
     │
     ↓
[MouseButton] (каждый кадр)
     │
     ├── delta = currentPos - startPos
     ├── HorizontalAxis = delta.x / (Screen.width * 0.5)
     ├── VerticalAxis = delta.y / (Screen.height * 0.5)
     ├── Рассчитываем velocity
     │
     ↓
[MouseButtonUp]
     │
     ├── IsMouseHeld = false
     ├── LastReleaseWorld = ScreenToWorldPoint(pos)
     └── HasReleasePoint = true
```

#### Использование

```csharp
// Проверка удержания
if (ClickDirectionManager.IsMouseHeld)
{
    float x = ClickDirectionManager.HorizontalAxis;
    // x > 0: тянем вправо (разгон)
    // x < 0: тянем влево (торможение)
}

// Получение направления
Vector2 direction = ClickDirectionManager.GetCurrentDirection();

// Точка отпускания для aim assist
if (ClickDirectionManager.HasReleasePoint)
{
    Vector2 target = ClickDirectionManager.LastReleaseWorld;
}
```

### Интеграция с TrainManager

```csharp
// TrainManager.Update()
bool isAccelerating = ClickDirectionManager.IsMouseHeld;
float x = ClickDirectionManager.HorizontalAxis;
float vx = ClickDirectionManager.HorizontalVelocity;

// Разгон/торможение по горизонтали
if (x > 0f)
    accelerationValue = x * _acceleration * 4f;
else if (x < 0f)
    accelerationValue = x * _brakeDeceleration * 3f;

// Флики усиливают действие
if (vx > 0.7f)
    accelerationValue += _acceleration * 3f * (vx - 0.7f);
```

### ManualPairingManager - Ручное создание пар

**Файл:** `Core/ManualPairingManager.cs`

Позволяет создавать пары кликом по двум близко стоящим пассажирам.

#### Интеграция

```csharp
// В ClickDirectionManager или другом обработчике:
if (Input.GetMouseButtonDown(0))
{
    if (ManualPairingManager.Instance.HandleClick(Input.mousePosition))
    {
        // Клик обработан как создание пары
        return;
    }
    // Иначе - обычное управление поездом
}
```

#### Логика

```csharp
public bool HandleClick(Vector2 screenPosition)
{
    Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);

    // Ищем пассажиров в вытянутой по вертикали области
    Vector2 boxSize = new Vector2(
        _clickRadius * 2f,
        _clickRadius * 2f * _verticalSearchFactor
    );
    Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, boxSize, 0f);

    // Собираем пассажиров
    List<Passenger> clicked = new List<Passenger>();
    foreach (var hit in hits)
    {
        if (hit.TryGetComponent<Passenger>(out var p))
            clicked.Add(p);
    }

    // Пытаемся создать пару из найденных
    return AttemptOverlapPairing(clicked);
}
```

---

## UI компоненты

### ScoreCounter - Счётчик очков

**Файл:** `UI/ScoreCounter.cs`

#### Основные методы

```csharp
public int GetBasePointsPerCouple()
{
    return _basePointsPerCouple; // По умолчанию 100
}

public void AwardMatchPoints(Vector3 screenPosition, int points)
{
    _totalScore += points;
    UpdateScoreDisplay();
    ShowScorePopup(screenPosition, points);
}

public int GetCurrentScore()
{
    return _totalScore;
}

public void ResetScore()
{
    _totalScore = 0;
    UpdateScoreDisplay();
}
```

#### Popup с очками

При создании пары показывается анимированный текст с количеством очков:

```csharp
private void ShowScorePopup(Vector3 screenPos, int points)
{
    // Создаём текст
    var popup = Instantiate(_scorePopupPrefab, _canvas.transform);
    popup.transform.position = screenPos;
    popup.GetComponent<Text>().text = $"+{points}";

    // Анимация подъёма и исчезновения
    StartCoroutine(AnimatePopup(popup));
}
```

### InertiaArrowHUD - Стрелка инерции

**Файл:** `UI/InertiaArrowHUD.cs`

Показывает направление и силу последнего импульса инерции.

#### Работа

```csharp
private void Update()
{
    Vector2 impulse = TrainManager.LastInertiaImpulse;

    if (impulse.sqrMagnitude < 0.01f)
    {
        // Скрываем стрелку
        _arrow.gameObject.SetActive(false);
        return;
    }

    _arrow.gameObject.SetActive(true);

    // Поворачиваем стрелку по направлению импульса
    float angle = Mathf.Atan2(impulse.y, impulse.x) * Mathf.Rad2Deg;
    _arrow.rotation = Quaternion.Euler(0, 0, angle);

    // Масштабируем по силе
    float magnitude = impulse.magnitude;
    float scale = Mathf.Clamp(magnitude / _maxMagnitude, 0.5f, 2f);
    _arrow.localScale = Vector3.one * scale;
}
```

### MenuManager - Управление меню

**Файл:** `UI/MenuManager.cs`

#### Основные методы

```csharp
public void ShowMainMenu();
public void HideMainMenu();
public void ShowPauseMenu();
public void HidePauseMenu();
public void ShowGameOverScreen(int finalScore);
public void RestartGame();
public void QuitGame();
```

#### Состояния

```csharp
public enum MenuState
{
    Hidden,
    MainMenu,
    Playing,
    Paused,
    GameOver
}

private MenuState _currentState = MenuState.MainMenu;
```

### CharactersPanel - Выбор персонажей

**Файл:** `UI/CharactersPanel.cs`

Панель для выбора/разблокировки персонажей.

#### Структура

```csharp
[System.Serializable]
public class CharacterData
{
    public string name;
    public Sprite portrait;
    public Passenger prefab;
    public bool isUnlocked;
    public int unlockCost;
}

[SerializeField] private List<CharacterData> _characters;
```

### SettingsPanel - Настройки

**Файл:** `UI/SettingsPanel.cs`

```csharp
[SerializeField] private Slider _musicVolumeSlider;
[SerializeField] private Slider _sfxVolumeSlider;
[SerializeField] private Toggle _vibrationToggle;

public void OnMusicVolumeChanged(float value)
{
    AudioManager.Instance.SetMusicVolume(value);
    PlayerPrefs.SetFloat("MusicVolume", value);
}

public void OnVibrationToggled(bool enabled)
{
    PlayerPrefs.SetInt("Vibration", enabled ? 1 : 0);
}
```

---

## Параллакс и фон

### ParallaxEffect

**Файл:** `Parallax/ParallaxEffect.cs`

Основной эффект параллакса для фона.

```csharp
public void SetTrainSpeed(float speed)
{
    _currentSpeed = speed;
}

private void Update()
{
    // Смещаем слои с разной скоростью
    foreach (var layer in _layers)
    {
        float offset = _currentSpeed * layer.speedMultiplier * Time.deltaTime;
        layer.transform.Translate(Vector3.left * offset);

        // Бесконечный скролл
        if (layer.transform.position.x < -layer.width)
        {
            layer.transform.position += Vector3.right * layer.width * 2;
        }
    }
}
```

### SimpleBackgroundScroller

**Файл:** `Parallax/SimpleBackgroundScroller.cs`

Упрощённый скроллинг фона.

```csharp
private void Update()
{
    float speed = _train != null ? _train.GetCurrentSpeed() : 0f;

    // Смещаем только при удержании мыши
    if (ClickDirectionManager.IsMouseHeld)
    {
        _offset += speed * _scrollSpeed * Time.deltaTime;
        _renderer.material.mainTextureOffset = new Vector2(_offset, 0f);
    }
}
```

### ParallaxMaterialDriver

**Файл:** `Parallax/ParallaxMaterialDriver.cs`

Управление материалами для параллакса через shader offset.

```csharp
private void Update()
{
    float speed = _train != null ? _train.GetCurrentSpeed() : 0f;

    foreach (var layer in _layers)
    {
        float offset = speed * layer.speedFactor * Time.deltaTime;
        layer.material.mainTextureOffset += new Vector2(offset, 0f);
    }
}
```

---

## Анимации UI

### Popup анимация

```csharp
private IEnumerator AnimatePopup(GameObject popup, float duration = 1f)
{
    float elapsed = 0f;
    Vector3 startPos = popup.transform.position;
    Color startColor = popup.GetComponent<Text>().color;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        // Подъём
        popup.transform.position = startPos + Vector3.up * (t * 50f);

        // Затухание
        Color c = startColor;
        c.a = 1f - t;
        popup.GetComponent<Text>().color = c;

        // Масштаб (вначале увеличивается, потом уменьшается)
        float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
        popup.transform.localScale = Vector3.one * scale;

        yield return null;
    }

    Destroy(popup);
}
```

### Fade-эффекты

```csharp
public IEnumerator FadeIn(CanvasGroup group, float duration = 0.3f)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        group.alpha = elapsed / duration;
        yield return null;
    }
    group.alpha = 1f;
}

public IEnumerator FadeOut(CanvasGroup group, float duration = 0.3f)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        group.alpha = 1f - (elapsed / duration);
        yield return null;
    }
    group.alpha = 0f;
}
```

---

## Обработка событий

### События UI

```csharp
// Подписка на кнопки
[SerializeField] private Button _startButton;
[SerializeField] private Button _settingsButton;

private void Start()
{
    _startButton.onClick.AddListener(OnStartClicked);
    _settingsButton.onClick.AddListener(OnSettingsClicked);
}

private void OnStartClicked()
{
    HideMainMenu();
    StartGame();
}
```

### Интеграция с игровыми событиями

```csharp
// Подписка на события поезда
private void Start()
{
    _trainManager.OnBrakeStart += OnTrainBrakeStart;
    _trainManager.OnBrakeEnd += OnTrainBrakeEnd;
}

private void OnDestroy()
{
    if (_trainManager != null)
    {
        _trainManager.OnBrakeStart -= OnTrainBrakeStart;
        _trainManager.OnBrakeEnd -= OnTrainBrakeEnd;
    }
}

private void OnTrainBrakeStart()
{
    // Показать индикатор торможения
    _brakeIndicator.SetActive(true);
}
```

---

## Адаптивный UI

### Safe Area

```csharp
private void ApplySafeArea()
{
    Rect safeArea = Screen.safeArea;
    Vector2 anchorMin = safeArea.position;
    Vector2 anchorMax = safeArea.position + safeArea.size;

    anchorMin.x /= Screen.width;
    anchorMin.y /= Screen.height;
    anchorMax.x /= Screen.width;
    anchorMax.y /= Screen.height;

    _safeAreaRect.anchorMin = anchorMin;
    _safeAreaRect.anchorMax = anchorMax;
}
```

### Масштабирование

```csharp
// CanvasScaler настройки
[SerializeField] private CanvasScaler _scaler;

private void ConfigureScaler()
{
    _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    _scaler.referenceResolution = new Vector2(1920, 1080);
    _scaler.matchWidthOrHeight = 0.5f; // Баланс между шириной и высотой
}
```
