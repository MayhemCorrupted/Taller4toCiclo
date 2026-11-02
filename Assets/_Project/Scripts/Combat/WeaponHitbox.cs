using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    public event Action OnEntityHit;
    private float _currentDamage;
    private float _currentKnockback;
    private Transform _owner;
    private Collider _hitboxCollider;
    private readonly List<Health> _alreadyHitEntities = new();
    private void Awake()
    {
        _hitboxCollider = GetComponent<Collider>();
        _hitboxCollider.isTrigger = true;
        _hitboxCollider.enabled = false;
        _owner = GetComponentInParent<PlayerCombat>()?.transform ?? transform.root;
    }
    public void SetDamage(float damage)
    {
        _currentDamage = damage;
    }
    public void SetKnockback(float knockback)
    {
        _currentKnockback = knockback;
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
        ApplyHitKnockback(targetHealth);
        OnEntityHit?.Invoke();
#if UNITY_EDITOR
        Debug.Log($"{gameObject.name} Pegó a {other.name} daño inflingido: {_currentDamage}");
#endif
    }
    private void ApplyHitKnockback(Health targetHealth)
    {
        if (_currentKnockback <= 0f) return;

        IKnockbackReceiver receiver = targetHealth.GetComponent<IKnockbackReceiver>()
            ?? targetHealth.GetComponentInParent<IKnockbackReceiver>();
        if (receiver == null) return;

        Vector3 origin = _owner != null ? _owner.position : transform.position;
        Vector3 knockbackDir = targetHealth.transform.position - origin;
        receiver.ApplyKnockback(knockbackDir, _currentKnockback);
    }
}
