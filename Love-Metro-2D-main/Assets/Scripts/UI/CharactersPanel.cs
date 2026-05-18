using System.Collections.Generic;
using LoveMetro.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharactersPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform _charactersContainer;
    [SerializeField] private GameObject _characterCardPrefab;
    [SerializeField] private Button _backButton;

    [Header("Character Details")]
    [SerializeField] private Image _selectedCharacterImage;
    [SerializeField] private TMP_Text _selectedCharacterName;
    [SerializeField] private TMP_Text _selectedCharacterDescription;
    [SerializeField] private TMP_Text _selectedCharacterStats;

    [Header("Character Data")]
    [SerializeField] private CharacterData[] _charactersData;

    [SerializeField] private MenuManager _menuManager;

    private readonly List<CharacterCard> _characterCards = new List<CharacterCard>();
    private CharacterSelectionModel _selectionModel;

    public void Configure(MenuManager menuManager)
    {
        if (menuManager != null)
            _menuManager = menuManager;
    }

    internal void InitializeForTests()
    {
        SetupButtonListeners();
        CreateCharacterCards();
        SelectCharacter(0);
    }

    private void Start()
    {
        SetupButtonListeners();
        CreateCharacterCards();
        SelectCharacter(0);
    }

    private CharacterSelectionModel SelectionModel
    {
        get
        {
            int count = CharacterCount;
            if (_selectionModel == null || _selectionModel.Count != count)
                _selectionModel = new CharacterSelectionModel(count);

            return _selectionModel;
        }
    }

    private int CharacterCount => _charactersData != null ? _charactersData.Length : 0;

    private void SetupButtonListeners()
    {
        if (_backButton == null)
            return;

        _backButton.onClick.RemoveListener(BackToMenu);
        _backButton.onClick.AddListener(BackToMenu);
    }

    private void CreateCharacterCards()
    {
        ClearCharacterCards();

        if (_charactersData == null || _characterCardPrefab == null || _charactersContainer == null)
            return;

        for (int i = 0; i < _charactersData.Length; i++)
        {
            GameObject cardObj = Instantiate(_characterCardPrefab, _charactersContainer);
            cardObj.SetActive(true);

            CharacterCard card = cardObj.GetComponent<CharacterCard>();
            if (card == null)
                continue;

            int index = i;
            card.Initialize(_charactersData[i], () => SelectCharacter(index));
            _characterCards.Add(card);
        }

        _characterCardPrefab.SetActive(false);
    }

    private void ClearCharacterCards()
    {
        _characterCards.Clear();

        if (_charactersContainer == null)
            return;

        var childrenToDestroy = new List<GameObject>();
        foreach (Transform child in _charactersContainer)
        {
            if (child != null && child.gameObject != _characterCardPrefab)
                childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
            DestroyUiObject(child);
    }

    public void SelectCharacter(int index)
    {
        if (!SelectionModel.TrySelect(index, out int selectedIndex))
            return;

        CharacterData character = _charactersData[selectedIndex];

        if (character != null)
            UpdateSelectedCharacter(character);
        else
            ClearSelectedCharacterDetails();

        UpdateCardSelection(selectedIndex);

        string characterName = character != null ? character.characterName : "null";
        Debug.Log($"CharactersPanel: selected character {characterName}");
    }

    public void BackToMenu()
    {
        if (_menuManager != null)
            _menuManager.BackToMainMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SelectPreviousCharacter();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SelectNextCharacter();
        }
    }

    public void SelectPreviousCharacter()
    {
        if (SelectionModel.TrySelectPrevious(out int selectedIndex))
            SelectCharacter(selectedIndex);
    }

    public void SelectNextCharacter()
    {
        if (SelectionModel.TrySelectNext(out int selectedIndex))
            SelectCharacter(selectedIndex);
    }

    private void UpdateSelectedCharacter(CharacterData character)
    {
        if (_selectedCharacterImage != null && character.portrait != null)
            _selectedCharacterImage.sprite = character.portrait;

        if (_selectedCharacterName != null)
            _selectedCharacterName.text = character.characterName;

        if (_selectedCharacterDescription != null)
            _selectedCharacterDescription.text = character.description;

        if (_selectedCharacterStats != null)
            _selectedCharacterStats.text = FormatCharacterStats(character);
    }

    private void ClearSelectedCharacterDetails()
    {
        if (_selectedCharacterImage != null)
            _selectedCharacterImage.sprite = null;

        if (_selectedCharacterName != null)
            _selectedCharacterName.text = string.Empty;

        if (_selectedCharacterDescription != null)
            _selectedCharacterDescription.text = string.Empty;

        if (_selectedCharacterStats != null)
            _selectedCharacterStats.text = string.Empty;
    }

    private void UpdateCardSelection(int selectedIndex)
    {
        for (int i = 0; i < _characterCards.Count; i++)
            _characterCards[i].SetSelected(i == selectedIndex);
    }

    private static string FormatCharacterStats(CharacterData character)
    {
        return $"\u0421\u043A\u043E\u0440\u043E\u0441\u0442\u044C: {character.speed}\n" +
               $"\u041F\u0440\u0438\u0432\u043B\u0435\u043A\u0430\u0442\u0435\u043B\u044C\u043D\u043E\u0441\u0442\u044C: {character.attractiveness}\n" +
               $"\u0423\u0441\u0442\u043E\u0439\u0447\u0438\u0432\u043E\u0441\u0442\u044C: {character.stability}";
    }

    private static void DestroyUiObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}

