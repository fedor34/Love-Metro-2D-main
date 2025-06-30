using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("Звук")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Toggle _muteToggle;
    
    [Header("Графика")]
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private Toggle _vsyncToggle;
    
    [Header("Игра")]
    [SerializeField] private Slider _gameSpeedSlider;
    [SerializeField] private Toggle _debugModeToggle;
    
    [Header("Кнопки")]
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _defaultsButton;
    [SerializeField] private Button _backButton;
    
    // Ссылка на менеджер меню
    private MenuManager _menuManager;
    
    private void Start()
    {
        _menuManager = FindObjectOfType<MenuManager>();
        InitializeSettings();
        SetupButtonListeners();
        LoadSettings();
    }
    
    private void InitializeSettings()
    {
        // Инициализация качества графики
        if (_qualityDropdown != null)
        {
            _qualityDropdown.ClearOptions();
            _qualityDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Низкое", "Среднее", "Высокое", "Ультра"
            });
        }
        
        // Инициализация разрешений (базовые варианты)
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.ClearOptions();
            _resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "1280x720", "1920x1080", "2560x1440", "3840x2160"
            });
        }
    }
    
    private void SetupButtonListeners()
    {
        if (_applyButton != null)
            _applyButton.onClick.AddListener(ApplySettings);
            
        if (_defaultsButton != null)
            _defaultsButton.onClick.AddListener(ResetToDefaults);
            
        if (_backButton != null)
            _backButton.onClick.AddListener(BackToMenu);
        
        // Настройка слайдеров
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }
    
    #region Settings Handlers
    
    private void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        SaveSettings();
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        // Здесь можно настроить громкость музыки через AudioMixer
        SaveSettings();
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        // Здесь можно настроить громкость звуковых эффектов
        SaveSettings();
    }
    
    public void ApplySettings()
    {
        // Применяем настройки графики
        if (_qualityDropdown != null)
        {
            QualitySettings.SetQualityLevel(_qualityDropdown.value);
        }
        
        if (_fullscreenToggle != null)
        {
            Screen.fullScreen = _fullscreenToggle.isOn;
        }
        
        if (_vsyncToggle != null)
        {
            QualitySettings.vSyncCount = _vsyncToggle.isOn ? 1 : 0;
        }
        
        SaveSettings();
        Debug.Log("SettingsPanel: Настройки применены");
    }
    
    public void ResetToDefaults()
    {
        // Сброс к стандартным значениям
        if (_masterVolumeSlider != null) _masterVolumeSlider.value = 1.0f;
        if (_musicVolumeSlider != null) _musicVolumeSlider.value = 0.8f;
        if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = 1.0f;
        if (_muteToggle != null) _muteToggle.isOn = false;
        
        if (_qualityDropdown != null) _qualityDropdown.value = 2; // Высокое
        if (_fullscreenToggle != null) _fullscreenToggle.isOn = true;
        if (_vsyncToggle != null) _vsyncToggle.isOn = true;
        
        if (_gameSpeedSlider != null) _gameSpeedSlider.value = 1.0f;
        if (_debugModeToggle != null) _debugModeToggle.isOn = false;
        
        ApplySettings();
        Debug.Log("SettingsPanel: Настройки сброшены к умолчанию");
    }
    
    public void BackToMenu()
    {
        if (_menuManager != null)
        {
            _menuManager.BackToMainMenu();
        }
    }
    
    #endregion
    
    #region Save/Load Settings
    
    private void SaveSettings()
    {
        // Сохранение настроек в PlayerPrefs
        if (_masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", _masterVolumeSlider.value);
            
        if (_musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", _musicVolumeSlider.value);
            
        if (_sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolumeSlider.value);
            
        if (_muteToggle != null)
            PlayerPrefs.SetInt("Mute", _muteToggle.isOn ? 1 : 0);
            
        if (_qualityDropdown != null)
            PlayerPrefs.SetInt("Quality", _qualityDropdown.value);
            
        if (_fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", _fullscreenToggle.isOn ? 1 : 0);
            
        if (_vsyncToggle != null)
            PlayerPrefs.SetInt("VSync", _vsyncToggle.isOn ? 1 : 0);
            
        if (_gameSpeedSlider != null)
            PlayerPrefs.SetFloat("GameSpeed", _gameSpeedSlider.value);
            
        if (_debugModeToggle != null)
            PlayerPrefs.SetInt("DebugMode", _debugModeToggle.isOn ? 1 : 0);
        
        PlayerPrefs.Save();
    }
    
    private void LoadSettings()
    {
        // Загрузка настроек из PlayerPrefs
        if (_masterVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            _masterVolumeSlider.value = volume;
            AudioListener.volume = volume;
        }
        
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            
        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            
        if (_muteToggle != null)
            _muteToggle.isOn = PlayerPrefs.GetInt("Mute", 0) == 1;
            
        if (_qualityDropdown != null)
        {
            int quality = PlayerPrefs.GetInt("Quality", 2);
            _qualityDropdown.value = quality;
            QualitySettings.SetQualityLevel(quality);
        }
        
        if (_fullscreenToggle != null)
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            _fullscreenToggle.isOn = fullscreen;
            Screen.fullScreen = fullscreen;
        }
        
        if (_vsyncToggle != null)
        {
            bool vsync = PlayerPrefs.GetInt("VSync", 1) == 1;
            _vsyncToggle.isOn = vsync;
            QualitySettings.vSyncCount = vsync ? 1 : 0;
        }
        
        if (_gameSpeedSlider != null)
            _gameSpeedSlider.value = PlayerPrefs.GetFloat("GameSpeed", 1.0f);
            
        if (_debugModeToggle != null)
            _debugModeToggle.isOn = PlayerPrefs.GetInt("DebugMode", 0) == 1;
    }
    
    #endregion
} 