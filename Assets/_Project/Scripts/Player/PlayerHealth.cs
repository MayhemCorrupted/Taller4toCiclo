using System;
using UnityEngine;
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerStamina))]
[RequireComponent(typeof(PlayerFSM))]
public class PlayerHealth : Health
{
    public event Action OnHurt;
    public event Action OnLifeLost;
    public event Action OnHealed;
    public event Action OnPermadeath;
    public event Action OnBlockHit;
    private int _currentLives = 3;
    [SerializeField] private float _invulnerabilityDuration = 0.5f;
    private float _invulnerabilityTimer = 0f;
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
    private void OnDestroy()
    {
        _statsSystem.OnStatsUpdated -= SetHealth;
    }
    private void Update()
    {
        if (_invulnerabilityTimer > 0)
        {
            _invulnerabilityTimer -= Time.deltaTime;
        }
    }
    public void SetHealth()
    {
        _maxHealth = _statsSystem.MaxHealth;
        _defense = _statsSystem.Defense;
        if (_currentHealth <= 0)
        {
            _currentHealth = _maxHealth;
            NotifyHealthChanged();
        }
    }
    public override bool TakeDamage(float incomingDamage)
    {
        if (_currentHealth <= 0 || _invulnerabilityTimer > 0) return false;

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
                OnBlockHit?.Invoke();
            }
            else
            {
                _staminaSystem.ExhaustStamina();
                _currentHealth -= unmitigatedDamage;
                OnTakingDamage(unmitigatedDamage);
                if (_currentHealth > 0)
                {
                    _playerFSM.ChangeState(PlayerState.Hurt);
                    OnHurt?.Invoke();
                }
            }   
        }
        else
        {
            _currentHealth -= finalDamage;
            OnTakingDamage(finalDamage);
            if (_currentHealth > 0)
            {
                _playerFSM.ChangeState(PlayerState.Hurt);
                OnHurt?.Invoke();
            }
        }
        _invulnerabilityTimer = _invulnerabilityDuration;
        NotifyHealthChanged();
        if (_currentHealth <= 0)
        {
            Die();
            _currentHealth = 0;
        }
        return true;
    }
    public void Heal(float amount)
    {
        if (_currentHealth <= 0) return;

        _currentHealth += amount;
        if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;

        OnHealed?.Invoke();
    }
    public void TakeDirectDamage(float amount)
    {
        if (_currentHealth <= 0) return;

        _currentHealth -= amount;
        NotifyHealthChanged();

        if (_currentHealth <= 0)
        {
            Die();
            _currentHealth = 0;
        }
        else
        {
            _playerFSM.ChangeState(PlayerState.Hurt);
            OnHurt?.Invoke();
        }
    }
    protected override void Die()
    {
        if (_currentLives > 1)
        {
            _currentLives--;
            _playerFSM.ChangeState(PlayerState.Knockout);
            OnLifeLost?.Invoke();
        }
        else
        {
            _playerFSM.ChangeState(PlayerState.Death);
            OnPermadeath?.Invoke();
        }
    }
    public int CurrentLives => _currentLives;
}
