using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    [Header("Названия сцен")]
    [SerializeField] private string _mainMenuScene = "MainMenu";
    [SerializeField] private string _gameScene = "Scene2";
    [SerializeField] private string _loadingScene = "Loading";
    
    [Header("Настройки загрузки")]
    [SerializeField] private bool _useLoadingScreen = true;
    [SerializeField] private float _minimumLoadingTime = 1.0f;
    
    // Синглтон
    public static GameSceneManager Instance { get; private set; }
    
    // События
    public System.Action<string> OnSceneLoadStarted;
    public System.Action<string> OnSceneLoadCompleted;
    public System.Action<float> OnLoadingProgress;
    
    private void Awake()
    {
        // Синглтон паттерн
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    #region Public Methods
    
    public void LoadMainMenu()
    {
        LoadScene(_mainMenuScene);
    }
    
    public void LoadGameScene()
    {
        LoadScene(_gameScene);
    }
    
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("GameSceneManager: Имя сцены не может быть пустым!");
            return;
        }
        
        Debug.Log($"GameSceneManager: Загрузка сцены {sceneName}");
        
        if (_useLoadingScreen && !string.IsNullOrEmpty(_loadingScene))
        {
            StartCoroutine(LoadSceneWithLoadingScreen(sceneName));
        }
        else
        {
            StartCoroutine(LoadSceneDirectly(sceneName));
        }
    }
    
    public void ReloadCurrentScene()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LoadScene(currentSceneName);
    }
    
    public void QuitGame()
    {
        Debug.Log("GameSceneManager: Выход из игры");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    #endregion
    
    #region Private Methods
    
    private IEnumerator LoadSceneWithLoadingScreen(string targetScene)
    {
        OnSceneLoadStarted?.Invoke(targetScene);
        
        // Загружаем экран загрузки
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_loadingScene);
        
        // Небольшая задержка для показа экрана загрузки
        yield return new WaitForSeconds(0.1f);
        
        // Начинаем асинхронную загрузку целевой сцены
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;
        
        float timer = 0f;
        
        // Ждем загрузки с отображением прогресса
        while (!asyncLoad.isDone)
        {
            timer += Time.deltaTime;
            
            // Прогресс загрузки (0.9 максимум до allowSceneActivation)
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            OnLoadingProgress?.Invoke(progress);
            
            // Активируем сцену после минимального времени загрузки
            if (asyncLoad.progress >= 0.9f && timer >= _minimumLoadingTime)
            {
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        OnSceneLoadCompleted?.Invoke(targetScene);
        Debug.Log($"GameSceneManager: Сцена {targetScene} успешно загружена");
    }
    
    private IEnumerator LoadSceneDirectly(string sceneName)
    {
        OnSceneLoadStarted?.Invoke(sceneName);
        
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            OnLoadingProgress?.Invoke(asyncLoad.progress);
            yield return null;
        }
        
        OnSceneLoadCompleted?.Invoke(sceneName);
        Debug.Log($"GameSceneManager: Сцена {sceneName} успешно загружена");
    }
    
    #endregion
    
    #region Utility Methods
    
    public string GetCurrentSceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    
    public bool IsInMainMenu()
    {
        return GetCurrentSceneName() == _mainMenuScene;
    }
    
    public bool IsInGame()
    {
        return GetCurrentSceneName() == _gameScene;
    }
    
    // Метод для возврата в меню из игры
    public void ReturnToMainMenu()
    {
        // Сохраняем прогресс если нужно
        SaveGameProgress();
        
        // Возвращаемся в главное меню
        LoadMainMenu();
    }
    
    private void SaveGameProgress()
    {
        // Здесь можно добавить логику сохранения прогресса
        Debug.Log("GameSceneManager: Сохраняем прогресс игры");
    }
    
    #endregion
    
    #region Input Handling
    
    private void Update()
    {
        // Быстрый возврат в меню по Escape (только в игре)
        if (Input.GetKeyDown(KeyCode.Escape) && IsInGame())
        {
            ReturnToMainMenu();
        }
    }
    
    #endregion
} 