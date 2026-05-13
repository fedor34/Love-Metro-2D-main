using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _charactersButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _exitButton;
    
    [Header("Панели меню")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _charactersPanel;
    [SerializeField] private GameObject _settingsPanel;
    
    [Header("Настройки игры")]
    [SerializeField] private string _gameSceneName = "Scene2";
    
    [Header("Анимация")]
    [SerializeField] private Animator _menuAnimator;

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
    }
    
    private void Start()
    {
        // На всякий случай убедимся, что UI кликается
        GameBootstrap.EnsureRuntimeServices();

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        InitializeMenu();
        SetupButtonListeners();
    }
    
    private void InitializeMenu()
    {
        // Автоматически ищем панели при необходимости
        // Показываем главное меню
        ShowMainMenu();
        
        // Настройка курсора
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    private void SetupButtonListeners()
    {
        // Если ссылки не назначены в инспекторе, пытаемся найти по имени
        if (_playButton != null)
        {
            _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            _playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        if (_charactersButton != null)
        {
            _charactersButton.onClick.RemoveListener(OnCharactersButtonClicked);
            _charactersButton.onClick.AddListener(OnCharactersButtonClicked);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(OnExitButtonClicked);
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }

    #region Button Handlers
    
    public void OnPlayButtonClicked()
    {
        Debug.Log("MenuManager: Начинаем игру!");
        
        // Анимация перехода (если есть)
        if (_menuAnimator != null)
        {
            _menuAnimator.SetTrigger("FadeOut");
            // Задержка для проигрывания анимации
            Invoke(nameof(LoadGameScene), 0.5f);
        }
        else
        {
            LoadGameScene();
        }
    }
    
    public void OnCharactersButtonClicked()
    {
        Debug.Log("MenuManager: Открываем панель персонажей");
        ShowCharactersPanel();
    }
    
    public void OnSettingsButtonClicked()
    {
        Debug.Log("MenuManager: Открываем настройки");
        ShowSettingsPanel();
    }
    
    public void OnExitButtonClicked()
    {
        Debug.Log("MenuManager: Выход из игры");
        
        // Используем GameSceneManager если он доступен
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.QuitGame();
        }
        else
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
    
    #endregion
    
    #region Panel Management
    
    public void ShowMainMenu()
    {
        SetActivePanel(_mainMenuPanel);
    }
    
    public void ShowCharactersPanel()
    {
        SetActivePanel(_charactersPanel);
    }
    
    public void ShowSettingsPanel()
    {
        SetActivePanel(_settingsPanel);
    }
    
    public void BackToMainMenu()
    {
        ShowMainMenu();
    }
    
    private void SetActivePanel(GameObject targetPanel)
    {
        // Скрываем все панели
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
        if (_charactersPanel != null) _charactersPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        
        // Показываем целевую панель
        if (targetPanel != null)
            targetPanel.SetActive(true);
    }
    
    #endregion
    
    #region Scene Loading
    
    private void LoadGameScene()
    {
        // Используем GameSceneManager если он доступен
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadGameScene();
        }
        else if (!string.IsNullOrEmpty(_gameSceneName))
        {
            SceneManager.LoadScene(_gameSceneName);
        }
        else
        {
            Debug.LogError("MenuManager: Имя игровой сцены не задано!");
        }
    }
    
    #endregion
    
    #region Input Handling
    
    private void Update()
    {
        // ESC для возврата в главное меню
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_mainMenuPanel != null && !_mainMenuPanel.activeInHierarchy)
            {
                BackToMainMenu();
            }
        }
        
        // Enter для быстрого старта игры
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (_mainMenuPanel != null && _mainMenuPanel.activeInHierarchy)
            {
                OnPlayButtonClicked();
            }
        }
    }
    
    #endregion
}
