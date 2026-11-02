using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    public event Action OnEntityHit;
    private float _currentDamage;
    private Collider _hitboxCollider;
    private readonly List<Health> _alreadyHitEntities = new();
    private void Awake()
    {
        _hitboxCollider = GetComponent<Collider>();
        _hitboxCollider.isTrigger = true;
        _hitboxCollider.enabled = false;
    }
    public void SetDamage(float damage)
    {
        _currentDamage = damage;
    }
    public void EnableHitbox()
    {
        _alreadyHitEntities.Clear();
        _hitboxCollider.enabled = true;
    }
    public void DisableHitbox()
    {
        _hitboxCollider.enabled = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }
    private void TryHit(Collider other)
    {
        Health targetHealth = other.GetComponentInParent<Health>();
        if (targetHealth == null || targetHealth is PlayerHealth) return;
        if (_alreadyHitEntities.Contains(targetHealth)) return;

        if (!targetHealth.TakeDamage(_currentDamage)) return;

        _alreadyHitEntities.Add(targetHealth);
        OnEntityHit?.Invoke();
#if UNITY_EDITOR
        Debug.Log($"{gameObject.name} Pegó a {other.name} daño inflingido: {_currentDamage}");
#endif
    }
}
