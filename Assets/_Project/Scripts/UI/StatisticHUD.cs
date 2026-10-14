using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class StatisticHUD : MonoBehaviour
{
    private PlayerStats _playerStats;

    [SerializeField] private TextMeshProUGUI _lifeValueTMP;
    [SerializeField] private TextMeshProUGUI _staminaValueTMP;
    [SerializeField] private TextMeshProUGUI _strengthValueTMP;
    [SerializeField] private TextMeshProUGUI _agilityValueTMP;
    [SerializeField] private TextMeshProUGUI _defenseValueTMP;
    [SerializeField] private TextMeshProUGUI _healthPointsValueTMP;
    [Space(10)]
    [SerializeField] private TextMeshProUGUI _lifeLabelTMP;
    [SerializeField] private TextMeshProUGUI _staminaLabelTMP;
    [SerializeField] private TextMeshProUGUI _strengthLabelTMP;
    [SerializeField] private TextMeshProUGUI _agilityLabelTMP;
    [SerializeField] private TextMeshProUGUI _defenseLabelTMP;
    [SerializeField] private TextMeshProUGUI _healthPointsLabelTMP;
    [Space(10)]
    [SerializeField] private LocalizedString _lifeLocalizedText;
    [SerializeField] private LocalizedString _staminaLocalizedText;
    [SerializeField] private LocalizedString _strengthLocalizedText;
    [SerializeField] private LocalizedString _agilityLocalizedText;
    [SerializeField] private LocalizedString _defenseLocalizedText;
    [SerializeField] private LocalizedString _healthPointsLocalizedText;

    private void Awake()
    {
        _playerStats = FindAnyObjectByType<PlayerStats>();
        _playerStats.OnStatsUpdated += UpdateStatsValues;

        _lifeLocalizedText.StringChanged += UpdateLifeLabel;
        _staminaLocalizedText.StringChanged += UpdateStaminaLabel;
        _strengthLocalizedText.StringChanged += UpdateStrengthLabel;
        _agilityLocalizedText.StringChanged += UpdateAgilityLabel;

        _defenseLocalizedText.StringChanged += UpdateDefenseLabel;
        _healthPointsLocalizedText.StringChanged += UpdateHPLabel;
        UpdateStatsValues();
    }
    private void OnDestroy()
    {
        if (_playerStats != null) _playerStats.OnStatsUpdated -= UpdateStatsValues;

        _lifeLocalizedText.StringChanged -= UpdateLifeLabel;
        _staminaLocalizedText.StringChanged -= UpdateStaminaLabel;
        _strengthLocalizedText.StringChanged -= UpdateStrengthLabel;
        _agilityLocalizedText.StringChanged -= UpdateAgilityLabel;

        _defenseLocalizedText.StringChanged -= UpdateDefenseLabel;
        _healthPointsLocalizedText.StringChanged -= UpdateHPLabel;
    }
     private void UpdateStatsValues()
    {
        if (_playerStats == null) return;
        _lifeValueTMP.text = _playerStats._lifeLevel.ToString();
        _staminaValueTMP.text = _playerStats._staminaLevel.ToString();
        _strengthValueTMP.text = _playerStats._strengthLevel.ToString();
        _agilityValueTMP.text = _playerStats._agilityLevel.ToString();

        _defenseValueTMP.text = _playerStats.Defense.ToString("F0");
        _healthPointsValueTMP.text = _playerStats.MaxHealth.ToString("F0");
    }
    private void UpdateLifeLabel(string text) => _lifeLabelTMP.text = text;
    private void UpdateStaminaLabel(string text) => _staminaLabelTMP.text = text;
    private void UpdateStrengthLabel(string text) => _strengthLabelTMP.text = text;
    private void UpdateAgilityLabel(string text) => _agilityLabelTMP.text = text;
    private void UpdateDefenseLabel(string text) => _defenseLabelTMP.text = text;
    private void UpdateHPLabel(string text) => _healthPointsLabelTMP.text = text;

}
