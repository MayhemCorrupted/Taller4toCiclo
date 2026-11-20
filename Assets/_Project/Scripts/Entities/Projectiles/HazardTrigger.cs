
using UnityEngine;

public enum HazardTriggerMode
{
    Projectile,
    ContactHazard
}

[RequireComponent(typeof(Collider))]
public class HazardTrigger : MonoBehaviour
{
    [Header("Modo de Trampa / Hazard")]
    [SerializeField] private HazardTriggerMode _mode = HazardTriggerMode.ContactHazard;
    [Tooltip("Si está activo, el trigger solo se activará una única vez en toda la partida.")]
    [SerializeField] private bool _triggerOnce = false;
    [Tooltip("Si está activo, seguirá dañando o disparando mientras el jugador permanezca dentro respetando el Cooldown.")]
    [SerializeField] private bool _retriggerWhileInside = true;
    [Tooltip("Tiempo mínimo en segundos entre cada activación del trigger.")]
    [Min(0.05f)][SerializeField] private float _cooldown = 1f;

    [Header("Daño y Empuje (Ambos Modos)")]
    [SerializeField] private float _damage = 15f;
    [Tooltip("Fuerza de retroceso aplicada al jugador si el golpe conecta (0 = sin knockback).")]
    [Min(0f)][SerializeField] private float _knockbackForce = 6f;

    [Header("Punto de Instancia (Transform)")]
    [Tooltip("En Projectile: punto y dirección de disparo. En ContactHazard: si tiene Transform y Visual Prefab, instancia el efecto visual aquí.")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Configuración: Modo Projectile")]
    [SerializeField] private EntityProjectile _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 10f;
    [SerializeField] private float _projectileLifetime = 4f;
    [Tooltip("True = apunta hacia la posición actual del jugador. False = dispara hacia el eje azul (Forward) del Spawn Point.")]
    [SerializeField] private bool _aimAtPlayer = false;

    [Header("Configuración: Modo ContactHazard (Visual)")]
    [Tooltip("Opcional: Prefab puramente visual (ej. pinchos, explosión, partículas) que se instancia en el Transform asignado.")]
    [SerializeField] private GameObject _visualPrefab;
    [Tooltip("Segundos antes de destruir el objeto visual instanciado (0 = no destruir automáticamente).")]
    [Min(0f)][SerializeField] private float _visualLifetime = 2f;
    private float _nextTriggerTime;
    private bool _hasTriggered;
    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        TryActivateHazard(other);
    }
    private void OnTriggerStay(Collider other)
    {
        if (!_retriggerWhileInside) return;
        TryActivateHazard(other);
    }
    private void TryActivateHazard(Collider other)
    {
        if (_triggerOnce && _hasTriggered) return;
        if (Time.time < _nextTriggerTime) return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        _hasTriggered = true;
        _nextTriggerTime = Time.time + _cooldown;

        if (_mode == HazardTriggerMode.Projectile)
        {
            ExecuteProjectileMode(playerHealth.transform);
        }
        else
        {
            ExecuteContactHazardMode(playerHealth);
        }
    }
    private void ExecuteProjectileMode(Transform playerTransform)
    {
        if (_projectilePrefab == null)
        {
            GameLogger.LogWarning($"[HazardTrigger] '{gameObject.name}' está en modo Projectile pero no tiene _projectilePrefab asignado.");
            return;
        }

        Transform origin = _spawnPoint != null ? _spawnPoint : transform;
        Vector3 fireDirection;

        if (_aimAtPlayer && playerTransform != null)
        {
            fireDirection = playerTransform.position - origin.position;
            fireDirection.y = 0f;
            if (fireDirection.sqrMagnitude < 0.0001f)
                fireDirection = origin.forward;
        }
        else
        {
            fireDirection = origin.forward;
            fireDirection.y = 0f;
        }

        fireDirection.Normalize();
        Quaternion spawnRotation = fireDirection != Vector3.zero ? Quaternion.LookRotation(fireDirection) : origin.rotation;

        EntityProjectile projectileInstance = Instantiate(_projectilePrefab, origin.position, spawnRotation);
        projectileInstance.Init(fireDirection, _projectileSpeed, _damage, _projectileLifetime, transform, _knockbackForce);
    }
    private void ExecuteContactHazardMode(PlayerHealth playerHealth)
    {
        if (_spawnPoint != null && _visualPrefab != null)
        {
            GameObject visualInstance = Instantiate(_visualPrefab, _spawnPoint.position, _spawnPoint.rotation);
            if (_visualLifetime > 0f)
            {
                Destroy(visualInstance, _visualLifetime);
            }
        }

        bool damageApplied = playerHealth.TakeDamage(_damage);

        if (damageApplied && _knockbackForce > 0f)
        {
            PlayerMovement playerMovement = playerHealth.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                Vector3 pushDirection = playerHealth.transform.position - transform.position;
                pushDirection.y = 0f;

                if (pushDirection.sqrMagnitude < 0.0001f)
                    pushDirection = -playerHealth.transform.forward;

                playerMovement.ApplyKnockback(pushDirection, _knockbackForce);
            }
        }
    }
}