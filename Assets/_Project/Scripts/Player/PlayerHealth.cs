using System;
using UnityEngine;
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerStamina))]
[RequireComponent(typeof(PlayerFSM))]
public class PlayerHealth : HealthSystem
{
    public event Action OnLifeLost;
    public event Action OnPermadeath;

    private int _currentLives = 3;

    private PlayerStats _statsSystem;
    private PlayerStamina _staminaSystem;
    private PlayerFSM _playerFSM;

    protected override void Awake()
    {
        _statsSystem = GetComponent<PlayerStats>();
        _staminaSystem = GetComponent<PlayerStamina>();
        _playerFSM = GetComponent<PlayerFSM>();

        _statsSystem.OnStatsUpdated += SetHealth;
    }
    private void Start()
    {
        SetHealth();
    }
    private void OnDestroy()
    {
        _statsSystem.OnStatsUpdated -= SetHealth;
    }
    private void SetHealth()
    {
        _maxHealth = _statsSystem.MaxHealth;
        _defense = _statsSystem.Defense;
        if (_currentHealth <= 0)
        {
            _currentHealth = _maxHealth;
        }
    }
    public override void TakeDamage(float incomingDamage)
    {
        if (_currentHealth <= 0) return;

        float unmitigatedDamage = (incomingDamage * 100) / (100 + _defense);
        float finalDamage = unmitigatedDamage;

        if (_playerFSM.CurrentState == PlayerState.Blocking)
        {
            finalDamage *= 0.4f;
            float blockStaminaCost = 10 + (unmitigatedDamage * 0.5f);
            if (_staminaSystem.HasEnoughStamina(blockStaminaCost))
            {
                _staminaSystem.UseStamina(blockStaminaCost);
                _currentHealth -= finalDamage;
                OnTakingDamage(finalDamage);
            }
            else
            {
                _staminaSystem.ExhaustStamina();
                _currentHealth -= unmitigatedDamage;
                OnTakingDamage(unmitigatedDamage);
            }   
        }
        else
        {
            _currentHealth -= finalDamage;
            OnTakingDamage(finalDamage);
        }

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }
    }
    protected override void Die()
    {
        if (_currentLives > 1)
        {
            _currentLives--;
            OnLifeLost?.Invoke();
        }
        else
        {
            _currentLives--;
            OnPermadeath?.Invoke();
        }
    }
    public int CurrentLives => _currentLives;
}
