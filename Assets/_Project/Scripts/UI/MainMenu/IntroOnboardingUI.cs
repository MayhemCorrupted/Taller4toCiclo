using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class IntroOnboardingUI : MonoBehaviour
{
    [Header("Contenedor de Estadísticas Iniciales")]
    [SerializeField] private GameObject _statsContainer;
    [SerializeField] private TMP_Text _characterNameHeaderText;
    [SerializeField] private TMP_Text _availablePointsText;
    [SerializeField] private Button _confirmStatsButton;

    [Header("Configuración de Puntos")]
    [SerializeField] private int _startingAttributePoints = 4;
    [SerializeField] private int _minStatLevel = 1;
    [SerializeField] private int _maxStatLevel = 10;

    [Header("Controles de Vida")]
    [SerializeField] private TMP_Text _vidaLevelText;
    [SerializeField] private TMP_Text _vidaPreviewText;
    [SerializeField] private Button _vidaPlusButton;
    [SerializeField] private Button _vidaMinusButton;

    [Header("Controles de Stamina")]
    [SerializeField] private TMP_Text _staminaLevelText;
    [SerializeField] private TMP_Text _staminaPreviewText;
    [SerializeField] private Button _staminaPlusButton;
    [SerializeField] private Button _staminaMinusButton;

    [Header("Controles de Fuerza")]
    [SerializeField] private TMP_Text _fuerzaLevelText;
    [SerializeField] private TMP_Text _fuerzaPreviewText;
    [SerializeField] private Button _fuerzaPlusButton;
    [SerializeField] private Button _fuerzaMinusButton;

    [Header("Controles de Agilidad")]
    [SerializeField] private TMP_Text _agilidadLevelText;
    [SerializeField] private TMP_Text _agilidadPreviewText;
    [SerializeField] private Button _agilidadPlusButton;
    [SerializeField] private Button _agilidadMinusButton;

    [Header("Secuencia Posterior: Diálogo y Escena 1")]
    [SerializeField] private DialogueManager _dialogueManager;
    [SerializeField] private DialogueSequence _introDialogueSequence;
    [SerializeField] private string _stage1SceneName = "Stage1_Bosque";

    [Header("Tiempos de Desvanecimiento antes de Cargar Escena")]
    [Tooltip("Duración del fade out del diálogo al terminar la última línea.")]
    [SerializeField] private float _dialogueFadeOutDuration = 1.0f;
    [Tooltip("Breve pausa tras desvanecerse el diálogo antes de iniciar la carga de la escena.")]
    [SerializeField] private float _delayBeforeSceneLoad = 0.25f;

    private int _vidaLevel;
    private int _staminaLevel;
    private int _fuerzaLevel;
    private int _agilidadLevel;
    private int _remainingPoints;
    private bool _hasConfirmedStats;
    private bool _isLoadingNextScene;
    private void Awake()
    {
        if (_dialogueManager == null)
            _dialogueManager = FindAnyObjectByType<DialogueManager>(FindObjectsInactive.Include);

        _vidaLevel = _minStatLevel;
        _staminaLevel = _minStatLevel;
        _fuerzaLevel = _minStatLevel;
        _agilidadLevel = _minStatLevel;
        _remainingPoints = _startingAttributePoints;
    }
    private void OnEnable()
    {
        if (_vidaPlusButton != null) _vidaPlusButton.onClick.AddListener(() => ModifyStat(ref _vidaLevel, +1));
        if (_vidaMinusButton != null) _vidaMinusButton.onClick.AddListener(() => ModifyStat(ref _vidaLevel, -1));

        if (_staminaPlusButton != null) _staminaPlusButton.onClick.AddListener(() => ModifyStat(ref _staminaLevel, +1));
        if (_staminaMinusButton != null) _staminaMinusButton.onClick.AddListener(() => ModifyStat(ref _staminaLevel, -1));

        if (_fuerzaPlusButton != null) _fuerzaPlusButton.onClick.AddListener(() => ModifyStat(ref _fuerzaLevel, +1));
        if (_fuerzaMinusButton != null) _fuerzaMinusButton.onClick.AddListener(() => ModifyStat(ref _fuerzaLevel, -1));

        if (_agilidadPlusButton != null) _agilidadPlusButton.onClick.AddListener(() => ModifyStat(ref _agilidadLevel, +1));
        if (_agilidadMinusButton != null) _agilidadMinusButton.onClick.AddListener(() => ModifyStat(ref _agilidadLevel, -1));

        if (_confirmStatsButton != null) _confirmStatsButton.onClick.AddListener(OnConfirmStatsClicked);
        if (_dialogueManager != null) _dialogueManager.OnDialogueEnded += HandleIntroDialogueFinished;
    }
    private void OnDisable()
    {
        if (_vidaPlusButton != null) _vidaPlusButton.onClick.RemoveAllListeners();
        if (_vidaMinusButton != null) _vidaMinusButton.onClick.RemoveAllListeners();
        if (_staminaPlusButton != null) _staminaPlusButton.onClick.RemoveAllListeners();
        if (_staminaMinusButton != null) _staminaMinusButton.onClick.RemoveAllListeners();
        if (_fuerzaPlusButton != null) _fuerzaPlusButton.onClick.RemoveAllListeners();
        if (_fuerzaMinusButton != null) _fuerzaMinusButton.onClick.RemoveAllListeners();
        if (_agilidadPlusButton != null) _agilidadPlusButton.onClick.RemoveAllListeners();
        if (_agilidadMinusButton != null) _agilidadMinusButton.onClick.RemoveAllListeners();

        if (_confirmStatsButton != null) _confirmStatsButton.onClick.RemoveListener(OnConfirmStatsClicked);
        if (_dialogueManager != null) _dialogueManager.OnDialogueEnded -= HandleIntroDialogueFinished;
    }

    private void Start()
    {
        if (_statsContainer != null)
            _statsContainer.SetActive(true);

        if (_characterNameHeaderText != null && SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
        {
            _characterNameHeaderText.text = SaveManager.Instance.CurrentRun.player.nickname;
        }

        RefreshUI();
    }
    private void ModifyStat(ref int statField, int delta)
    {
        if (_hasConfirmedStats) return;

        if (delta > 0)
        {
            if (_remainingPoints <= 0 || statField >= _maxStatLevel) return;
            statField++;
            _remainingPoints--;
        }
        else if (delta < 0)
        {
            if (statField <= _minStatLevel) return;
            statField--;
            _remainingPoints++;
        }

        RefreshUI();
    }
    private void RefreshUI()
    {
        if (_availablePointsText != null)
            _availablePointsText.text = _remainingPoints.ToString();

        bool canAdd = _remainingPoints > 0;

        if (_vidaLevelText != null) _vidaLevelText.text = _vidaLevel.ToString();
        if (_vidaPreviewText != null) _vidaPreviewText.text = $"{80 + (12 * _vidaLevel)} HP";
        if (_vidaPlusButton != null) _vidaPlusButton.interactable = canAdd && _vidaLevel < _maxStatLevel;
        if (_vidaMinusButton != null) _vidaMinusButton.interactable = _vidaLevel > _minStatLevel;

        if (_staminaLevelText != null) _staminaLevelText.text = _staminaLevel.ToString();
        if (_staminaPreviewText != null) _staminaPreviewText.text = $"{60 + (8 * _staminaLevel)} ST";
        if (_staminaPlusButton != null) _staminaPlusButton.interactable = canAdd && _staminaLevel < _maxStatLevel;
        if (_staminaMinusButton != null) _staminaMinusButton.interactable = _staminaLevel > _minStatLevel;

        float strengthBonus = _fuerzaLevel <= 6
            ? _fuerzaLevel * 0.05f
            : (6 * 0.05f) + ((_fuerzaLevel - 6) * 0.03f);
        if (_fuerzaLevelText != null) _fuerzaLevelText.text = _fuerzaLevel.ToString();
        if (_fuerzaPreviewText != null) _fuerzaPreviewText.text = $"+{Mathf.RoundToInt(strengthBonus * 100f)}%";
        if (_fuerzaPlusButton != null) _fuerzaPlusButton.interactable = canAdd && _fuerzaLevel < _maxStatLevel;
        if (_fuerzaMinusButton != null) _fuerzaMinusButton.interactable = _fuerzaLevel > _minStatLevel;

        if (_agilidadLevelText != null) _agilidadLevelText.text = _agilidadLevel.ToString();
        if (_agilidadPreviewText != null) _agilidadPreviewText.text = $"+{_agilidadLevel * 3}% Vel.";
        if (_agilidadPlusButton != null) _agilidadPlusButton.interactable = canAdd && _agilidadLevel < _maxStatLevel;
        if (_agilidadMinusButton != null) _agilidadMinusButton.interactable = _agilidadLevel > _minStatLevel;
    }

    private async void OnConfirmStatsClicked()
    {
        if (_hasConfirmedStats) return;
        _hasConfirmedStats = true;

        if (_confirmStatsButton != null)
            _confirmStatsButton.interactable = false;

        if (SaveManager.Instance != null)
        {
            await SaveManager.Instance.ConfirmInitialStatsAsync(_vidaLevel, _staminaLevel, _fuerzaLevel, _agilidadLevel);
        }

        if (_statsContainer != null)
            _statsContainer.SetActive(false);

        if (_dialogueManager != null && _introDialogueSequence != null)
        {
            _dialogueManager.SetCloseFadeDuration(_dialogueFadeOutDuration);
            _dialogueManager.StartDialogue(_introDialogueSequence);
        }
        else
        {
            HandleIntroDialogueFinished();
        }
    }
    private async void HandleIntroDialogueFinished()
    {
        if (_isLoadingNextScene) return;
        _isLoadingNextScene = true;

        if (_delayBeforeSceneLoad > 0f)
        {
            await Task.Delay(Mathf.RoundToInt(_delayBeforeSceneLoad * 1000f));
        }

        if (this == null) return;

        if (SaveManager.Instance != null)
        {
            string targetStage = !string.IsNullOrWhiteSpace(SaveManager.Instance.Stage1SceneName)
                ? SaveManager.Instance.Stage1SceneName
                : _stage1SceneName;

            await SaveManager.Instance.TransitionToNextStageAsync(targetStage, newStateValue: 0);
        }
        else
        {
            await SceneManager.LoadSceneAsync(_stage1SceneName);
        }
    }
}