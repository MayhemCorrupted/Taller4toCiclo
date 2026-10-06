using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    private float _currentDamage;
    private Collider _hitboxCollider;
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
        _hitboxCollider.enabled = true;
    }
    public void DisableHitbox()
    {
        _hitboxCollider.enabled = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out HealthSystem targetHealth))
        {
            targetHealth.TakeDamage(_currentDamage);
        }
    }
}