[System.Serializable]
public class CharacterData
{
    [Header("Main Info")]
    public string characterName = "\u041F\u0435\u0440\u0441\u043E\u043D\u0430\u0436";
    public Sprite portrait;
    public Sprite fullBodySprite;

    [Header("Description")]
    [TextArea(3, 5)]
    public string description = "\u041E\u043F\u0438\u0441\u0430\u043D\u0438\u0435 \u043F\u0435\u0440\u0441\u043E\u043D\u0430\u0436\u0430";

    [Header("Stats")]
    [Range(1, 10)]
    public int speed = 5;
    [Range(1, 10)]
    public int attractiveness = 5;
    [Range(1, 10)]
    public int stability = 5;

    [Header("Gameplay Prefabs")]
    public GameObject malePrefab;
    public GameObject femalePrefab;

    [Header("Unlock")]
    public bool isUnlocked = true;
    public string unlockCondition = "";
}

public class CharacterCard : MonoBehaviour
{
    [Header("Card UI Elements")]
    [SerializeField] private Image _portraitImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Button _selectButton;
    [SerializeField] private GameObject _selectedIndicator;
    [SerializeField] private GameObject _lockedOverlay;

    private CharacterData _characterData;
    private System.Action _onSelectCallback;

    public void Initialize(CharacterData data, System.Action onSelectCallback)
    {
        _characterData = data;
        _onSelectCallback = onSelectCallback;

        if (_portraitImage != null)
            _portraitImage.sprite = data != null ? data.portrait : null;

        if (_nameText != null)
            _nameText.text = data != null ? data.characterName : string.Empty;

        if (_selectButton != null)
        {
            _selectButton.onClick.RemoveListener(OnCardClicked);
            _selectButton.onClick.AddListener(OnCardClicked);
        }

        bool isUnlocked = data == null || data.isUnlocked;
        if (_lockedOverlay != null)
            _lockedOverlay.SetActive(!isUnlocked);

        if (_selectButton != null)
            _selectButton.interactable = isUnlocked;
    }

    public void SetSelected(bool selected)
    {
        if (_selectedIndicator != null)
            _selectedIndicator.SetActive(selected);
    }

    private void OnCardClicked()
    {
        if (_characterData != null && _characterData.isUnlocked)
            _onSelectCallback?.Invoke();
    }
}
