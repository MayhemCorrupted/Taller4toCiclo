using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Health))]
public class HealthEvents : MonoBehaviour
{
    private Health _entityHealth;

    [Header("Eventos de Inspector")]
    public UnityEvent OnEntityDeath;
    public UnityEvent OnEntityHurt;

    private void Awake()
    {
        _entityHealth = GetComponent<Health>();
        _entityHealth.OnDeath += InvokeDeathEvent;
        _entityHealth.OnHealthChanged += InvokeHurtEvent;
    }

    private void OnDestroy()
    {
        if (_entityHealth != null)
        {
            _entityHealth.OnDeath -= InvokeDeathEvent;
            _entityHealth.OnHealthChanged -= InvokeHurtEvent;
        }
    }

    private void InvokeHurtEvent()
    {
        OnEntityHurt?.Invoke();
    }

    private void InvokeDeathEvent()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.RegisterEntityKilled(gameObject.tag);
        }

        OnEntityDeath?.Invoke();
    }
}