using System.Collections.Generic;
using LoveMetro.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    private const float VolumeStep = 0.05f;
    private const float GameSpeedStep = 0.1f;

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

    [Header("Step Buttons")]
    [SerializeField] private Button _masterVolumeDecreaseButton;
    [SerializeField] private Button _masterVolumeIncreaseButton;
    [SerializeField] private Button _musicVolumeDecreaseButton;
    [SerializeField] private Button _musicVolumeIncreaseButton;
    [SerializeField] private Button _sfxVolumeDecreaseButton;
    [SerializeField] private Button _sfxVolumeIncreaseButton;
    [SerializeField] private Button _gameSpeedDecreaseButton;
    [SerializeField] private Button _gameSpeedIncreaseButton;
    [SerializeField] private Button _qualityPreviousButton;
    [SerializeField] private Button _qualityNextButton;
    [SerializeField] private Button _resolutionPreviousButton;
    [SerializeField] private Button _resolutionNextButton;

    [Header("Value Labels")]
    [SerializeField] private TextMeshProUGUI _masterVolumeValueText;
    [SerializeField] private TextMeshProUGUI _musicVolumeValueText;
    [SerializeField] private TextMeshProUGUI _sfxVolumeValueText;
    [SerializeField] private TextMeshProUGUI _gameSpeedValueText;

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
        InitializeControls();
    }

    private void OnEnable()
    {
        InitializeControls();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
            ApplyResponsiveLayout();
    }

    private void InitializeControls()
    {
        ApplyResponsiveLayout();
        InitializeSettings();
        SetupButtonListeners();
        LoadSettings();
    }

    private void ApplyResponsiveLayout()
    {
        SettingsPanelLayout.Apply(transform as RectTransform);
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
        BindButton(_masterVolumeDecreaseButton, DecreaseMasterVolume);
        BindButton(_masterVolumeIncreaseButton, IncreaseMasterVolume);
        BindButton(_musicVolumeDecreaseButton, DecreaseMusicVolume);
        BindButton(_musicVolumeIncreaseButton, IncreaseMusicVolume);
        BindButton(_sfxVolumeDecreaseButton, DecreaseSfxVolume);
        BindButton(_sfxVolumeIncreaseButton, IncreaseSfxVolume);
        BindButton(_gameSpeedDecreaseButton, DecreaseGameSpeed);
        BindButton(_gameSpeedIncreaseButton, IncreaseGameSpeed);
        BindButton(_qualityPreviousButton, PreviousQuality);
        BindButton(_qualityNextButton, NextQuality);
        BindButton(_resolutionPreviousButton, PreviousResolution);
        BindButton(_resolutionNextButton, NextResolution);

        BindSlider(_masterVolumeSlider, OnMasterVolumeChanged);
        BindSlider(_musicVolumeSlider, OnMusicVolumeChanged);
        BindSlider(_sfxVolumeSlider, OnSFXVolumeChanged);
        BindSlider(_gameSpeedSlider, OnGameSpeedChanged);
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
        UpdateValueLabels();
        SaveAndApplyCurrentSettings();
    }

    private void OnMusicVolumeChanged(float value)
    {
        UpdateValueLabels();
        SaveAndApplyCurrentSettings();
    }

    private void OnSFXVolumeChanged(float value)
    {
        UpdateValueLabels();
        SaveAndApplyCurrentSettings();
    }

    private void OnGameSpeedChanged(float value)
    {
        UpdateValueLabels();
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

    public void DecreaseMasterVolume()
    {
        AdjustSlider(_masterVolumeSlider, -VolumeStep);
    }

    public void IncreaseMasterVolume()
    {
        AdjustSlider(_masterVolumeSlider, VolumeStep);
    }

    public void DecreaseMusicVolume()
    {
        AdjustSlider(_musicVolumeSlider, -VolumeStep);
    }

    public void IncreaseMusicVolume()
    {
        AdjustSlider(_musicVolumeSlider, VolumeStep);
    }

    public void DecreaseSfxVolume()
    {
        AdjustSlider(_sfxVolumeSlider, -VolumeStep);
    }

    public void IncreaseSfxVolume()
    {
        AdjustSlider(_sfxVolumeSlider, VolumeStep);
    }

    public void DecreaseGameSpeed()
    {
        AdjustSlider(_gameSpeedSlider, -GameSpeedStep);
    }

    public void IncreaseGameSpeed()
    {
        AdjustSlider(_gameSpeedSlider, GameSpeedStep);
    }

    public void PreviousQuality()
    {
        StepDropdown(_qualityDropdown, -1);
    }

    public void NextQuality()
    {
        StepDropdown(_qualityDropdown, 1);
    }

    public void PreviousResolution()
    {
        StepDropdown(_resolutionDropdown, -1);
    }

    public void NextResolution()
    {
        StepDropdown(_resolutionDropdown, 1);
    }

    private void SaveAndApplyCurrentSettings()
    {
        if (_syncingControls)
            return;

        SaveAndApply(ReadSnapshotFromControls(SettingsStore.Load()));
    }

    private void AdjustSlider(Slider slider, float delta)
    {
        if (slider == null)
            return;

        float value = Mathf.Clamp(slider.value + delta, slider.minValue, slider.maxValue);
        slider.SetValueWithoutNotify(value);
        UpdateValueLabels();
        SaveAndApplyCurrentSettings();
    }

    private void StepDropdown(TMP_Dropdown dropdown, int delta)
    {
        if (dropdown == null || dropdown.options.Count == 0)
            return;

        dropdown.value = Mathf.Clamp(dropdown.value + delta, 0, dropdown.options.Count - 1);
        dropdown.RefreshShownValue();
        SaveAndApplyCurrentSettings();
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
            UpdateValueLabels();
        }
        finally
        {
            _syncingControls = false;
        }
    }

    private void UpdateValueLabels()
    {
        if (_masterVolumeValueText != null)
            _masterVolumeValueText.text = ToPercentText(_masterVolumeSlider);

        if (_musicVolumeValueText != null)
            _musicVolumeValueText.text = ToPercentText(_musicVolumeSlider);

        if (_sfxVolumeValueText != null)
            _sfxVolumeValueText.text = ToPercentText(_sfxVolumeSlider);

        if (_gameSpeedValueText != null && _gameSpeedSlider != null)
            _gameSpeedValueText.text = _gameSpeedSlider.value.ToString("0.0") + "X";
    }

    private static string ToPercentText(Slider slider)
    {
        return slider != null ? Mathf.RoundToInt(slider.value * 100f).ToString() : string.Empty;
    }
}
