using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttack", menuName = "Scriptable Objects/EnemyAttack")]
public abstract class EnemyAttack : ScriptableObject
{
    [Header("GENERAL PROPERTIES")]

    [Min(0.0f)] public float damage;
    [Min(0.0f)] public float knockback;
    [Min(0.01f)] public float cooldown;

    public abstract void Execute(Transform origin, Transform target);
}
