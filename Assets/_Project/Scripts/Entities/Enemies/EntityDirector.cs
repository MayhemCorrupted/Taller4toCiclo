using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class EntityDirector : MonoBehaviour
{
    private static EntityDirector _instance;
    public static EntityDirector Instance => _instance;

    private readonly List<EntityCombat> _registeredEnemies = new();
    private readonly Queue<EntityCombat> _attackQueue = new();
    private EntityCombat _currentAttacker;

    [Header("Flocking / Flanking Settings")]
    public float globalAttackDelay = 0.6f;
    public float baseSurroundRadius = 4f;
    public float repelledSurroundRadius = 12f;

    [Header("Ambush & Separation Settings")]
    [Tooltip("Tiempo en segundos para predecir el movimiento del jugador. A mayor valor, más adelante se posicionan intentando cortarle el paso.")]
    public float ambushPredictionTime = 0.8f;
    [Tooltip("Distancia mínima que los enemigos intentarán mantener entre sí para no amontonarse.")]
    public float minSeparationDistance = 2.5f;

    private float _nextGlobalAttackTime = 0f;
    private bool _areMinionsCommandedToAttack = false;
    private bool _areLeadersCommandedToAttack = false;
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
        if (!_registeredEnemies.Contains(enemy)) _registeredEnemies.Add(enemy);
    }
    public void UnregisterEnemy(EntityCombat enemy)
    {
        if (_registeredEnemies.Contains(enemy)) _registeredEnemies.Remove(enemy);
        if (_attackQueue.Contains(enemy))
        {
            var temp = new List<EntityCombat>(_attackQueue);
            temp.Remove(enemy);
            _attackQueue.Clear();
            foreach (var e in temp) _attackQueue.Enqueue(e);
        }
        if (_currentAttacker == enemy) NotifyAttackFinished(enemy);
    }
    public EnemyRank GetHighestRankPresent()
    {
        if (_registeredEnemies.Count == 0) return EnemyRank.Minion;
        return _registeredEnemies.Max(e => e.Rank);
    }
    public Vector3 GetTacticalPosition(EntityCombat enemy, Transform targetTransform, Vector3 targetVelocity)
    {
        if (targetTransform == null) return enemy.transform.position;

        EnemyRank currentHighestRank = GetHighestRankPresent();

        Vector3 predictedCenter = targetTransform.position;
        Vector3 flatVelocity = new(targetVelocity.x, 0, targetVelocity.z);
        if (flatVelocity.sqrMagnitude > 0.5f)
        {
            predictedCenter += flatVelocity * ambushPredictionTime;
        }

        if (enemy.Rank == EnemyRank.Leader && currentHighestRank != EnemyRank.Boss) return predictedCenter;
        if (enemy.Rank == EnemyRank.MiniBoss || enemy.Rank == EnemyRank.Boss) return predictedCenter;

        List<EntityCombat> flankingEnemies = _registeredEnemies.Where(e => e.Rank == EnemyRank.Minion).ToList();
        float radiusToUse = (currentHighestRank >= EnemyRank.MiniBoss) ? repelledSurroundRadius : baseSurroundRadius;

        if (flankingEnemies.Count > 4) radiusToUse += (flankingEnemies.Count - 4) * 0.4f;

        int index = flankingEnemies.IndexOf(enemy);
        int total = Mathf.Max(1, flankingEnemies.Count);
        float angle = (index / (float)total) * Mathf.PI * 2f;

        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radiusToUse;
        return predictedCenter + offset;
    }
    public Vector3 GetSeparationVector(EntityCombat enemy)
    {
        Vector3 separation = Vector3.zero;
        foreach (var otherEnemy in _registeredEnemies)
        {
            if (otherEnemy == enemy || otherEnemy == null) continue;

            Vector3 dirToOther = enemy.transform.position - otherEnemy.transform.position;
            dirToOther.y = 0; 
            float dist = dirToOther.magnitude;

            if (dist > 0.05f && dist < minSeparationDistance)
            {
                separation += (dirToOther.normalized) * (minSeparationDistance - dist);
            }
        }
        return separation;
    }
    public bool RequestAttackPermission(EntityCombat enemy)
    {
        EnemyRank currentHighestRank = GetHighestRankPresent();

        if (currentHighestRank == EnemyRank.Boss && enemy.Rank != EnemyRank.Boss)
        {
            if (enemy.Rank == EnemyRank.Minion && !_areMinionsCommandedToAttack) return false;
            if (enemy.Rank == EnemyRank.Leader && !_areLeadersCommandedToAttack) return false;
        }

        if (currentHighestRank == EnemyRank.MiniBoss && enemy.Rank == EnemyRank.Minion) return false;
        if (_currentAttacker == enemy) return true;

        while (_attackQueue.Count > 0 && _attackQueue.Peek() == null) _attackQueue.Dequeue();

        if (_currentAttacker == null && Time.time >= _nextGlobalAttackTime)
        {
            if (_attackQueue.Count == 0 || _attackQueue.Peek() == enemy)
            {
                if (_attackQueue.Count > 0) _attackQueue.Dequeue();
                _currentAttacker = enemy;
                return true;
            }
        }

        if (!_attackQueue.Contains(enemy) && _currentAttacker != enemy) _attackQueue.Enqueue(enemy);
        return false;
    }
    public void NotifyAttackFinished(EntityCombat enemy)
    {
        if (_currentAttacker == enemy || _currentAttacker == null)
        {
            _currentAttacker = null;
            _nextGlobalAttackTime = Time.time + globalAttackDelay;
            while (_attackQueue.Count > 0 && _attackQueue.Peek() == null) _attackQueue.Dequeue();
        }
    }
    public void CommandAlliesToAttack(EnemyRank targetRank, float duration)
    {
        if (targetRank == EnemyRank.Minion)
        {
            _areMinionsCommandedToAttack = true;
            Invoke(nameof(ResetMinionCommand), duration);
        }
        else if (targetRank == EnemyRank.Leader)
        {
            _areLeadersCommandedToAttack = true;
            Invoke(nameof(ResetLeaderCommand), duration);
        }
    }
    private void ResetMinionCommand() => _areMinionsCommandedToAttack = false;
    private void ResetLeaderCommand() => _areLeadersCommandedToAttack = false;
}