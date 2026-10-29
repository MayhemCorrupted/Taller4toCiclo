using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    public event Action OnEnemyHit;
    private float _currentDamage;
    private Collider _hitboxCollider;
    private Rigidbody _hitboxRigidbody;

    private readonly List<Collider> _alreadyHitEnemies = new List<Collider>();
    private void Awake()
    {
        _hitboxCollider = GetComponent<Collider>();
        _hitboxCollider.isTrigger = true;
        _hitboxCollider.enabled = false;

        _hitboxRigidbody = GetComponent<Rigidbody>();
        if (_hitboxRigidbody == null)
        {
            _hitboxRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        _hitboxRigidbody.isKinematic = true;
        _hitboxRigidbody.useGravity = false;
        _hitboxRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }
    public void SetDamage(float damage)
    {
        _currentDamage = damage;
    }
    public void EnableHitbox()
    {
        _alreadyHitEnemies.Clear();
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
    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }
    private void TryHit(Collider other)
    {
        if (other == null || _alreadyHitEnemies.Contains(other)) return;
        if (other.transform.root == transform.root) return;

        HealthSystem targetHealth = other.GetComponentInParent<HealthSystem>();
        if (targetHealth == null) return;

        targetHealth.TakeDamage(_currentDamage);
        _alreadyHitEnemies.Add(other);
        OnEnemyHit?.Invoke();
    }
}