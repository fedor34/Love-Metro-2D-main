using System.Collections.Generic;
using LoveMetro.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Toggle _muteToggle;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private Toggle _vsyncToggle;

    [Header("Game")]
    [SerializeField] private Slider _gameSpeedSlider;
    [SerializeField] private Toggle _debugModeToggle;

    [Header("Buttons")]
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _defaultsButton;
    [SerializeField] private Button _backButton;

    [SerializeField] private MenuManager _menuManager;

    private ISettingsStore _settingsStore;
    private SettingsApplier _settingsApplier;
    private bool _syncingControls;

    public void Configure(MenuManager menuManager)
    {
        if (menuManager != null)
            _menuManager = menuManager;
    }

    private ISettingsStore SettingsStore
    {
        get
        {
            if (_settingsStore == null)
                _settingsStore = new PlayerPrefsSettingsStore();

            return _settingsStore;
        }
    }

    private SettingsApplier SettingsApplier
    {
        get
        {
            if (_settingsApplier == null)
                _settingsApplier = new SettingsApplier();

            return _settingsApplier;
        }
    }

    internal void ConfigureForTests(ISettingsStore settingsStore, SettingsApplier settingsApplier)
    {
        if (settingsStore != null)
            _settingsStore = settingsStore;

        if (settingsApplier != null)
            _settingsApplier = settingsApplier;
    }

    internal void InitializeForTests()
    {
        InitializeSettings();
        SetupButtonListeners();
        LoadSettings();
    }

    private void Start()
    {
        InitializeSettings();
        SetupButtonListeners();
        LoadSettings();
    }

    private void InitializeSettings()
    {
        if (_qualityDropdown != null)
        {
            _qualityDropdown.ClearOptions();
            _qualityDropdown.AddOptions(new List<string>
            {
                "\u041D\u0438\u0437\u043A\u043E\u0435",
                "\u0421\u0440\u0435\u0434\u043D\u0435\u0435",
                "\u0412\u044B\u0441\u043E\u043A\u043E\u0435",
                "\u0423\u043B\u044C\u0442\u0440\u0430"
            });
        }

        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.ClearOptions();
            _resolutionDropdown.AddOptions(new List<string>
            {
                "1280x720",
                "1920x1080",
                "2560x1440",
                "3840x2160"
            });
        }
    }

    private void SetupButtonListeners()
    {
        BindButton(_applyButton, ApplySettings);
        BindButton(_defaultsButton, ResetToDefaults);
        BindButton(_backButton, BackToMenu);

        BindSlider(_masterVolumeSlider, OnMasterVolumeChanged);
        BindSlider(_musicVolumeSlider, OnMusicVolumeChanged);
        BindSlider(_sfxVolumeSlider, OnSFXVolumeChanged);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null)
            return;

        slider.onValueChanged.RemoveListener(action);
        slider.onValueChanged.AddListener(action);
    }

    private void OnMasterVolumeChanged(float value)
    {
        SaveAndApplyCurrentSettings();
    }

    private void OnMusicVolumeChanged(float value)
    {
        SaveAndApplyCurrentSettings();
    }

    private void OnSFXVolumeChanged(float value)
    {
        SaveAndApplyCurrentSettings();
    }

    public void ApplySettings()
    {
        SaveAndApplyCurrentSettings();
        Debug.Log("SettingsPanel: settings applied.");
    }

    public void ResetToDefaults()
    {
        SettingsSnapshot defaults = SettingsSnapshot.Defaults;
        ApplySnapshotToControls(defaults);
        SaveAndApply(defaults);
        Debug.Log("SettingsPanel: settings reset to defaults.");
    }

    public void BackToMenu()
    {
        if (_menuManager != null)
            _menuManager.BackToMainMenu();
    }

    private void SaveAndApplyCurrentSettings()
    {
        if (_syncingControls)
            return;

        SaveAndApply(ReadSnapshotFromControls(SettingsStore.Load()));
    }

    private void SaveAndApply(SettingsSnapshot settings)
    {
        SettingsStore.Save(settings);
        SettingsApplier.Apply(settings);
    }

    private SettingsSnapshot ReadSnapshotFromControls(SettingsSnapshot fallback)
    {
        float masterVolume = _masterVolumeSlider != null ? _masterVolumeSlider.value : fallback.MasterVolume;
        float musicVolume = _musicVolumeSlider != null ? _musicVolumeSlider.value : fallback.MusicVolume;
        float sfxVolume = _sfxVolumeSlider != null ? _sfxVolumeSlider.value : fallback.SfxVolume;
        bool mute = _muteToggle != null ? _muteToggle.isOn : fallback.Mute;
        int quality = _qualityDropdown != null ? _qualityDropdown.value : fallback.Quality;
        bool fullscreen = _fullscreenToggle != null ? _fullscreenToggle.isOn : fallback.Fullscreen;
        bool vSync = _vsyncToggle != null ? _vsyncToggle.isOn : fallback.VSync;
        float gameSpeed = _gameSpeedSlider != null ? _gameSpeedSlider.value : fallback.GameSpeed;
        bool debugMode = _debugModeToggle != null ? _debugModeToggle.isOn : fallback.DebugMode;

        return new SettingsSnapshot(
            masterVolume,
            musicVolume,
            sfxVolume,
            mute,
            quality,
            fullscreen,
            vSync,
            gameSpeed,
            debugMode);
    }

    private void LoadSettings()
    {
        SettingsSnapshot settings = SettingsStore.Load();
        ApplySnapshotToControls(settings);
        SettingsApplier.Apply(settings);
    }

    private void ApplySnapshotToControls(SettingsSnapshot settings)
    {
        _syncingControls = true;
        try
        {
            if (_masterVolumeSlider != null) _masterVolumeSlider.value = settings.MasterVolume;
            if (_musicVolumeSlider != null) _musicVolumeSlider.value = settings.MusicVolume;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = settings.SfxVolume;
            if (_muteToggle != null) _muteToggle.isOn = settings.Mute;
            if (_qualityDropdown != null) _qualityDropdown.value = settings.Quality;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = settings.Fullscreen;
            if (_vsyncToggle != null) _vsyncToggle.isOn = settings.VSync;
            if (_gameSpeedSlider != null) _gameSpeedSlider.value = settings.GameSpeed;
            if (_debugModeToggle != null) _debugModeToggle.isOn = settings.DebugMode;

            _qualityDropdown?.RefreshShownValue();
            _resolutionDropdown?.RefreshShownValue();
        }
        finally
        {
            _syncingControls = false;
        }
    }
}
