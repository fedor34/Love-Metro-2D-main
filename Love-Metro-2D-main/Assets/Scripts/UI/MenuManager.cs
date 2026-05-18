using LoveMetro.UI;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _charactersButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _exitButton;

    [Header("Menu Panels")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _charactersPanel;
    [SerializeField] private GameObject _settingsPanel;

    [Header("Game Settings")]
    [SerializeField] private string _gameSceneName = "Scene2";

    [Header("Animation")]
    [SerializeField] private Animator _menuAnimator;

    private MenuPanelRouter _panelRouter;
    private IMenuSceneActions _sceneActions;

    public void Configure(
        Button playButton,
        Button charactersButton,
        Button settingsButton,
        Button exitButton,
        GameObject mainMenuPanel,
        GameObject charactersPanel,
        GameObject settingsPanel,
        string gameSceneName)
    {
        _playButton = playButton;
        _charactersButton = charactersButton;
        _settingsButton = settingsButton;
        _exitButton = exitButton;
        _mainMenuPanel = mainMenuPanel;
        _charactersPanel = charactersPanel;
        _settingsPanel = settingsPanel;

        if (!string.IsNullOrEmpty(gameSceneName))
            _gameSceneName = gameSceneName;

        RebuildPanelRouter();
    }

    public void Configure(
        SettingsPanel settingsPanel,
        CharactersPanel charactersPanel,
        Button startButton,
        Button settingsButton,
        Button exitButton)
    {
        if (settingsPanel != null)
        {
            settingsPanel.Configure(this);
            _settingsPanel = settingsPanel.gameObject;
        }

        if (charactersPanel != null)
        {
            charactersPanel.Configure(this);
            _charactersPanel = charactersPanel.gameObject;
        }

        if (startButton != null)
            _playButton = startButton;

        if (settingsButton != null)
            _settingsButton = settingsButton;

        if (exitButton != null)
            _exitButton = exitButton;

        RebuildPanelRouter();
        SetupButtonListeners();
    }

    private MenuPanelRouter PanelRouter
    {
        get
        {
            if (_panelRouter == null)
                RebuildPanelRouter();

            return _panelRouter;
        }
    }

    private IMenuSceneActions SceneActions
    {
        get
        {
            if (_sceneActions == null)
                _sceneActions = UnityMenuSceneActions.Instance;

            return _sceneActions;
        }
    }

    internal void ConfigureSceneActionsForTests(IMenuSceneActions sceneActions)
    {
        _sceneActions = sceneActions;
    }

    internal void InitializeForTests()
    {
        InitializeMenu();
        SetupButtonListeners();
    }

    private void Start()
    {
        GameBootstrap.EnsureRuntimeServices();
        EnsureGraphicRaycaster();
        InitializeMenu();
        SetupButtonListeners();
    }

    private void InitializeMenu()
    {
        ShowMainMenu();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void EnsureGraphicRaycaster()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void SetupButtonListeners()
    {
        BindButton(_playButton, OnPlayButtonClicked);
        BindButton(_charactersButton, OnCharactersButtonClicked);
        BindButton(_settingsButton, OnSettingsButtonClicked);
        BindButton(_exitButton, OnExitButtonClicked);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    public void OnPlayButtonClicked()
    {
        Debug.Log("MenuManager: starting game.");

        if (_menuAnimator != null)
        {
            _menuAnimator.SetTrigger("FadeOut");
            Invoke(nameof(LoadGameScene), 0.5f);
            return;
        }

        LoadGameScene();
    }

    public void OnCharactersButtonClicked()
    {
        Debug.Log("MenuManager: opening characters panel.");
        ShowCharactersPanel();
    }

    public void OnSettingsButtonClicked()
    {
        Debug.Log("MenuManager: opening settings panel.");
        ShowSettingsPanel();
    }

    public void OnExitButtonClicked()
    {
        Debug.Log("MenuManager: exiting game.");
        SceneActions.QuitGame();
    }

    public void ShowMainMenu()
    {
        PanelRouter.Show(MenuPanelId.Main);
    }

    public void ShowCharactersPanel()
    {
        PanelRouter.Show(MenuPanelId.Characters);
    }

    public void ShowSettingsPanel()
    {
        PanelRouter.Show(MenuPanelId.Settings);
    }

    public void BackToMainMenu()
    {
        ShowMainMenu();
    }

    private void LoadGameScene()
    {
        SceneActions.LoadGameScene(_gameSceneName);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !PanelRouter.IsVisible(MenuPanelId.Main))
            BackToMainMenu();

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) &&
            PanelRouter.IsVisible(MenuPanelId.Main))
        {
            OnPlayButtonClicked();
        }
    }

    private void RebuildPanelRouter()
    {
        _panelRouter = new MenuPanelRouter(_mainMenuPanel, _charactersPanel, _settingsPanel);
    }
}
