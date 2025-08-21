using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharactersPanel : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private Transform _charactersContainer;
    [SerializeField] private GameObject _characterCardPrefab;
    [SerializeField] private Button _backButton;
    
    [Header("Детали персонажа")]
    [SerializeField] private Image _selectedCharacterImage;
    [SerializeField] private TMP_Text _selectedCharacterName;
    [SerializeField] private TMP_Text _selectedCharacterDescription;
    [SerializeField] private TMP_Text _selectedCharacterStats;
    
    [Header("Данные персонажей")]
    [SerializeField] private CharacterData[] _charactersData;
    
    // Ссылка на менеджер меню
    private MenuManager _menuManager;
    private List<CharacterCard> _characterCards = new List<CharacterCard>();
    private int _selectedCharacterIndex = 0;
    
    private void Start()
    {
        _menuManager = FindObjectOfType<MenuManager>();
        SetupButtonListeners();
        CreateCharacterCards();
        SelectCharacter(0);
    }
    
    private void SetupButtonListeners()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(BackToMenu);
    }
    
    private void CreateCharacterCards()
    {
        if (_charactersData == null || _characterCardPrefab == null || _charactersContainer == null)
            return;
            
        // Очищаем существующие карточки
        foreach (Transform child in _charactersContainer)
        {
            if (child.gameObject != _characterCardPrefab)
                Destroy(child.gameObject);
        }
        _characterCards.Clear();
        
        // Создаем карточки персонажей
        for (int i = 0; i < _charactersData.Length; i++)
        {
            GameObject cardObj = Instantiate(_characterCardPrefab, _charactersContainer);
            CharacterCard card = cardObj.GetComponent<CharacterCard>();
            
            if (card != null)
            {
                int index = i; // Захватываем индекс для лямбды
                card.Initialize(_charactersData[i], () => SelectCharacter(index));
                _characterCards.Add(card);
            }
        }
        
        // Скрываем префаб
        if (_characterCardPrefab != null)
            _characterCardPrefab.SetActive(false);
    }
    
    public void SelectCharacter(int index)
    {
        if (index < 0 || index >= _charactersData.Length)
            return;
            
        _selectedCharacterIndex = index;
        CharacterData character = _charactersData[index];
        
        // Обновляем детали персонажа
        if (_selectedCharacterImage != null && character.portrait != null)
            _selectedCharacterImage.sprite = character.portrait;
            
        if (_selectedCharacterName != null)
            _selectedCharacterName.text = character.characterName;
            
        if (_selectedCharacterDescription != null)
            _selectedCharacterDescription.text = character.description;
            
        if (_selectedCharacterStats != null)
        {
            _selectedCharacterStats.text = $"Скорость: {character.speed}\n" +
                                         $"Привлекательность: {character.attractiveness}\n" +
                                         $"Устойчивость: {character.stability}";
        }
        
        // Обновляем выделение карточек
        for (int i = 0; i < _characterCards.Count; i++)
        {
            _characterCards[i].SetSelected(i == index);
        }
        
        Debug.Log($"CharactersPanel: Выбран персонаж {character.characterName}");
    }
    
    public void BackToMenu()
    {
        if (_menuManager != null)
        {
            _menuManager.BackToMainMenu();
        }
    }
    
    #region Navigation
    
    private void Update()
    {
        // Навигация стрелками
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
        int newIndex = _selectedCharacterIndex - 1;
        if (newIndex < 0)
            newIndex = _charactersData.Length - 1;
        SelectCharacter(newIndex);
    }
    
    public void SelectNextCharacter()
    {
        int newIndex = _selectedCharacterIndex + 1;
        if (newIndex >= _charactersData.Length)
            newIndex = 0;
        SelectCharacter(newIndex);
    }
    
    #endregion
}

[System.Serializable]
public class CharacterData
{
    [Header("Основная информация")]
    public string characterName = "Персонаж";
    public Sprite portrait;
    public Sprite fullBodySprite;
    
    [Header("Описание")]
    [TextArea(3, 5)]
    public string description = "Описание персонажа";
    
    [Header("Характеристики")]
    [Range(1, 10)]
    public int speed = 5;
    [Range(1, 10)]
    public int attractiveness = 5;
    [Range(1, 10)]
    public int stability = 5;
    
    [Header("Игровые префабы")]
    public GameObject malePrefab;
    public GameObject femalePrefab;
    
    [Header("Разблокировка")]
    public bool isUnlocked = true;
    public string unlockCondition = "";
}

public class CharacterCard : MonoBehaviour
{
    [Header("UI элементы карточки")]
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
        
        // Настраиваем UI
        if (_portraitImage != null && data.portrait != null)
            _portraitImage.sprite = data.portrait;
            
        if (_nameText != null)
            _nameText.text = data.characterName;
            
        if (_selectButton != null)
            _selectButton.onClick.AddListener(OnCardClicked);
            
        // Настраиваем состояние разблокировки
        bool isUnlocked = data.isUnlocked;
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
        if (_characterData.isUnlocked)
        {
            _onSelectCallback?.Invoke();
        }
    }
} 