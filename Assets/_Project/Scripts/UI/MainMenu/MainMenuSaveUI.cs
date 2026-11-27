using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct SaveSlotButtonBinding
{
    public Button slotButton;
    public Button deleteButton;
    public TMP_Text characterNameText;
    public TMP_Text playTimeText;
}

public class MainMenuSaveUI : MonoBehaviour
{
    private enum SlotMenuMode { NewGame, LoadGame }

    [Header("Controlador de Paneles")]
    [SerializeField] private MainMenuPanelUI _mainMenuPanelUI;

    [Header("Botones Principales (En MainMenuContainer)")]
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _loadGameButton;

    [Header("Panel Principal de Partida (Slots + Nombre)")]
    [Tooltip("Contenedor raíz que agrupa los slots, el subcontenedor de nombre y el botón Return principal.")]
    [SerializeField] private GameObject _playRootContainer;
    [Tooltip("Contenedor específico de los 3 botones de slot.")]
    [SerializeField] private GameObject _slotsContainer;
    [Tooltip("Botón Return principal para volver al Menú Principal (se apaga al abrir el subcontenedor de nombre).")]
    [SerializeField] private Button _closeSlotsButton;
    [SerializeField] private SaveSlotButtonBinding[] _slotBindings = new SaveSlotButtonBinding[3];

    [Header("Sub-Contenedor de Nombre (Nueva Partida)")]
    [SerializeField] private GameObject _nameInputSubContainer;
    [SerializeField] private TMP_InputField _nicknameInputField;
    [SerializeField] private Button _confirmNameButton;
    [SerializeField] private Button _cancelNameButton;
    [Tooltip("Si está activo y _slotsContainer es independiente de _nameInputSubContainer, oculta los slots mientras se escribe el nombre.")]
    [SerializeField] private bool _hideSlotsWhileNaming = false;

    [Header("Textos por Defecto")]
    [SerializeField] private string _emptySlotLabel = "Ranura Vacía";
    [SerializeField] private string _emptyTimeLabel = "--:--:--";
    [SerializeField] private string _defaultNickname = "Faulvalier";

    private SaveSlotSummary[] _cachedSummaries;
    private string[] _cloudJsonPerSlot = new string[3];
    private SlotMenuMode _currentMode = SlotMenuMode.NewGame;
    private int _selectedSlotIndex = -1;
    private bool _isStartingGame;
    private void Awake()
    {
        if (_mainMenuPanelUI == null)
            _mainMenuPanelUI = GetComponentInParent<MainMenuPanelUI>(true);

        if (_mainMenuPanelUI == null)
            _mainMenuPanelUI = FindAnyObjectByType<MainMenuPanelUI>(FindObjectsInactive.Include);
    }
    private async void Start()
    {
        BinarySaveService.Initialize();

        SetActiveSafe(_nameInputSubContainer, false);
        SetActiveSafe(_slotsContainer, false);
        SetActiveSafe(_playRootContainer, false);

        RegisterListeners();
        await RefreshSlotsDataAsync();
    }
    private void OnDestroy()
    {
        UnregisterListeners();
    }
    private void RegisterListeners()
    {
        if (_newGameButton != null) _newGameButton.onClick.AddListener(OnClickNewGameButton);
        if (_loadGameButton != null) _loadGameButton.onClick.AddListener(OnClickLoadGameButton);
        if (_closeSlotsButton != null) _closeSlotsButton.onClick.AddListener(CloseSlotsContainer);

        if (_confirmNameButton != null) _confirmNameButton.onClick.AddListener(OnClickConfirmNewGame);
        if (_cancelNameButton != null) _cancelNameButton.onClick.AddListener(OnClickCancelNameInput);

        for (int i = 0; i < _slotBindings.Length; i++)
        {
            int capturedIndex = i;
            if (_slotBindings[i].slotButton != null)
            {
                _slotBindings[i].slotButton.onClick.AddListener(() => OnClickSlot(capturedIndex));
            }
        }
    }
    private void UnregisterListeners()
    {
        if (_newGameButton != null) _newGameButton.onClick.RemoveListener(OnClickNewGameButton);
        if (_loadGameButton != null) _loadGameButton.onClick.RemoveListener(OnClickLoadGameButton);
        if (_closeSlotsButton != null) _closeSlotsButton.onClick.RemoveListener(CloseSlotsContainer);

        if (_confirmNameButton != null) _confirmNameButton.onClick.RemoveListener(OnClickConfirmNewGame);
        if (_cancelNameButton != null) _cancelNameButton.onClick.RemoveListener(OnClickCancelNameInput);

        for (int i = 0; i < _slotBindings.Length; i++)
        {
            if (_slotBindings[i].slotButton != null)
                _slotBindings[i].slotButton.onClick.RemoveAllListeners();
        }
    }
    public async Task RefreshSlotsDataAsync()
    {
        _cachedSummaries = new SaveSlotSummary[BinarySaveService.MAX_SLOTS];
        _cloudJsonPerSlot = new string[BinarySaveService.MAX_SLOTS];

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.IsLoggedIn())
        {
            var cloudChars = DatabaseManager.Instance.Characters.myCharacters;

            for (int i = 0; i < BinarySaveService.MAX_SLOTS; i++)
            {
                if (cloudChars != null && i < cloudChars.Length)
                {
                    var c = cloudChars[i];
                    _cloudJsonPerSlot[i] = c.save_json;
                    _cachedSummaries[i] = new SaveSlotSummary
                    {
                        slotIndex = i,
                        characterId = c.character_id,
                        isOccupied = true,
                        nickname = c.nickname,
                        playTimeSeconds = c.play_time,
                        runResult = c.result
                    };
                }
                else
                {
                    _cachedSummaries[i] = new SaveSlotSummary { slotIndex = i, characterId = -1, isOccupied = false };
                }
            }
        }
        else
        {
            _cachedSummaries = await BinarySaveService.GetAllSlotSummariesAsync();
        }

