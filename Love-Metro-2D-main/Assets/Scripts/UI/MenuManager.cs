using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
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
    
    private void Start()
    {
        InitializeMenu();
        SetupButtonListeners();
    }
    
    private void InitializeMenu()
    {
        // Автоматически ищем панели при необходимости
        if (_mainMenuPanel == null)
            _mainMenuPanel = GameObject.Find("MainMenuPanel");
        if (_charactersPanel == null)
            _charactersPanel = GameObject.Find("CharactersPanel");
        if (_settingsPanel == null)
            _settingsPanel = GameObject.Find("SettingsPanel");

        // Показываем главное меню
        ShowMainMenu();
        
        // Настройка курсора
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    private void SetupButtonListeners()
    {
        // Если ссылки не назначены в инспекторе, пытаемся найти по имени
        if (_playButton == null)
            _playButton = FindButtonByName("PlayButton");
        if (_charactersButton == null)
            _charactersButton = FindButtonByName("CharactersButton");
        if (_settingsButton == null)
            _settingsButton = FindButtonByName("SettingsButton");
        if (_exitButton == null)
            _exitButton = FindButtonByName("ExitButton");

        if (_playButton != null)
            _playButton.onClick.AddListener(OnPlayButtonClicked);

        if (_charactersButton != null)
            _charactersButton.onClick.AddListener(OnCharactersButtonClicked);

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (_exitButton != null)
            _exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private UnityEngine.UI.Button FindButtonByName(string name)
    {
        // Сначала ищем по точному имени
        GameObject go = GameObject.Find(name);
        if (go != null)
            return go.GetComponent<UnityEngine.UI.Button>();

        // Если не нашли, пытаемся найти среди всех кнопок по тексту
        foreach (var btn in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>())
        {
            var txt = btn.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null && (txt.text.Equals("ИГРАТЬ", System.StringComparison.OrdinalIgnoreCase) && name == "PlayButton" ||
                                txt.text.Equals("ЧЕЛОВЕЧКИ", System.StringComparison.OrdinalIgnoreCase) && name == "CharactersButton" ||
                                txt.text.Equals("НАСТРОЙКИ", System.StringComparison.OrdinalIgnoreCase) && name == "SettingsButton" ||
                                txt.text.Equals("ВЫХОД", System.StringComparison.OrdinalIgnoreCase) && name == "ExitButton"))
            {
                return btn;
            }
        }
        return null;
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