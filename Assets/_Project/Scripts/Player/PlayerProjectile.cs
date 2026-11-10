using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private float _lifetime = 3f; 
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _wallLayer;

    private Vector3 _direction;
    private float _damage;
    public void Init(Vector3 direction, float damageAmount)
    {
        _direction = direction.normalized;
        _damage = damageAmount;
        Destroy(gameObject, _lifetime); 
    }
    private void Update()
    {
        transform.position += _speed * Time.deltaTime * _direction;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _wallLayer) != 0)
        {
            Destroy(gameObject);
            return;
        }

        if (((1 << other.gameObject.layer) & _enemyLayer) != 0)
        {
            if (other.TryGetComponent(out Health enemyHealth))
            {
                enemyHealth.TakeDamage(_damage);
            }
        }
    }
}
