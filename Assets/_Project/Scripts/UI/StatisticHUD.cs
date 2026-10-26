using TMPro;
using UnityEngine;
public class StatisticHUD : MonoBehaviour
{
    private PlayerStats _playerStats;
    [SerializeField] private TextMeshProUGUI _lifeValueTMP;
    [SerializeField] private TextMeshProUGUI _staminaValueTMP;
    [SerializeField] private TextMeshProUGUI _strengthValueTMP;
    [SerializeField] private TextMeshProUGUI _agilityValueTMP;
    [SerializeField] private TextMeshProUGUI _defenseValueTMP;
    [SerializeField] private TextMeshProUGUI _healthPointsValueTMP;
    private void Awake()
    {
        _playerStats = FindAnyObjectByType<PlayerStats>();
        if (_playerStats != null)
        {
            _playerStats.OnStatsUpdated += UpdateStatsValues;
            UpdateStatsValues();
        }
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
}