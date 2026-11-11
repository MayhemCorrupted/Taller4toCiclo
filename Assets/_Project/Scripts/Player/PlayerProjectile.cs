using UnityEngine;
using System.Collections.Generic; 
[RequireComponent(typeof(Collider))]
public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private float _lifetime = 3f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _wallLayer;

    private Vector3 _direction;
    private float _damage;
    private bool _isInitialized = false;
    private readonly HashSet<Health> _hitEnemies = new();
    public void Init(Vector3 direction, float damageAmount)
    {
        _direction = direction.normalized;
        _damage = damageAmount;
        _isInitialized = true;
        Destroy(gameObject, _lifetime);
    }
    private void Update()
    {
        if (!_isInitialized) return;
        transform.position += _speed * Time.deltaTime * _direction;
    }
    private void OnTriggerEnter(Collider other)
    {
        int otherLayer = 1 << other.gameObject.layer;

        if ((otherLayer & _wallLayer) != 0)
        {
            Destroy(gameObject);
            return;
        }
        if ((otherLayer & _enemyLayer) != 0)
        {
            if (other.TryGetComponent(out Health enemyHealth))
            {
                if (!_hitEnemies.Contains(enemyHealth))
                {
                    enemyHealth.TakeDamage(_damage);
                    _hitEnemies.Add(enemyHealth);
                }
            }
        }
    }
}