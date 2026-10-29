using System;
using UnityEngine;
public class Health : MonoBehaviour
{
    public event Action<float> OnTakeDamage;
    public event Action OnDeath;
    public event Action OnHealthChanged;

    [SerializeField] protected float _maxHealth;
    [SerializeField] protected float _defense;

    protected float _currentHealth;
    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;
    protected virtual void Awake()
    {
        _currentHealth = _maxHealth;
    }
    public virtual void TakeDamage(float amount)
    {
        if (_currentHealth <= 0) return;
        float damage = (amount * 100) / (100 + _defense);
        _currentHealth -= damage;
        OnTakeDamage?.Invoke(damage);
        NotifyHealthChanged();
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }
    }
    protected void OnTakingDamage(float damage)
    {
        OnTakeDamage?.Invoke(damage);
    }
    protected void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }
    protected virtual void Die()
    {
        OnDeath?.Invoke();
    }
}