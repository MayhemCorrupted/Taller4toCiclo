using System.Collections;
using UnityEngine;

public enum ArmorType { None, Superarmor, Hyperarmor }

public abstract class EntityAttack : ScriptableObject
{
    [Header("GENERAL PROPERTIES")]
    [Min(0.0f)] public float damage;
    [Min(0.0f)] public float knockback;
    [Min(0.0f)] public float telegraphDuration;
    [Min(0.0f)] public float attackDuration;
    [Tooltip("1 = Trackea todo el tiempo de preparación. 0.5 = Trackea solo la primera mitad.")]
    [Range(0f, 1f)] public float trackTargetUntil = 1.0f;

    [Header("ARMOR OVERRIDE PROPERTIES")]
    [Tooltip("Establecer si este ataque modifica el estado de armadura base del enemigo.")]
    public bool overrideBaseArmor = false;
    public ArmorType armorState = ArmorType.None;
    [Tooltip("Sólo se aplica si el ArmorType es Hyperarmor. 0 = Invulnerable, 1 = Daño Normal.")]
    [Range(0f, 1f)] public float hyperarmorDamageMultiplier = 0.5f;

    [Header("ANIMATION & EXECUTION SYNC")]
    public string animationTrigger;
    [Tooltip("Tiempo de espera (en segundos) durante la ejecución antes de aplicar el daño/hitbox para sincronizar con el frame de impacto de la animación.")]
    [Min(0f)] public float damageDelay = 0f;
    public virtual void Prepare(Transform origin, Transform target) { }
    public virtual void UpdatePreparation(Transform origin, Transform target, float normalizedTime) { }
    public abstract IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner);
    public virtual void CancelAttack() { }
    protected IEnumerator WaitDamageDelay()
    {
        if (damageDelay > 0f)
        {
            yield return new WaitForSeconds(damageDelay);
        }
    }
    protected bool ApplyDamageAndKnockback(Collider hitCollider, Vector3 knockbackDirection, Vector3 fallbackOrigin)
    {
        if (hitCollider == null) return false;

        PlayerHealth playerHealth = hitCollider.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return false;

        if (!playerHealth.TakeDamage(damage))
            return false;

        if (knockback > 0f)
        {
            if (playerHealth.TryGetComponent<PlayerMovement>(out var playerMov))
            {
                Vector3 flatDir = knockbackDirection;
                flatDir.y = 0f;

                if (flatDir.sqrMagnitude < 0.001f)
                {
                    flatDir = playerHealth.transform.position - fallbackOrigin;
                    flatDir.y = 0f;
                }

                if (flatDir.sqrMagnitude > 0.001f)
                {
                    playerMov.ApplyKnockback(flatDir.normalized, knockback);
                }
            }
        }

        return true;
    }
}