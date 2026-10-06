using System;
using UnityEngine;
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerFSM))]
public class PlayerStamina : MonoBehaviour
{
    public event Action OnStaminaChanged;

    private float _currentStamina;
    private float _staminaRegenDelay;
    readonly private float _staminaDrainRate = 15f;

    private PlayerStats _statsSystem;
    private PlayerFSM _playerFSM;
    private void Awake()
    {
        _statsSystem = GetComponent<PlayerStats>();
        _statsSystem.OnStatsUpdated += SetStamina;
        _playerFSM = GetComponent<PlayerFSM>();
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
        DrainStamina();
        StaminaRegeneration();
    }
    private void DrainStamina()
    {
        if (_playerFSM.CurrentState == PlayerState.Running)
        {
           UseStamina(_staminaDrainRate * Time.deltaTime);
        }
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
