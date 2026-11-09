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
    [Tooltip("1 = Trackea todo el tiempo de preparación. 0.5 = Trackea solo la primera mitad y luego se fija.")]
    [Range(0f, 1f)] public float trackTargetUntil = 1.0f;
    [Header("ARMOR PROPERTIES")]
    public ArmorType armorState = ArmorType.None;
    [Tooltip("Sólo se aplica si el ArmorType es Hyperarmor. 0 = Invulnerable, 1 = Daño Normal.")]
    [Range(0f, 1f)] public float hyperarmorDamageMultiplier = 0.5f;
    [Header("ANIMATION")]
    public string animationTrigger;
    protected Health _entityHealth;
    public virtual void Prepare(Transform origin, Transform target) { }
    public virtual void UpdatePreparation(Transform origin, Transform target, float normalizedTime) { }
    public abstract IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner);
    public virtual void CancelAttack() { }
}