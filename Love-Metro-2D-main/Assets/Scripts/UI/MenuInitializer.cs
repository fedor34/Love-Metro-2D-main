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

        GameBootstrap.EnsureRuntimeServices();
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
            sceneManager.Configure(_mainMenuSceneName, _gameSceneName, _useLoadingScreen);

            DontDestroyOnLoad(sceneManagerObj);
            Debug.Log("MenuInitializer: GameSceneManager создан и настроен");
            return;
        }

        GameSceneManager.Instance.Configure(_mainMenuSceneName, _gameSceneName, _useLoadingScreen);
    }

    private void AutoConfigureMenuManager()
    {
        // Пытаемся найти Canvas
        Canvas canvas = FindMenuCanvas();
        GameObject root = canvas != null ? canvas.gameObject : this.gameObject;

        // Проверяем / создаём MenuManager
        MenuManager menuManager = root.GetComponent<MenuManager>();
        if (menuManager == null)
        {
            menuManager = root.AddComponent<MenuManager>();
            Debug.Log("MenuInitializer: MenuManager добавлен на " + root.name);
        }

        // Находим UI объекты
        UnityEngine.UI.Button playBtn = FindButton(_playButtonName, root);
        UnityEngine.UI.Button charBtn = FindButton(_charactersButtonName, root);
        UnityEngine.UI.Button setBtn  = FindButton(_settingsButtonName, root);
        UnityEngine.UI.Button exitBtn = FindButton(_exitButtonName, root);

        GameObject mainMenuPanel = FindSceneObject(_mainMenuPanelName, root);
        GameObject charactersPanel = FindSceneObject(_charactersPanelName, root);
        GameObject settingsPanel = FindSceneObject(_settingsPanelName, root);

        menuManager.Configure(
            playBtn,
            charBtn,
            setBtn,
            exitBtn,
            mainMenuPanel,
            charactersPanel,
            settingsPanel,
            _gameSceneName);

        SettingsPanel settingsPanelComponent = settingsPanel != null ? settingsPanel.GetComponent<SettingsPanel>() : null;
        CharactersPanel charactersPanelComponent = charactersPanel != null ? charactersPanel.GetComponent<CharactersPanel>() : null;
        menuManager.Configure(settingsPanelComponent, charactersPanelComponent, playBtn, setBtn, exitBtn);

        Debug.Log("MenuInitializer: MenuManager автоматически сконфигурирован");
    }

    private Canvas FindMenuCanvas()
    {
        Canvas localCanvas = GetComponent<Canvas>();
        if (IsMenuCanvas(localCanvas))
            return localCanvas;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (IsMenuCanvas(parentCanvas))
            return parentCanvas;

        Canvas childCanvas = GetComponentInChildren<Canvas>(true);
        if (IsMenuCanvas(childCanvas))
            return childCanvas;

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas fallback = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.name == "ScoreHudCanvas")
                continue;

            if (canvas.GetComponentInChildren<UnityEngine.UI.Button>() != null)
                return canvas;

            if (fallback == null)
                fallback = canvas;
        }

        return fallback;
    }

    private bool IsMenuCanvas(Canvas canvas)
    {
        return canvas != null && canvas.name != "ScoreHudCanvas";
    }

    private UnityEngine.UI.Button FindButton(string name, GameObject preferredRoot)
    {
        GameObject go = FindSceneObject(name, preferredRoot);
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

    private GameObject FindSceneObject(string name, GameObject preferredRoot = null)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (preferredRoot != null)
        {
            foreach (Transform childTransform in preferredRoot.GetComponentsInChildren<Transform>(true))
            {
                if (childTransform != null && childTransform.name == name)
                    return childTransform.gameObject;
            }
        }

        GameObject activeObject = GameObject.Find(name);
        if (activeObject != null)
            return activeObject;

        foreach (Transform sceneTransform in FindObjectsOfType<Transform>(true))
        {
            if (sceneTransform != null && sceneTransform.name == name)
                return sceneTransform.gameObject;
        }

        return null;
    }
}
