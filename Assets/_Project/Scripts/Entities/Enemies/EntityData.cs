using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityData", menuName = "Rotbound/Entity/EntityData")]
public class EntityData : ScriptableObject
{
    [Header("Stats")]
    [Min(1)] public int health;
    [Min(0)] public int defense;
    [Min(0.0f)] public float speed;

    [Header("Detection Properties")]
    [Min(0.1f)] public float detectionRadius;
    [Min(0.1f)] public float attackRadius;
    [Min(0.1f)] public float lossRadius;

    [Header("Combat Properties")]
    [Tooltip("El tiempo mínimo que el enemigo esperará antes de lanzar su siguiente ataque.")]
    [Min(0.01f)] public float minAttackCooldown = 1.0f;
    [Tooltip("El tiempo máximo que el enemigo esperará antes de lanzar su siguiente ataque.")]
    [Min(0.01f)] public float maxAttackCooldown = 2.0f;
    public List<EntityAttack> attacks = new();
}