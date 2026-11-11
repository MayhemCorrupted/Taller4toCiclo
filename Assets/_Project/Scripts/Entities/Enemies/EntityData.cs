using System;
using System.Collections.Generic;
using UnityEngine;
public enum EnemyRank { Minion, Leader, MiniBoss, Boss }
[Serializable]
public class AttackDistancePool
{
    [Header("Rangos de Distancia")]
    public float closeRangeMax = 3f;
    public float mediumRangeMax = 8f;

    [Header("Listas de Ataques")]
    public List<EntityAttack> closeRangeAttacks = new();
    public List<EntityAttack> mediumRangeAttacks = new();
    public List<EntityAttack> farRangeAttacks = new();
}
[Serializable]
public class EnemyPhase
{
    public string phaseName = "Fase 1";
    [Range(0f, 1f)] public float healthThresholdPercentage = 1.0f;
    public AttackDistancePool attackPool;
}
[CreateAssetMenu(fileName = "EntityData", menuName = "Rotbound/Entity/EntityData")]
public class EntityData : ScriptableObject
{
    [Header("Identidad del Enemigo")]
    public EnemyRank rank = EnemyRank.Minion;

    [Tooltip("0: Ninguno, 1: Corta, 2: Media, 3: Larga")]
    [Range(0, 3)] public int preferredAttackDistance = 0;

    [Header("Stats Base")]
    [Min(1)] public int health = 100;
    [Min(0)] public int defense = 10;
    [Min(0.0f)] public float speed = 3.5f;

    [Header("Estado de Armadura Base")]
    public ArmorType defaultArmorState = ArmorType.None;
    [Range(0f, 1f)] public float defaultHyperarmorMultiplier = 0.5f;

    [Header("Detection Properties")]
    [Min(0.1f)] public float detectionRadius = 10f;
    [Min(0.1f)] public float attackRadius = 8f;
    [Min(0.1f)] public float lossRadius = 15f;

    [Header("Combat Cooldowns")]
    [Min(0.01f)] public float minAttackCooldown = 1.0f;
    [Min(0.01f)] public float maxAttackCooldown = 2.0f;

    [Header("Fases de Enemigo (Ordenadas de > % HP a < % HP)")]
    public List<EnemyPhase> phases = new();
}