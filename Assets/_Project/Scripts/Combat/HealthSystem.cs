using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event Action<float> OnTakeDamage;
    public event Action OnDeath;

    [SerializeField] protected float _maxHealth;
    [SerializeField] protected float _defense;

    protected float _currentHealth;

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
    protected virtual void Die()
    {
        OnDeath?.Invoke();
    }
}
