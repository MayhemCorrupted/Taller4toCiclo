using System;
using System.Collections.Generic;
using UnityEngine;
public enum EnemyRank { Weak, Strong, Leader, MiniBoss, Boss }
public enum EnemyCombatArchetype { Melee, Ranged, Versatile }
public enum EnemyHealthBarMode { ScreenUI, WorldSpace }
public enum AttackRangeCategory { None, Close, Medium, Far }
[Serializable] public class AttackDistancePool
{
    [Header("Ataques y Cooldown de Rotación: Corto Alcance")]
    public List<EntityAttack> closeRangeAttacks = new();
    [Min(0.05f)] public float closeRotationCooldown = 1.2f;

    [Header("Ataques y Cooldown de Rotación: Medio Alcance")]
    public List<EntityAttack> mediumRangeAttacks = new();
    [Min(0.05f)] public float mediumRotationCooldown = 2.0f;

    [Header("Ataques y Cooldown de Rotación: Largo Alcance")]
    public List<EntityAttack> farRangeAttacks = new();
    [Min(0.05f)] public float farRotationCooldown = 3.0f;
}
[Serializable] public class EnemyPhase
{
    public string phaseName = "Fase 1";
    [Range(0f, 1f)] public float healthThresholdPercentage = 1.0f;
    public AttackDistancePool attackPool = new();
}
[Serializable] public struct VesaniaAgonizeSetting
{
    [Range(1, 4)] public int vesaniaLevel;
    [Range(0f, 1f)] public float agonizeProbability;
    [Min(0f)] public float agonizeCooldown;
}
[CreateAssetMenu(fileName = "EntityData", menuName = "Rotbound/Entity/EntityData")]
public class EntityData : ScriptableObject
{
    [Header("Identidad, Rango y Arquetipo")]
    public EnemyRank rank = EnemyRank.Weak;
    public EnemyCombatArchetype combatArchetype = EnemyCombatArchetype.Melee;
    public AttackRangeCategory preferredCombatRange = AttackRangeCategory.None;

    [Header("Interfaz de Vida")]
    public EnemyHealthBarMode healthBarMode = EnemyHealthBarMode.WorldSpace;

    [Header("Estadísticas Base")]
    [Min(1)] public int health = 100;
    [Min(0)] public int defense = 10;
    [Min(0.1f)] public float moveSpeed = 3.5f;

    [Header("Armadura y Aturdimiento")]
    public bool isStunnable = true;
    public ArmorType defaultArmorState = ArmorType.None;
    [Range(0f, 1f)] public float defaultHyperarmorMultiplier = 0.5f;

    [Header("Mecánica de Agonía")]
    public bool canAgonize = false;
    public List<VesaniaAgonizeSetting> vesaniaAgonizeSettings = new();
    [Min(0f)] public float defaultAgonizeCooldown = 10f;
    [Range(0f, 1f)] public float defaultAgonizeProbability = 0.25f;

    [Header("Fases de Combate (Ordenadas de > % HP a < % HP)")]
    public List<EnemyPhase> phases = new();

    public void GetAgonizeParameters(int vesaniaLevel, out float probability, out float cooldown)
    {
        probability = defaultAgonizeProbability;
        cooldown = defaultAgonizeCooldown;

        if (vesaniaAgonizeSettings == null || vesaniaAgonizeSettings.Count == 0)
            return;

        int bestMatchedLevel = -1;
        for (int i = 0; i < vesaniaAgonizeSettings.Count; i++)
        {
            VesaniaAgonizeSetting setting = vesaniaAgonizeSettings[i];
            if (setting.vesaniaLevel == vesaniaLevel)
            {
                probability = setting.agonizeProbability;
                cooldown = setting.agonizeCooldown;
                return;
            }

            if (setting.vesaniaLevel < vesaniaLevel && setting.vesaniaLevel > bestMatchedLevel)
            {
                bestMatchedLevel = setting.vesaniaLevel;
                probability = setting.agonizeProbability;
                cooldown = setting.agonizeCooldown;
            }
        }

        if (bestMatchedLevel == -1)
        {
            probability = vesaniaAgonizeSettings[0].agonizeProbability;
            cooldown = vesaniaAgonizeSettings[0].agonizeCooldown;
        }
    }
}