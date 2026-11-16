using System;
using System.Collections.Generic;
using UnityEngine;
public enum EnemyRank { Minion, Leader, MiniBoss, Boss }
public enum EnemyHealthBarMode { ScreenUI, WorldSpace }
[Serializable] public class AttackDistancePool
{
    [Header("Rangos de Distancia")]
    public float closeRangeMax = 3f;
    public float mediumRangeMax = 8f;

    [Header("Listas de Ataques")]
    public List<EntityAttack> closeRangeAttacks = new();
    public List<EntityAttack> mediumRangeAttacks = new();
    public List<EntityAttack> farRangeAttacks = new();
}
[Serializable] public class EnemyPhase
{
    public string phaseName = "Fase 1";
    [Range(0f, 1f)] public float healthThresholdPercentage = 1.0f;
    public AttackDistancePool attackPool;
}
[Serializable] public struct VesaniaAgonizeSetting
{
    [Tooltip("Nivel de Vesania del jugador (1 a 4).")]
    [Range(1, 4)] public int vesaniaLevel;
    [Tooltip("Probabilidad (0 a 1) de activar Agonize en este nivel de Vesania.")]
    [Range(0f, 1f)] public float agonizeProbability;
    [Tooltip("Cooldown en segundos después de reproducir Agonize en este nivel de Vesania.")]
    [Min(0f)] public float agonizeCooldown;
}
[CreateAssetMenu(fileName = "EntityData", menuName = "Rotbound/Entity/EntityData")]
public class EntityData : ScriptableObject
{
    [Header("Identidad del Enemigo")]
    public EnemyRank rank = EnemyRank.Minion;

    [Tooltip("0: Ninguno, 1: Corta, 2: Media, 3: Larga")]
    [Range(0, 3)] public int preferredAttackDistance = 0;

    [Header("Interfaz de Vida (HUD)")]
    [Tooltip("ScreenUI: Barra en el Canvas al detectar/fijar. WorldSpace: Barra persistente encima del enemigo.")]
    public EnemyHealthBarMode healthBarMode = EnemyHealthBarMode.WorldSpace;

    [Header("Stats Base")]
    [Min(1)] public int health = 100;
    [Min(0)] public int defense = 10;
    [Min(0.0f)] public float speed = 3.5f;

    [Header("Estado de Armadura y Reacción al Daño")]
    [Tooltip("Define si el enemigo puede ser interrumpido y aturdido al recibir daño.")]
    public bool isStunnable = true;
    public ArmorType defaultArmorState = ArmorType.None;
    [Range(0f, 1f)] public float defaultHyperarmorMultiplier = 0.5f;

    [Header("Mecánica de Agonía (Agonize)")]
    [Tooltip("Confirma si este enemigo posee y puede reproducir la animación Agonize.")]
    public bool canAgonize = false;
    [Tooltip("Configuración de probabilidad y cooldown por cada fase de Vesania (1 a 4).")]
    public List<VesaniaAgonizeSetting> vesaniaAgonizeSettings = new();

    [Header("Respaldo de Agonía (Si la lista de Vesania está vacía)")]
    [Min(0f)] public float defaultAgonizeCooldown = 10f;
    [Range(0f, 1f)] public float defaultAgonizeProbability = 0.25f;

    [Header("Detection Properties")]
    [Min(0.1f)] public float detectionRadius = 10f;
    [Min(0.1f)] public float attackRadius = 8f;
    [Min(0.1f)] public float lossRadius = 15f;

    [Header("Combat Cooldowns")]
    [Min(0.01f)] public float minAttackCooldown = 1.0f;
    [Min(0.01f)] public float maxAttackCooldown = 2.0f;

    [Header("Fases de Enemigo (Ordenadas de > % HP a < % HP)")]
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
    public void GetPreferredRangeBand(EnemyPhase phase, out float minRange, out float optimalRange, out float maxRange)
    {
        float closeMax = phase != null ? phase.attackPool.closeRangeMax : Mathf.Min(3f, attackRadius);
        float mediumMax = phase != null ? phase.attackPool.mediumRangeMax : Mathf.Min(8f, attackRadius);
        float farMax = Mathf.Max(attackRadius, mediumMax * 1.5f);

        switch (preferredAttackDistance)
        {
            case 1:
                minRange = Mathf.Max(1.1f, closeMax * 0.35f);
                maxRange = closeMax;
                optimalRange = Mathf.Clamp(closeMax * 0.75f, minRange, maxRange);
                break;

            case 2:
                minRange = Mathf.Max(1.5f, closeMax * 0.9f);
                maxRange = Mathf.Max(minRange + 1.5f, mediumMax);
                optimalRange = (minRange + maxRange) * 0.5f;
                break;

            case 3:
                minRange = Mathf.Max(2.5f, mediumMax * 0.9f);
                maxRange = Mathf.Max(minRange + 2.5f, farMax);
                optimalRange = Mathf.Lerp(minRange, maxRange, 0.55f);
                break;

            default:
                minRange = 1.2f;
                maxRange = attackRadius;
                optimalRange = Mathf.Clamp(attackRadius * 0.75f, minRange, maxRange);
                break;
        }
    }
}