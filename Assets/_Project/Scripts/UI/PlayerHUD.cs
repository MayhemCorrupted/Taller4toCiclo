using UnityEngine;
using UnityEngine.UI;
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image _healthFillImage;
    [SerializeField] private Image _staminaFillImage;
    [SerializeField] private float _lerpSpeed = 10f;
    private PlayerHealth _playerHealth;
    private PlayerStamina _playerStamina;
    private PlayerStats _playerStats;
    private float _targetHealthFill = 1;
    private float _targetStaminaFill = 1;
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
        UpdateStaminaBar();
        UpdateHealthBar();

        if (_healthFillImage != null) _healthFillImage.fillAmount = _targetHealthFill;
        if (_staminaFillImage != null) _staminaFillImage.fillAmount = _targetStaminaFill;
    }
    private void Update()
    {
        if (_healthFillImage != null)
            _healthFillImage.fillAmount = Mathf.Lerp(_healthFillImage.fillAmount, _targetHealthFill, Time.deltaTime * _lerpSpeed);

        if (_staminaFillImage != null)
            _staminaFillImage.fillAmount = Mathf.Lerp(_staminaFillImage.fillAmount, _targetStaminaFill, Time.deltaTime * _lerpSpeed);
    }
    private void UpdateHealthBar()
    {
        if (_playerHealth == null || _playerHealth.MaxHealth <= 0) return;
        _targetHealthFill = _playerHealth.CurrentHealth / _playerHealth.MaxHealth;
    }
    private void UpdateStaminaBar()
    {
        if (_playerStamina == null || _playerStats == null || _playerStats.MaxStamina <= 0) return;
        _targetStaminaFill = _playerStamina.CurrentStamina / _playerStats.MaxStamina;
    }
}
