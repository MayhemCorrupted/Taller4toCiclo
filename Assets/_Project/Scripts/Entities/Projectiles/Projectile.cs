using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private float _damage;
    private Rigidbody _rb;
    public void Init(Vector3 velocity, float dmg, float lifetime)
    {
        _rb = GetComponent<Rigidbody>();
        _rb.linearVelocity = velocity;
        _damage = dmg;

        StartCoroutine(PendingDestruction(lifetime));
    }
    private void OnTriggerEnter(Collider other)
    {
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