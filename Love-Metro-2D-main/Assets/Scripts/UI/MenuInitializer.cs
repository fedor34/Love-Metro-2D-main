using UnityEngine;

/// <summary>
/// Компонент для инициализации GameSceneManager в сцене меню
/// Добавьте этот компонент на любой GameObject в сцене MainMenu
/// </summary>
[DefaultExecutionOrder(-1000)] // гарантируем, что выполняется раньше MenuManager
public class MenuInitializer : MonoBehaviour
{
    private static MenuInitializer _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInitializer()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("MenuInitializer(Auto)");
            _instance = go.AddComponent<MenuInitializer>();
            DontDestroyOnLoad(go);
        }
    }

    [Header("Настройки сцен")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private string _gameSceneName = "Scene2";
    [SerializeField] private bool _useLoadingScreen = false;

    [Header("Названия объектов UI (опционально)")]
    [SerializeField] private string _playButtonName = "PlayButton";
    [SerializeField] private string _charactersButtonName = "CharactersButton";
    [SerializeField] private string _settingsButtonName = "SettingsButton";
    [SerializeField] private string _exitButtonName = "ExitButton";
    [SerializeField] private string _mainMenuPanelName = "MainMenuPanel";
    [SerializeField] private string _charactersPanelName = "CharactersPanel";
    [SerializeField] private string _settingsPanelName = "SettingsPanel";

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        EnsureSceneManager();
        AutoConfigureMenuManager();
    }

    private void EnsureSceneManager()
    {
        // Проверяем, есть ли уже GameSceneManager
        if (GameSceneManager.Instance == null)
        {
            // Создаём GameSceneManager
            GameObject sceneManagerObj = new GameObject("GameSceneManager");
            GameSceneManager sceneManager = sceneManagerObj.AddComponent<GameSceneManager>();

            // Настраиваем через рефлексию, так как поля private
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(GameSceneManager).GetField("_mainMenuScene", flags)?.SetValue(sceneManager, _mainMenuSceneName);
            typeof(GameSceneManager).GetField("_gameScene", flags)?.SetValue(sceneManager, _gameSceneName);
            typeof(GameSceneManager).GetField("_useLoadingScreen", flags)?.SetValue(sceneManager, _useLoadingScreen);

            DontDestroyOnLoad(sceneManagerObj);
            Debug.Log("MenuInitializer: GameSceneManager создан и настроен");
        }
    }

    private void AutoConfigureMenuManager()
    {
        // Пытаемся найти Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject root = canvas != null ? canvas.gameObject : this.gameObject;

        // Проверяем / создаём MenuManager
        MenuManager menuManager = root.GetComponent<MenuManager>();
        if (menuManager == null)
        {
            menuManager = root.AddComponent<MenuManager>();
            Debug.Log("MenuInitializer: MenuManager добавлен на " + root.name);
        }

        // Находим UI объекты
        UnityEngine.UI.Button playBtn = FindButton(_playButtonName);
        UnityEngine.UI.Button charBtn = FindButton(_charactersButtonName);
        UnityEngine.UI.Button setBtn  = FindButton(_settingsButtonName);
        UnityEngine.UI.Button exitBtn = FindButton(_exitButtonName);

        GameObject mainMenuPanel = GameObject.Find(_mainMenuPanelName);
        GameObject charactersPanel = GameObject.Find(_charactersPanelName);
        GameObject settingsPanel = GameObject.Find(_settingsPanelName);

        // Настраиваем приватные поля через рефлексию
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        System.Type menuType = typeof(MenuManager);
        menuType.GetField("_playButton", flags)?.SetValue(menuManager, playBtn);
        menuType.GetField("_charactersButton", flags)?.SetValue(menuManager, charBtn);
        menuType.GetField("_settingsButton", flags)?.SetValue(menuManager, setBtn);
        menuType.GetField("_exitButton", flags)?.SetValue(menuManager, exitBtn);

        menuType.GetField("_mainMenuPanel", flags)?.SetValue(menuManager, mainMenuPanel);
        menuType.GetField("_charactersPanel", flags)?.SetValue(menuManager, charactersPanel);
        menuType.GetField("_settingsPanel", flags)?.SetValue(menuManager, settingsPanel);

        // Удостоверимся, что Game Scene Name соответствует
        menuType.GetField("_gameSceneName", flags)?.SetValue(menuManager, _gameSceneName);

        Debug.Log("MenuInitializer: MenuManager автоматически сконфигурирован");
    }

    private UnityEngine.UI.Button FindButton(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogWarning($"MenuInitializer: Не найден объект {name}");
            return null;
        }
        var btn = go.GetComponent<UnityEngine.UI.Button>();
        if (btn == null)
        {
            Debug.LogWarning($"MenuInitializer: На объекте {name} отсутствует компонент Button");
        }
        return btn;
    }
} 