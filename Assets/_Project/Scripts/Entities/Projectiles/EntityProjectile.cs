using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EntityProjectile : MonoBehaviour
{
    [SerializeField] private LayerMask _wallLayer;
    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private Transform _owner;
    private bool _isInitialized = false;
    public void Init(Vector3 direction, float speed, float dmg, float lifetime, Transform owner)
    {
        _direction = direction;
        _direction.y = 0f; 
        _direction.Normalize();

        _speed = speed;
        _damage = dmg;
        _owner = owner;
        _isInitialized = true;

        Destroy(gameObject, lifetime);
    }
    private void Update()
    {
        if (!_isInitialized) return;
        transform.position += _speed * Time.deltaTime * _direction;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_owner != null && (other.transform == _owner || other.transform.IsChildOf(_owner)))
            return;

        if (other.CompareTag("Enemy"))
            return;

        if (_wallLayer != 0 && ((1 << other.gameObject.layer) & _wallLayer) != 0)
        {
            Destroy(gameObject);
            return;
        }

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}