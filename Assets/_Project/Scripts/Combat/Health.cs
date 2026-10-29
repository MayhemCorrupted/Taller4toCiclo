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
    public float Defense => _defense;
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
    public void Init(float maxHealth, float defense)
    {
        _maxHealth = maxHealth;
        _defense = defense;
        _currentHealth = _maxHealth;
        NotifyHealthChanged();
    }
    protected void OnTakingDamage(float damage)
    {
        OnTakeDamage?.Invoke(damage);
#if UNITY_EDITOR
        Debug.Log($"Vida actual de {gameObject.name}: {_currentHealth}");
#endif
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