        bool hasAnyPlayableSave = false;
        for (int i = 0; i < _cachedSummaries.Length; i++)
        {
            if (_cachedSummaries[i].isOccupied && string.IsNullOrEmpty(_cachedSummaries[i].runResult))
            {
                hasAnyPlayableSave = true;
                break;
            }
        }

        if (_loadGameButton != null)
            _loadGameButton.gameObject.SetActive(hasAnyPlayableSave);

        UpdateSlotButtonsVisuals(slotsInteractable: true);
    }

    private void UpdateSlotButtonsVisuals(bool slotsInteractable)
    {
        if (_cachedSummaries == null) return;

        for (int i = 0; i < _slotBindings.Length && i < _cachedSummaries.Length; i++)
        {
            SaveSlotSummary summary = _cachedSummaries[i];
            SaveSlotButtonBinding binding = _slotBindings[i];

            if (summary.isOccupied)
            {
                if (binding.characterNameText != null)
                    binding.characterNameText.text = summary.nickname;

                if (binding.playTimeText != null)
                    binding.playTimeText.text = FormatPlayTime(summary.playTimeSeconds);
            }
            else
            {
                if (binding.characterNameText != null)
                    binding.characterNameText.text = _emptySlotLabel;

                if (binding.playTimeText != null)
                    binding.playTimeText.text = _emptyTimeLabel;
            }

            if (binding.slotButton != null)
            {
                bool modeAllowsClick = (_currentMode == SlotMenuMode.NewGame) || summary.isOccupied;
                binding.slotButton.interactable = slotsInteractable && modeAllowsClick;
            }
        }
    }
    private void OnClickNewGameButton()
    {
        if (_isStartingGame) return;
        OpenPlayPanelWithMode(SlotMenuMode.NewGame);
    }
    private void OnClickLoadGameButton()
    {
        if (_isStartingGame) return;
        OpenPlayPanelWithMode(SlotMenuMode.LoadGame);
    }
    private void OpenPlayPanelWithMode(SlotMenuMode mode)
    {
        _currentMode = mode;
        _selectedSlotIndex = -1;

        if (_mainMenuPanelUI != null)
        {
            _mainMenuPanelUI.OpenPlayPanel();
        }

        SetActiveSafe(_playRootContainer, true);
        SetActiveSafe(_slotsContainer, true);
        SetActiveSafe(_nameInputSubContainer, false);

        if (_closeSlotsButton != null)
            _closeSlotsButton.gameObject.SetActive(true);

        UpdateSlotButtonsVisuals(slotsInteractable: true);
    }
    private void CloseSlotsContainer()
    {
        if (_isStartingGame) return;

        _selectedSlotIndex = -1;
        SetActiveSafe(_nameInputSubContainer, false);
        SetActiveSafe(_slotsContainer, false);
        SetActiveSafe(_playRootContainer, false);

        if (_mainMenuPanelUI != null)
        {
            _mainMenuPanelUI.ClosePlayPanelToMainMenu();
        }
    }
    private async void OnClickSlot(int slotIndex)
    {
        if (_isStartingGame || _cachedSummaries == null || slotIndex < 0 || slotIndex >= _cachedSummaries.Length)
        return;

    var summary = _cachedSummaries[slotIndex];
    if (!summary.isOccupied) return;

    BinarySaveService.DeleteSlotSave(slotIndex);

    if (DatabaseManager.Instance != null && summary.characterId > 0)
        DatabaseManager.Instance.Characters.DeleteCharacter(summary.characterId);

    await RefreshSlotsDataAsync();
}
    private void OnClickCancelNameInput()
    {
        if (_isStartingGame) return;

        _selectedSlotIndex = -1;
        SetActiveSafe(_nameInputSubContainer, false);
        SetActiveSafe(_slotsContainer, true);

        if (_closeSlotsButton != null)
            _closeSlotsButton.gameObject.SetActive(true);

        UpdateSlotButtonsVisuals(slotsInteractable: true);
    }
    private async void OnClickConfirmNewGame()
    {
        if (_isStartingGame || _selectedSlotIndex < 0) return;

        _isStartingGame = true;

        string chosenNickname = _nicknameInputField != null && !string.IsNullOrWhiteSpace(_nicknameInputField.text)
            ? _nicknameInputField.text.Trim()
            : _defaultNickname;

        if (SaveManager.Instance != null)
        {
            await SaveManager.Instance.StartNewGameAsync(_selectedSlotIndex, chosenNickname);
        }

        _isStartingGame = false;
    }
    private bool CanHideSlotsSafely()
    {
        if (_slotsContainer == null || _nameInputSubContainer == null) return false;
        if (_slotsContainer == _nameInputSubContainer) return false;
        return !_nameInputSubContainer.transform.IsChildOf(_slotsContainer.transform);
    }
    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }
    public static string FormatPlayTime(float totalSeconds)
    {
        int secondsInt = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        int hours = secondsInt / 3600;
        int minutes = (secondsInt % 3600) / 60;
        int seconds = secondsInt % 60;
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
}