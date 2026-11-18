using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
public class EntityMovement : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private EntityBrain _brain;
    [SerializeField] private EntityCombat _combat;
    [SerializeField] private LayerMask _wallLayer;

    [Header("Patrulla y Delay de Giro")]
    [Min(0.0f)] public float _patrolWaitingTime = 1.0f;
    [Min(0.1f)] public float _patrolRadius = 5.0f;
    [Min(0.0f)][SerializeField] private float _patrolTurnDelay = 0.25f;
    [Min(1f)][SerializeField] private float _patrolRotationSpeed = 6f;

    [Header("Rodeo Táctico, Strafe y Espacio Personal")]
    [SerializeField] private bool _enableOrbitalMovement = true;
    [Min(10f)][SerializeField] private float _orbitAngularSpeed = 45f;
    [Min(0.5f)][SerializeField] private float _orbitDirectionChangeInterval = 3.5f;
    [Min(0.5f)][SerializeField] private float _retreatSpeedMultiplier = 1.15f;
    [Min(1f)][SerializeField] private float _combatRotationSpeed = 14f;

    [Header("Reacción al Daño y Muerte")]
    [SerializeField] private float _stunDuration = 0.45f;
    [SerializeField] private float _knockbackForce = 12f;
    [SerializeField] private float _knockbackFriction = 10f;
    [SerializeField] private bool _disableCollidersOnDeath = true;

    private NavMeshAgent _navAgent;
    private Health _health;
    private Rigidbody _rb;
    private EntityData _entityData;

    private float _baseSpeed = 3.5f;
    private float _patrolWaitingTimer;
    private float _patrolTurnDelayTimer;
    private Vector3 _pendingPatrolDestination;
    private bool _hasPendingPatrolDestination;
    private bool _isWaiting;
    private Coroutine _stunCoroutine;

    private int _orbitDirection = 1;
    private float _orbitSwitchTimer;
    public bool IsKnockbackActive => _stunCoroutine != null;
    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _rb = GetComponent<Rigidbody>();

        if (_brain == null) TryGetComponent(out _brain);
        if (_combat == null) TryGetComponent(out _combat);

        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.freezeRotation = true;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        _navAgent.autoBraking = true;
        _navAgent.updateRotation = false;
        _navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        _orbitDirection = Random.value > 0.5f ? 1 : -1;
        _orbitSwitchTimer = Random.Range(_orbitDirectionChangeInterval * 0.6f, _orbitDirectionChangeInterval * 1.4f);
    }
    private void Start()
    {
        if (_brain != null)
        {
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnHurtTriggered += HandleHurtTriggered;
            if (_brain.CurrentState == EnemyState.Patroling) SetRandomPatrolPoint();
        }
    }
    private void OnDestroy()
    {
        if (_brain != null)
        {
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnHurtTriggered -= HandleHurtTriggered;
        }
    }
    public void SetData(EntityData data)
    {
        _entityData = data;
    }
    public void SetSpeed(float speed) => _baseSpeed = speed;
    private void Update()
    {
        if (!_navAgent.enabled || _brain == null) return;

        if (_combat != null && _combat.IsPerformingAttack)
        {
            StopAgentCompletely();
            return;
        }

        switch (_brain.CurrentState)
        {
            case EnemyState.Patroling:
                _navAgent.speed = _baseSpeed;
                HandlePatroling();
                break;
            case EnemyState.Chasing:
                HandleCombatPositioning();
                break;
            case EnemyState.Attacking:
                HandleAttacking();
                break;
            case EnemyState.Hurt:
            case EnemyState.Agonizing:
            case EnemyState.Death:
                StopAgentCompletely();
                break;
        }
    }
    private void StopAgentCompletely()
    {
        if (!_navAgent.enabled) return;
        _navAgent.isStopped = true;
        _navAgent.velocity = Vector3.zero;
        if (_navAgent.hasPath) _navAgent.ResetPath();
    }

    private void FlattenRotation()
    {
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
    }
    private void SetRandomPatrolPoint()
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * _patrolRadius;
            Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (TryGetValidNavMeshPoint(candidate, 2.5f, out Vector3 validPoint))
            {
                _pendingPatrolDestination = validPoint;
                _hasPendingPatrolDestination = true;
                _patrolTurnDelayTimer = _patrolTurnDelay;
                _navAgent.isStopped = true;
                return;
            }
        }
    }
    private void HandleStateChanged(EnemyState newState)
    {
        if (newState == EnemyState.Death)
        {
            if (_stunCoroutine != null)
            {
                StopCoroutine(_stunCoroutine);
                _stunCoroutine = null;
            }

            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
            _rb.useGravity = false;
            FlattenRotation();

            if (_navAgent.enabled)
            {
                StopAgentCompletely();
                _navAgent.enabled = false;
            }

            if (_disableCollidersOnDeath)
            {
                foreach (Collider col in GetComponentsInChildren<Collider>())
                {
                    if (!col.isTrigger) col.enabled = false;
                }
            }
            return;
        }

        if (newState == EnemyState.Agonizing)
        {
            if (_stunCoroutine != null)
            {
                StopCoroutine(_stunCoroutine);
                _stunCoroutine = null;
            }

            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
            _rb.useGravity = false;
            FlattenRotation();

            if (!_navAgent.enabled)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    transform.position = hit.position;
                _navAgent.enabled = true;
            }

            StopAgentCompletely();
            return;
        }

        if (!_navAgent.enabled) return;

        if (newState == EnemyState.Hurt)
        {
            StopAgentCompletely();
            return;
        }

        if (newState == EnemyState.Patroling)
        {
            _isWaiting = false;
            _hasPendingPatrolDestination = false;
            SetRandomPatrolPoint();
        }
        else if (newState == EnemyState.Chasing)
        {
            _hasPendingPatrolDestination = false;
            _navAgent.isStopped = false;
            _orbitDirection = Random.value > 0.5f ? 1 : -1;
            _orbitSwitchTimer = Random.Range(_orbitDirectionChangeInterval * 0.6f, _orbitDirectionChangeInterval * 1.4f);
        }
    }
    private void HandlePatroling()
    {
        if (_hasPendingPatrolDestination)
        {
            Vector3 dirToDest = _pendingPatrolDestination - transform.position;
            RotateSmoothly(dirToDest, _patrolRotationSpeed);

            _patrolTurnDelayTimer -= Time.deltaTime;
            if (_patrolTurnDelayTimer <= 0f)
            {
                _hasPendingPatrolDestination = false;
                _navAgent.isStopped = false;
                _navAgent.SetDestination(_pendingPatrolDestination);
            }
            return;
        }

        Vector3 moveDir = _navAgent.desiredVelocity.sqrMagnitude > 0.01f
            ? _navAgent.desiredVelocity
            : _navAgent.velocity;

        if (moveDir.sqrMagnitude > 0.02f)
        {
            RotateSmoothly(moveDir, _patrolRotationSpeed);
        }

        if (_isWaiting)
        {
            _patrolWaitingTimer -= Time.deltaTime;
            if (_patrolWaitingTimer <= 0.0f)
            {
                _isWaiting = false;
                SetRandomPatrolPoint();
            }
            return;
        }

        if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
        {
            if (!_navAgent.hasPath || _navAgent.velocity.sqrMagnitude < 0.01f)
            {
                _isWaiting = true;
                _patrolWaitingTimer = _patrolWaitingTime;
            }
        }
    }
    private void HandleAttacking()
    {
        if (_combat != null && _combat.IsPerformingAttack)
        {
            StopAgentCompletely();
            return;
        }

        if (_combat != null && _combat.PreferredCombatRange == AttackRangeCategory.None && _combat.CanAttackFromCurrentPosition)
        {
            if (_combat.DistanceToTarget >= _brain.PersonalSpaceRadius)
            {
                StopAgentCompletely();
                RotateSmoothly(_combat.TargetLocation - transform.position, _combatRotationSpeed);
                return;
            }
        }

        HandleCombatPositioning();
    }
    private void HandleCombatPositioning()
    {
        if (_combat == null || _combat.TargetTransform == null || _brain == null) return;

        Vector3 targetPos = _combat.TargetLocation;
        Vector3 toSelf = transform.position - targetPos;
        toSelf.y = 0f;
        float currentDist = toSelf.magnitude;
        Vector3 radialDir = currentDist > 0.01f ? toSelf / currentDist : -transform.forward;

        float personalSpace = _brain.PersonalSpaceRadius;
        float desiredHoldRadius = _combat.GetDesiredCombatHoldRadius();

        if (currentDist < personalSpace)
        {
            _navAgent.speed = _baseSpeed * _retreatSpeedMultiplier;
            float retreatDistance = Mathf.Max(personalSpace + 0.6f, desiredHoldRadius);
            Vector3 retreatGoal = targetPos + radialDir * retreatDistance;

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, radialDir, 1.5f, _wallLayer, QueryTriggerInteraction.Ignore))
            {
                Vector3 sideEscape = Vector3.Cross(Vector3.up, radialDir) * _orbitDirection;
                retreatGoal = transform.position + sideEscape * 2.5f;
            }

            if (TryGetValidNavMeshPoint(retreatGoal, 2.5f, out Vector3 safeRetreat))
            {
                _navAgent.isStopped = false;
                _navAgent.SetDestination(safeRetreat);
            }
        }
        else
        {
            _navAgent.speed = _baseSpeed;
            Vector3 tacticalGoal = EntityDirector.Instance != null
                ? EntityDirector.Instance.GetTacticalPosition(_combat, _combat.TargetTransform, _combat.TargetVelocity)
                : targetPos + radialDir * desiredHoldRadius;

            if (_enableOrbitalMovement && _combat.CombatArchetype != EnemyCombatArchetype.Ranged)
            {
                _orbitSwitchTimer -= Time.deltaTime;
                if (_orbitSwitchTimer <= 0f)
                {
                    _orbitDirection *= -1;
                    _orbitSwitchTimer = Random.Range(_orbitDirectionChangeInterval * 0.6f, _orbitDirectionChangeInterval * 1.4f);
                }

                Vector3 offsetFromTarget = tacticalGoal - targetPos;
                offsetFromTarget.y = 0f;
                if (offsetFromTarget.sqrMagnitude > 0.1f)
                {
                    float angleDelta = _orbitDirection * _orbitAngularSpeed * Time.deltaTime;
                    tacticalGoal = targetPos + (Quaternion.Euler(0f, angleDelta, 0f) * offsetFromTarget);
                }
            }

            if (EntityDirector.Instance != null)
                tacticalGoal += EntityDirector.Instance.GetSeparationVector(_combat);

            if (TryGetValidNavMeshPoint(tacticalGoal, 2.5f, out Vector3 validDest))
            {
                _navAgent.isStopped = false;
                _navAgent.SetDestination(validDest);
            }
            else if (TryGetValidNavMeshPoint(targetPos + radialDir * desiredHoldRadius, 2.5f, out Vector3 fallbackDest))
            {
                _navAgent.isStopped = false;
                _navAgent.SetDestination(fallbackDest);
            }
        }

        RotateSmoothly(targetPos - transform.position, _combatRotationSpeed);
    }
    private void RotateSmoothly(Vector3 flatDirection, float speed)
    {
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * speed);
        }
    }
    private bool TryGetValidNavMeshPoint(Vector3 desiredPoint, float searchRadius, out Vector3 resultPoint)
    {
        resultPoint = transform.position;

        if (!NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            return false;

        if (NavMesh.Raycast(transform.position, hit.position, out _, NavMesh.AllAreas))
            return false;

        Vector3 eyeOrigin = transform.position + Vector3.up * 0.6f;
        Vector3 eyeTarget = hit.position + Vector3.up * 0.6f;
        Vector3 dir = eyeTarget - eyeOrigin;
        float dist = dir.magnitude;

        if (dist > 0.1f && Physics.SphereCast(eyeOrigin, _navAgent.radius * 0.8f, dir / dist, out _, dist, _wallLayer, QueryTriggerInteraction.Ignore))
            return false;

        resultPoint = hit.position;
        return true;
    }
    private void HandleHurtTriggered()
    {
        if (_brain == null || _brain.CurrentState == EnemyState.Death || _brain.CurrentState == EnemyState.Agonizing)
            return;

        if (_stunCoroutine != null) StopCoroutine(_stunCoroutine);
        _stunCoroutine = StartCoroutine(StunAndKnockbackRoutine());
    }
    private IEnumerator StunAndKnockbackRoutine()
    {
        _navAgent.enabled = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.angularVelocity = Vector3.zero;

        Vector3 knockbackDir = Vector3.zero;
        if (_combat != null && _combat.TargetTransform != null)
        {
            knockbackDir = (transform.position - _combat.TargetLocation).normalized;
            knockbackDir.y = 0f;
        }
        else
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            if (player != null)
            {
                knockbackDir = (transform.position - player.transform.position).normalized;
                knockbackDir.y = 0f;
            }
        }

        _rb.linearVelocity = knockbackDir * _knockbackForce;

        float timer = 0f;
        while (timer < _stunDuration)
        {
            if (_brain == null || _brain.CurrentState == EnemyState.Death || _brain.CurrentState == EnemyState.Agonizing)
            {
                _stunCoroutine = null;
                yield break;
            }

            Vector3 currentVel = _rb.linearVelocity;
            Vector3 horizontalVel = new(currentVel.x, 0f, currentVel.z);
            horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, Time.deltaTime * _knockbackFriction);
            _rb.linearVelocity = new(horizontalVel.x, currentVel.y, horizontalVel.z);
            _rb.angularVelocity = Vector3.zero;
            FlattenRotation();

            timer += Time.deltaTime;
            yield return null;
        }

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _rb.useGravity = false;
        FlattenRotation();

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            transform.position = hit.position;

        _navAgent.enabled = true;
        StopAgentCompletely();
        _stunCoroutine = null;

        if (_brain != null && _brain.CurrentState == EnemyState.Hurt && !_brain.IsHurtAnimationActive)
        {
            _brain.EndHurt();
        }
    }
}