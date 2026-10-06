using System;
using UnityEngine;
[RequireComponent(typeof(PlayerStats))]
public class PlayerStamina : MonoBehaviour
{
    public event Action OnStaminaChanged;

    private float _currentStamina;
    private float _staminaRegenDelay;
    private PlayerStats _statsSystem;

    private void Awake()
    {
        _statsSystem = GetComponent<PlayerStats>();
        _statsSystem.OnStatsUpdated += SetStamina;
    }
    private void Start()
    {
        SetStamina();
    }
    private void OnDestroy()
    {
        _statsSystem.OnStatsUpdated -= SetStamina;
    }
    private void SetStamina()
    {
        _currentStamina = _statsSystem.MaxStamina;
    }
    private void Update()
    {
        StaminaRegeneration();
    }
    public bool UseStamina(float amount)
    {
        if (_currentStamina <= 0) return false;

        _currentStamina -= amount;

        if (_currentStamina <= 0)
        {
            _currentStamina = 0;
            _staminaRegenDelay = 1.5f;
        }
        else
        {
            _staminaRegenDelay = 0.8f;
        }
        OnStaminaChanged?.Invoke();
        return true;
    }
    public bool HasEnoughStamina(float amount)
    {
        return _currentStamina >= amount;
    }
    public void ExhaustStamina()
    {
        _currentStamina = 0;
        _staminaRegenDelay = 1.5f;
        OnStaminaChanged?.Invoke();
    }
    private void StaminaRegeneration()
    {
        if (_staminaRegenDelay > 0)
        {
            _staminaRegenDelay -= Time.deltaTime;
            return;
        }
        if (_currentStamina < _statsSystem.MaxStamina)
        {
            _currentStamina += 25 * Time.deltaTime;

            if (_currentStamina > _statsSystem.MaxStamina)
            {
                _currentStamina = _statsSystem.MaxStamina;
            }
            OnStaminaChanged?.Invoke();
        }
    }
    public float CurrentStamina => _currentStamina;
}
