using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    public event Action OnEntityHit;
    private float _currentDamage;
    private Collider _hitboxCollider;
    private readonly List<Collider> _alreadyHitEntities = new();
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
        if (_alreadyHitEntities.Contains(other)) return;

        if (other.TryGetComponent(out Health targetHealth))
        {
            targetHealth.TakeDamage(_currentDamage);
            _alreadyHitEntities.Add(other);
            OnEntityHit?.Invoke();
        }
    }
}