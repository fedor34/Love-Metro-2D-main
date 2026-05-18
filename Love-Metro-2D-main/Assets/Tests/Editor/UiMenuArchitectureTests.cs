using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LoveMetro.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiMenuArchitectureTests
{
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
                Object.DestroyImmediate(_createdObjects[i]);
        }

        _createdObjects.Clear();
    }

    [Test]
    public void MenuPanelRouter_ShowsOnlyRequestedPanel()
    {
        GameObject main = CreateGameObject("Main");
        GameObject characters = CreateGameObject("Characters");
        GameObject settings = CreateGameObject("Settings");
        var router = new MenuPanelRouter(main, characters, settings);

        router.Show(MenuPanelId.Settings);

        Assert.IsFalse(main.activeSelf);
        Assert.IsFalse(characters.activeSelf);
        Assert.IsTrue(settings.activeSelf);
        Assert.IsTrue(router.IsVisible(MenuPanelId.Settings));

        router.Show(MenuPanelId.Main);

        Assert.IsTrue(main.activeSelf);
        Assert.IsFalse(characters.activeSelf);
        Assert.IsFalse(settings.activeSelf);
    }

    [Test]
    public void CharacterSelectionModel_HandlesEmptyAndWrapsNavigation()
    {
        var empty = new CharacterSelectionModel(0);
        Assert.IsFalse(empty.TrySelectNext(out int emptyNext));
        Assert.AreEqual(-1, emptyNext);
        Assert.IsFalse(empty.HasSelection);

        var model = new CharacterSelectionModel(2);
        Assert.AreEqual(0, model.CurrentIndex);

        Assert.IsTrue(model.TrySelectPrevious(out int previous));
        Assert.AreEqual(1, previous);

        Assert.IsTrue(model.TrySelectNext(out int next));
        Assert.AreEqual(0, next);

        Assert.IsFalse(model.TrySelect(4, out int invalid));
        Assert.AreEqual(0, invalid);
    }

    [Test]
    public void SettingsSnapshot_CanRoundTripThroughMemoryStore()
    {
        var store = new MemorySettingsStore(SettingsSnapshot.Defaults);
        var custom = new SettingsSnapshot(0.4f, 0.5f, 0.6f, true, 1, false, false, 0.75f, true);

        store.Save(custom);

        Assert.AreEqual(custom, store.Load());
        Assert.AreEqual(1, store.SaveCount);
    }

    [Test]
    public void MenuManager_ButtonBindingsAreIdempotentAndRoutePanels()
    {
        GameObject menuObject = CreateGameObject("Menu", typeof(MenuManager));
        MenuManager menuManager = menuObject.GetComponent<MenuManager>();
        Button playButton = CreateButton("Play");
        Button charactersButton = CreateButton("CharactersButton");
        Button settingsButton = CreateButton("SettingsButton");
        Button exitButton = CreateButton("ExitButton");
        GameObject mainPanel = CreateGameObject("MainPanel");
        GameObject charactersPanel = CreateGameObject("CharactersPanel");
        GameObject settingsPanel = CreateGameObject("SettingsPanel");
        var sceneActions = new TestMenuSceneActions();

        menuManager.Configure(
            playButton,
            charactersButton,
            settingsButton,
            exitButton,
            mainPanel,
            charactersPanel,
            settingsPanel,
            "Scene2");
        menuManager.ConfigureSceneActionsForTests(sceneActions);

        menuManager.InitializeForTests();
        menuManager.InitializeForTests();

        playButton.onClick.Invoke();

        Assert.AreEqual(1, sceneActions.LoadGameSceneCount);
        Assert.AreEqual("Scene2", sceneActions.LastFallbackSceneName);

        charactersButton.onClick.Invoke();

        Assert.IsFalse(mainPanel.activeSelf);
        Assert.IsTrue(charactersPanel.activeSelf);
        Assert.IsFalse(settingsPanel.activeSelf);

        settingsButton.onClick.Invoke();

        Assert.IsFalse(mainPanel.activeSelf);
        Assert.IsFalse(charactersPanel.activeSelf);
        Assert.IsTrue(settingsPanel.activeSelf);
    }

    [Test]
    public void MenuInitializer_ConfiguresInactiveSettingsPanel()
    {
        GameObject canvasObject = CreateGameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(MenuInitializer));
        GameObject mainPanel = CreateGameObject("MainMenuPanel");
        GameObject settingsPanel = CreateGameObject("SettingsPanel", typeof(SettingsPanel));
        CreateButton("PlayButton");
        CreateButton("CharactersButton");
        CreateButton("SettingsButton");
        CreateButton("ExitButton");
        mainPanel.transform.SetParent(canvasObject.transform);
        settingsPanel.transform.SetParent(canvasObject.transform);
        settingsPanel.SetActive(false);
        MenuInitializer initializer = canvasObject.GetComponent<MenuInitializer>();

        typeof(MenuInitializer)
            .GetMethod("AutoConfigureMenuManager", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(initializer, null);

        MenuManager menuManager = canvasObject.GetComponent<MenuManager>();
        FieldInfo settingsField = typeof(MenuManager).GetField("_settingsPanel", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(menuManager);
        Assert.AreSame(settingsPanel, settingsField.GetValue(menuManager));
    }

    [Test]
    public void SettingsPanel_ResetAndApplyUseConfiguredStoreAndApplier()
    {
        GameObject panelObject = CreateGameObject("SettingsPanel", typeof(SettingsPanel));
        SettingsPanel panel = panelObject.GetComponent<SettingsPanel>();
        Slider master = CreateComponentObject<Slider>("MasterVolume");
        Slider music = CreateComponentObject<Slider>("MusicVolume");
        Slider sfx = CreateComponentObject<Slider>("SfxVolume");
        Toggle fullscreen = CreateComponentObject<Toggle>("Fullscreen");
        Toggle vSync = CreateComponentObject<Toggle>("VSync");
        var store = new MemorySettingsStore(new SettingsSnapshot(0.2f, 0.3f, 0.4f, true, 1, false, false, 0.9f, true));
        var applier = new TestSettingsApplier();

        SetPrivateField(panel, "_masterVolumeSlider", master);
        SetPrivateField(panel, "_musicVolumeSlider", music);
        SetPrivateField(panel, "_sfxVolumeSlider", sfx);
        SetPrivateField(panel, "_fullscreenToggle", fullscreen);
        SetPrivateField(panel, "_vsyncToggle", vSync);
        panel.ConfigureForTests(store, applier);

        panel.InitializeForTests();

        Assert.AreEqual(0.2f, master.value);
        Assert.AreEqual(0.3f, music.value);
        Assert.AreEqual(0.4f, sfx.value);
        Assert.IsFalse(fullscreen.isOn);
        Assert.IsFalse(vSync.isOn);
        Assert.AreEqual(1, applier.ApplyCount);

        master.value = 0.55f;
        panel.ApplySettings();

        Assert.AreEqual(0.55f, store.Current.MasterVolume);
        Assert.AreEqual(store.Current, applier.LastApplied);

        panel.ResetToDefaults();

        Assert.AreEqual(SettingsSnapshot.Defaults, store.Current);
        Assert.AreEqual(SettingsSnapshot.Defaults, applier.LastApplied);
    }

    [Test]
    public void SettingsPanel_StepControlsAdjustValuesAndPersist()
    {
        GameObject panelObject = CreateGameObject("SettingsPanel", typeof(SettingsPanel));
        SettingsPanel panel = panelObject.GetComponent<SettingsPanel>();
        Slider master = CreateComponentObject<Slider>("MasterVolume");
        Slider speed = CreateComponentObject<Slider>("GameSpeed");
        TMP_Dropdown quality = CreateComponentObject<TMP_Dropdown>("Quality");
        Button masterDecrease = CreateButton("MasterDecrease");
        Button speedIncrease = CreateButton("SpeedIncrease");
        Button qualityNext = CreateButton("QualityNext");
        var store = new MemorySettingsStore(SettingsSnapshot.Defaults);
        var applier = new TestSettingsApplier();

        speed.minValue = 0.5f;
        speed.maxValue = 2f;
        SetPrivateField(panel, "_masterVolumeSlider", master);
        SetPrivateField(panel, "_gameSpeedSlider", speed);
        SetPrivateField(panel, "_qualityDropdown", quality);
        SetPrivateField(panel, "_masterVolumeDecreaseButton", masterDecrease);
        SetPrivateField(panel, "_gameSpeedIncreaseButton", speedIncrease);
        SetPrivateField(panel, "_qualityNextButton", qualityNext);
        panel.ConfigureForTests(store, applier);

        panel.InitializeForTests();
        panel.InitializeForTests();
        masterDecrease.onClick.Invoke();
        speedIncrease.onClick.Invoke();
        qualityNext.onClick.Invoke();

        Assert.AreEqual(0.95f, master.value, 0.001f);
        Assert.AreEqual(1.1f, speed.value, 0.001f);
        Assert.AreEqual(3, quality.value);
        Assert.AreEqual(0.95f, store.Current.MasterVolume, 0.001f);
        Assert.AreEqual(1.1f, store.Current.GameSpeed, 0.001f);
        Assert.AreEqual(3, store.Current.Quality);
        Assert.AreEqual(store.Current, applier.LastApplied);
    }

    [Test]
    public void CharactersPanel_EmptyDataNavigationIsSafe()
    {
        GameObject panelObject = CreateGameObject("CharactersPanel", typeof(CharactersPanel));
        CharactersPanel panel = panelObject.GetComponent<CharactersPanel>();

        Assert.DoesNotThrow(() => panel.SelectCharacter(0));
        Assert.DoesNotThrow(panel.SelectNextCharacter);
        Assert.DoesNotThrow(panel.SelectPreviousCharacter);
    }

    [Test]
    public void CharactersPanel_NavigationWrapsAndUpdatesDetails()
    {
        GameObject panelObject = CreateGameObject("CharactersPanel", typeof(CharactersPanel));
        CharactersPanel panel = panelObject.GetComponent<CharactersPanel>();
        TextMeshProUGUI selectedName = CreateComponentObject<TextMeshProUGUI>("SelectedName");
        var characters = new[]
        {
            new CharacterData { characterName = "First" },
            new CharacterData { characterName = "Second" }
        };

        SetPrivateField(panel, "_selectedCharacterName", selectedName);
        SetPrivateField(panel, "_charactersData", characters);

        panel.SelectCharacter(0);
        Assert.AreEqual("First", selectedName.text);

        panel.SelectPreviousCharacter();
        Assert.AreEqual("Second", selectedName.text);

        panel.SelectNextCharacter();
        Assert.AreEqual("First", selectedName.text);
    }

    [Test]
    public void UiRuntimeCode_DoesNotSearchSceneObjectsOutsideMenuInitializer()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        string[] files =
        {
            Path.Combine(scriptsRoot, "UI", "MenuManager.cs"),
            Path.Combine(scriptsRoot, "UI", "SettingsPanel.cs"),
            Path.Combine(scriptsRoot, "UI", "CharactersPanel.cs"),
            Path.Combine(scriptsRoot, "UI", "InertiaArrowHUD.cs"),
            Path.Combine(scriptsRoot, "UI", "EnsureEventSystem.cs")
        };

        foreach (string file in files)
            AssertNoRuntimeSceneDiscovery(file);
    }

    private GameObject CreateGameObject(string name, params System.Type[] components)
    {
        var gameObject = new GameObject(name, components);
        _createdObjects.Add(gameObject);
        return gameObject;
    }

    private Button CreateButton(string name)
    {
        return CreateComponentObject<Button>(name);
    }

    private T CreateComponentObject<T>(string name) where T : Component
    {
        GameObject gameObject = CreateGameObject(name, typeof(RectTransform), typeof(T));
        return gameObject.GetComponent<T>();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} was not found.");
        field.SetValue(target, value);
    }

    private static void AssertNoRuntimeSceneDiscovery(string path)
    {
        string source = File.ReadAllText(path);
        string[] forbiddenTokens =
        {
            "FindObjectOfType<",
            "FindObjectsOfType<",
            "Object.FindObjectOfType<",
            "Object.FindObjectsOfType<",
            "GameObject.Find(",
            "Resources.FindObjectsOfTypeAll("
        };

        foreach (string token in forbiddenTokens)
            Assert.IsFalse(source.Contains(token), $"{path} uses runtime scene discovery token {token}.");
    }

    private sealed class MemorySettingsStore : ISettingsStore
    {
        public MemorySettingsStore(SettingsSnapshot initial)
        {
            Current = initial;
        }

        public SettingsSnapshot Current { get; private set; }
        public int SaveCount { get; private set; }

        public SettingsSnapshot Load()
        {
            return Current;
        }

        public void Save(SettingsSnapshot settings)
        {
            Current = settings;
            SaveCount++;
        }
    }

    private sealed class TestSettingsApplier : SettingsApplier
    {
        public SettingsSnapshot LastApplied { get; private set; }
        public int ApplyCount { get; private set; }

        public override void Apply(SettingsSnapshot settings)
        {
            LastApplied = settings;
            ApplyCount++;
        }
    }

    private sealed class TestMenuSceneActions : IMenuSceneActions
    {
        public int LoadGameSceneCount { get; private set; }
        public int QuitGameCount { get; private set; }
        public string LastFallbackSceneName { get; private set; }

        public void LoadGameScene(string fallbackSceneName)
        {
            LoadGameSceneCount++;
            LastFallbackSceneName = fallbackSceneName;
        }

        public void QuitGame()
        {
            QuitGameCount++;
        }
    }
}
