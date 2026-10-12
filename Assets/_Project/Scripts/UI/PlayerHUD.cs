using UnityEngine;
using UnityEngine.UI;
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image _healthFillImage;
    [SerializeField] private Image _staminaFillImage;

    private PlayerHealth _playerHealth;
    private PlayerStamina _playerStamina;
    private PlayerStats _playerStats;
    private void Awake()
    {
        _playerHealth = FindAnyObjectByType<PlayerHealth>();
        _playerStamina = FindAnyObjectByType<PlayerStamina>();
        _playerStats = FindAnyObjectByType<PlayerStats>();
    }
    private void OnEnable()
    {
        if (_playerHealth != null) _playerHealth.OnHealthChanged += UpdateHealthBar;
        if (_playerStamina != null) _playerStamina.OnStaminaChanged += UpdateStaminaBar;
    }

    private void OnDisable()
    {
        if (_playerHealth != null) _playerHealth.OnHealthChanged -= UpdateHealthBar;
        if (_playerStamina != null) _playerStamina.OnStaminaChanged -= UpdateStaminaBar;
    }
    private void Start()
    {
        UpdateHealthBar();
        UpdateStaminaBar();
    }
    private void UpdateHealthBar()
    {
        if (_playerHealth == null || _playerHealth.MaxHealth <= 0) return;
        _healthFillImage.fillAmount = _playerHealth.CurrentHealth / _playerHealth.MaxHealth;
    }

    private void UpdateStaminaBar()
    {
        if (_playerStamina == null || _playerStats == null || _playerStats.MaxStamina <= 0) return;
        _staminaFillImage.fillAmount = _playerStamina.CurrentStamina / _playerStats.MaxStamina;
    }
}
