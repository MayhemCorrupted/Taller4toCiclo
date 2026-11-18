using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class EntityDirector : MonoBehaviour
{
    private static EntityDirector _instance;
    public static EntityDirector Instance => _instance;

    private readonly List<EntityCombat> _registeredEnemies = new();
    private readonly List<EntityCombat> _attackWaitList = new();
    private EntityCombat _currentAttacker;

    [Header("Turnos y Cadencia Global")]
    [Min(0f)] public float globalAttackDelay = 0.45f;

    [Header("Formación Melee (Rodeo y Corte de Paso)")]
    [Min(0.5f)] public float meleeSurroundRadius = 2.8f;
    [Min(0f)] public float ambushPredictionTime = 0.65f;

    [Header("Formación Ranged (Fila Frontal)")]
    [Min(1f)] public float rangedLineDistance = 8.5f;
    [Min(0.5f)] public float rangedLineSpacing = 2.2f;

    [Header("Jerarquía, Escolta y Boss")]
    [Min(0.5f)] public float minSeparationDistance = 2.2f;
    [Min(1f)] public float bodyguardRadius = 2.6f;
    [Min(1.5f)] public float shieldRingRadius = 4.2f;
    [Min(4f)] public float bossPassiveStandoffRadius = 13.0f;
    private float _nextGlobalAttackTime = 0f;
    private bool _isBossCommandActive = false;
    private EnemyRank _commandedMaxRank = EnemyRank.Boss;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    public void RegisterEnemy(EntityCombat enemy)
    {
        if (enemy != null && !_registeredEnemies.Contains(enemy))
            _registeredEnemies.Add(enemy);
    }
    public void UnregisterEnemy(EntityCombat enemy)
    {
        if (_registeredEnemies.Contains(enemy))
            _registeredEnemies.Remove(enemy);

        _attackWaitList.Remove(enemy);

        if (_currentAttacker == enemy)
            NotifyAttackFinished(enemy);
    }
    public EnemyRank GetHighestRankPresent()
    {
        EnemyRank highest = EnemyRank.Weak;
        for (int i = 0; i < _registeredEnemies.Count; i++)
        {
            if (_registeredEnemies[i] != null && _registeredEnemies[i].HasTarget && _registeredEnemies[i].Rank > highest)
                highest = _registeredEnemies[i].Rank;
        }
        return highest;
    }
    private EntityCombat GetHighestRankLeaderEntity(EnemyRank minRank)
    {
        EntityCombat best = null;
        for (int i = 0; i < _registeredEnemies.Count; i++)
        {
            EntityCombat e = _registeredEnemies[i];
            if (e != null && e.HasTarget && e.Rank >= minRank)
            {
                if (best == null || e.Rank > best.Rank)
                    best = e;
            }
        }
        return best;
    }
    public Vector3 GetTacticalPosition(EntityCombat enemy, Transform targetTransform, Vector3 targetVelocity)
    {
        if (targetTransform == null) return enemy.transform.position;

        EnemyRank highestRank = GetHighestRankPresent();
        Vector3 playerPos = targetTransform.position;
        Vector3 flatVel = new(targetVelocity.x, 0f, targetVelocity.z);
        Vector3 predictedPlayerPos = flatVel.sqrMagnitude > 0.5f
            ? playerPos + flatVel * ambushPredictionTime
            : playerPos;

        if (highestRank == EnemyRank.Boss)
        {
            EntityCombat boss = GetHighestRankLeaderEntity(EnemyRank.Boss);
            if (enemy.Rank == EnemyRank.Boss)
            {
                if (_isBossCommandActive)
                    return GetArchetypeCombatPosition(enemy, playerPos, predictedPlayerPos);

                Vector3 awayFromPlayer = (enemy.transform.position - playerPos);
                awayFromPlayer.y = 0f;
                awayFromPlayer = awayFromPlayer.sqrMagnitude > 0.01f ? awayFromPlayer.normalized : -targetTransform.forward;
                return playerPos + awayFromPlayer * bossPassiveStandoffRadius;
            }

            if (!_isBossCommandActive && boss != null)
            {
                return GetEscortPositionAroundVIP(enemy, boss.transform.position, playerPos, bodyguardRadius);
            }
        }

        if (highestRank == EnemyRank.MiniBoss && enemy.Rank != EnemyRank.MiniBoss)
        {
            EntityCombat miniBoss = GetHighestRankLeaderEntity(EnemyRank.MiniBoss);
            if (miniBoss != null && enemy.Rank == EnemyRank.Leader)
            {
                return GetEscortPositionAroundVIP(enemy, miniBoss.transform.position, playerPos, bodyguardRadius);
            }
        }

        if (highestRank == EnemyRank.Leader && (enemy.Rank == EnemyRank.Strong || enemy.Rank == EnemyRank.Weak))
        {
            EntityCombat leader = GetHighestRankLeaderEntity(EnemyRank.Leader);
            if (leader != null)
            {
                float escortRadius = enemy.Rank == EnemyRank.Strong ? bodyguardRadius : shieldRingRadius;
                return GetEscortPositionAroundVIP(enemy, leader.transform.position, playerPos, escortRadius);
            }
        }

        if (highestRank == EnemyRank.Strong && enemy.Rank == EnemyRank.Weak)
        {
            EntityCombat strongAlly = GetHighestRankLeaderEntity(EnemyRank.Strong);
            if (strongAlly != null)
            {
                Vector3 toPlayer = (playerPos - strongAlly.transform.position);
                toPlayer.y = 0f;
                Vector3 dir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector3.forward;
                Vector3 basePos = GetArchetypeCombatPosition(enemy, playerPos, predictedPlayerPos);
                return Vector3.Lerp(basePos, strongAlly.transform.position - dir * 1.4f, 0.35f);
            }
        }

        return GetArchetypeCombatPosition(enemy, playerPos, predictedPlayerPos);
    }
    private Vector3 GetEscortPositionAroundVIP(EntityCombat escort, Vector3 vipPos, Vector3 playerPos, float radius)
    {
        List<EntityCombat> escortsOfSameRank = _registeredEnemies
            .Where(e => e != null && e.HasTarget && e.Rank == escort.Rank)
            .ToList();

        int count = Mathf.Max(1, escortsOfSameRank.Count);
        int index = Mathf.Max(0, escortsOfSameRank.IndexOf(escort));

        Vector3 vipToPlayer = playerPos - vipPos;
        vipToPlayer.y = 0f;
        Vector3 forwardDir = vipToPlayer.sqrMagnitude > 0.01f ? vipToPlayer.normalized : Vector3.forward;
        Quaternion baseRot = Quaternion.LookRotation(forwardDir);

        float spreadAngle = Mathf.Lerp(-110f, 110f, count == 1 ? 0.5f : index / (float)(count - 1));
        Vector3 offset = baseRot * (Quaternion.Euler(0f, spreadAngle, 0f) * Vector3.forward) * radius;
        return vipPos + offset;
    }
    private Vector3 GetArchetypeCombatPosition(EntityCombat enemy, Vector3 playerPos, Vector3 predictedPlayerPos)
    {
        EnemyCombatArchetype archetype = enemy.CombatArchetype;
        float preferredRadius = enemy.GetDesiredCombatHoldRadius();

        if (archetype == EnemyCombatArchetype.Versatile)
        {
            archetype = enemy.DistanceToTarget > preferredRadius * 1.25f
                ? EnemyCombatArchetype.Ranged
                : EnemyCombatArchetype.Melee;
        }

        if (archetype == EnemyCombatArchetype.Ranged)
        {
            List<EntityCombat> rangedGroup = _registeredEnemies
                .Where(e => e != null && e.HasTarget && (e.CombatArchetype == EnemyCombatArchetype.Ranged || e.CombatArchetype == EnemyCombatArchetype.Versatile))
                .ToList();

            int count = Mathf.Max(1, rangedGroup.Count);
            int index = Mathf.Max(0, rangedGroup.IndexOf(enemy));

            Vector3 groupCenter = Vector3.zero;
            for (int i = 0; i < rangedGroup.Count; i++) groupCenter += rangedGroup[i].transform.position;
            groupCenter /= count;

            Vector3 fromPlayerToLine = groupCenter - playerPos;
            fromPlayerToLine.y = 0f;
            Vector3 lineNormal = fromPlayerToLine.sqrMagnitude > 0.01f ? fromPlayerToLine.normalized : -enemy.transform.forward;
            Vector3 lineTangent = Vector3.Cross(Vector3.up, lineNormal).normalized;

            float lineDist = Mathf.Max(preferredRadius, rangedLineDistance);
            float lateralOffset = (index - (count - 1) * 0.5f) * rangedLineSpacing;

            return playerPos + (lineNormal * lineDist) + (lineTangent * lateralOffset);
        }

        List<EntityCombat> meleeGroup = _registeredEnemies
            .Where(e => e != null && e.HasTarget && (e.CombatArchetype == EnemyCombatArchetype.Melee || e.CombatArchetype == EnemyCombatArchetype.Versatile))
            .ToList();

        int meleeCount = Mathf.Max(1, meleeGroup.Count);
        int meleeIndex = Mathf.Max(0, meleeGroup.IndexOf(enemy));
        float surroundRadius = Mathf.Max(preferredRadius, meleeSurroundRadius);

        if (meleeCount > 4)
            surroundRadius += (meleeCount - 4) * 0.35f;

        float angle = (meleeIndex / (float)meleeCount) * Mathf.PI * 2f;
        Vector3 circleOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * surroundRadius;
        return predictedPlayerPos + circleOffset;
    }
    public Vector3 GetSeparationVector(EntityCombat enemy)
    {
        Vector3 separation = Vector3.zero;
        for (int i = 0; i < _registeredEnemies.Count; i++)
        {
            EntityCombat other = _registeredEnemies[i];
            if (other == enemy || other == null) continue;

            Vector3 diff = enemy.transform.position - other.transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;

            if (dist > 0.05f && dist < minSeparationDistance)
            {
                separation += diff.normalized * (minSeparationDistance - dist);
            }
        }
        return separation;
    }
    public bool RequestAttackPermission(EntityCombat enemy)
    {
        EnemyRank highestRank = GetHighestRankPresent();

        if (highestRank == EnemyRank.Boss)
        {
            if (!_isBossCommandActive)
                return false;

            if (enemy.Rank > _commandedMaxRank && enemy.Rank != EnemyRank.Boss)
                return false;
        }

        if (_currentAttacker == enemy)
            return true;

        _attackWaitList.RemoveAll(e => e == null || !e.HasTarget);

        if (!_attackWaitList.Contains(enemy))
            _attackWaitList.Add(enemy);

        if (_currentAttacker == null && Time.time >= _nextGlobalAttackTime)
        {
            EntityCombat closestCandidate = _attackWaitList
                .OrderBy(e => e.DistanceToTarget)
                .FirstOrDefault();

            if (closestCandidate == enemy)
            {
                _attackWaitList.Remove(enemy);
                _currentAttacker = enemy;
                return true;
            }
        }

        return false;
    }
    public void NotifyAttackFinished(EntityCombat enemy)
    {
        if (_currentAttacker == enemy || _currentAttacker == null)
        {
            _currentAttacker = null;
            _nextGlobalAttackTime = Time.time + globalAttackDelay;
        }
    }
    public void CommandAlliesToAttack(EnemyRank targetRank, float duration)
    {
        _isBossCommandActive = true;
        _commandedMaxRank = targetRank;
        CancelInvoke(nameof(ResetBossCommand));
        Invoke(nameof(ResetBossCommand), duration);
    }

    private void ResetBossCommand()
    {
        _isBossCommandActive = false;
    }
}