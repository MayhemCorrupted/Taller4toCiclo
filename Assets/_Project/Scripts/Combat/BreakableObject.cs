using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct ObjectEvent
{
    public UnityEvent OnHealthReached;
    public float requiredHealth;
}
[RequireComponent(typeof(Health))]
public class BreakableObject : MonoBehaviour
{
    [Header("Modularidad de Rotura")]
    [SerializeField] private bool _canBeBrokenByAttack = true;
    [SerializeField] private bool _canBeBrokenByDash = false;
    [SerializeField] private float _dashDamageAmount = 999f; 

    private Health _health;
    [SerializeField] private ObjectEvent[] _objectEvents;

    private bool[] _eventTriggeredState;
    private void Awake()
    {
        _health = GetComponent<Health>();

        if (_objectEvents != null)
        {
            _eventTriggeredState = new bool[_objectEvents.Length];
        }

        if (_health != null && _canBeBrokenByAttack)
        {
            _health.OnHealthChanged += CheckHealthThresholds;
            _health.OnDeath += HandleDeath;
        }
    }
    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= CheckHealthThresholds;
            _health.OnDeath -= HandleDeath;
        }
    }
    private void CheckHealthThresholds()
    {
        if (!_canBeBrokenByAttack || _objectEvents == null) return;

        for (int i = 0; i < _objectEvents.Length; i++)
        {
            if (_eventTriggeredState[i]) continue;

            if (_health.CurrentHealth <= _objectEvents[i].requiredHealth)
            {
                _eventTriggeredState[i] = true;
                _objectEvents[i].OnHealthReached?.Invoke();
            }
        }
    }
    private void HandleDeath()
    {
        gameObject.SetActive(false);
    }
    private void OnCollisionEnter(Collision collision)
    {
        TryBreakByDash(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBreakByDash(other.gameObject);
    }

    private void TryBreakByDash(GameObject hitObject)
    {
        if (!_canBeBrokenByDash) return;

        PlayerFSM playerFSM = hitObject.GetComponentInParent<PlayerFSM>();

        if (playerFSM != null && playerFSM.CurrentState == PlayerState.Dodging)
        {
            BreakByDash();
        }
    }
    private void BreakByDash()
    {
        if (_health != null && _canBeBrokenByAttack)
        {
            _health.TakeDamage(_dashDamageAmount);
        }
        else
        {
            if (_objectEvents != null)
            {
                for (int i = 0; i < _objectEvents.Length; i++)
                {
                    if (!_eventTriggeredState[i])
                    {
                        _eventTriggeredState[i] = true;
                        _objectEvents[i].OnHealthReached?.Invoke();
                    }
                }
            }
            Destroy(gameObject);
        }
    }
}