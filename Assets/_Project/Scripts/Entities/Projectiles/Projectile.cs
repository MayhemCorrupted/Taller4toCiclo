using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private float _damage;
    private Rigidbody _rb;
    private Transform _owner;
    public void Init(Vector3 velocity, float dmg, float lifetime, Transform owner)
    {
        _rb = GetComponent<Rigidbody>();
        _damage = dmg;
        _owner = owner;

        _rb.linearVelocity = velocity;
        StartCoroutine(PendingDestruction(lifetime));
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_owner != null && (other.transform == _owner || other.transform.IsChildOf(_owner)))
            return;

        if (other.CompareTag("Enemy"))
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(_damage);
        }

        Destroy(gameObject);
    }
    private IEnumerator PendingDestruction(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
}