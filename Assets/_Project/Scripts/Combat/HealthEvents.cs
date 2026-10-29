using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Health))]
public class HealthEvents : MonoBehaviour
{
    private Health _entityHealth;
    public UnityEvent OnEntityDeath;

    private void Awake()
    {
        _entityHealth = GetComponent<Health>();
        _entityHealth.OnDeath += InvokeDeathEvent;
    }
    private void OnDestroy()
    {
        _entityHealth.OnDeath -= InvokeDeathEvent;
    }
    private void InvokeDeathEvent()
    {
        OnEntityDeath?.Invoke();
    }
}
