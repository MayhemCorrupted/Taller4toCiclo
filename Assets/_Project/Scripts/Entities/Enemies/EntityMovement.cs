using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
public class EntityMovement : MonoBehaviour
{
    private NavMeshAgent _NavAgent;
    private Health _health;
    private Rigidbody _rb;

    [SerializeField] private EntityBrain _brain;
    [SerializeField] private EntityCombat _combat;

    [Header("Patrol Settings")]
    [Min(0.0f)] public float _patrolWaitingTime = 1.0f;
    [Min(0.1f)] public float _patrolRadius = 5.0f;

    [Header("Hit Reaction & Game Feel")]
    [SerializeField] private bool _canBeStunned = true;
    [SerializeField] private float _stunDuration = 0.5f;
    [SerializeField] private float _knockbackForce = 15f;
    [SerializeField] private float _knockbackFriction = 8f;

    private float _speed = 0.0f;
    private float _patrolWaitingTimer;
    private bool _isWaiting;
    private Coroutine _stunCoroutine;
    private void Awake()
    {
        _NavAgent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }
    private void Start()
    {
        if (_brain != null)
        {
            _brain.OnStateChanged += HandleStateChanged;
            if (_brain.CurrentState == EnemyState.Patroling) SetRandomPatrolPoint();
        }
        _health.OnTakeDamage += HandleDamageTaken;
    }
    private void OnDestroy()
    {
        if (_brain != null) _brain.OnStateChanged -= HandleStateChanged;
        if (_health != null) _health.OnTakeDamage -= HandleDamageTaken;
    }
    private void Update()
    {
        if (!_NavAgent.enabled || _brain == null) return;
        _NavAgent.speed = _speed;

        switch (_brain.CurrentState)
        {
            case EnemyState.Patroling: HandlePatroling(); break;
            case EnemyState.Chasing: HandleChasing(); break;
            case EnemyState.Attacking: HandleAttacking(); break;
            case EnemyState.Hurt: HandleHurt(); break;
        }
    }
    public void SetSpeed(float speed) => this._speed = speed;
    private void SetRandomPatrolPoint()
    {
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * _patrolRadius;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
        {
            _NavAgent.isStopped = false;
            _NavAgent.SetDestination(hit.position);
        }
    }
    private void HandleStateChanged(EnemyState newState)
    {
        if (!_NavAgent.enabled) return;
        if (newState == EnemyState.Patroling)
        {
            _isWaiting = false;
            SetRandomPatrolPoint();
        }
        else if (newState == EnemyState.Chasing) _NavAgent.isStopped = false;
    }
    private void HandlePatroling()
    {
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
        if (!_NavAgent.pathPending && _NavAgent.remainingDistance <= _NavAgent.stoppingDistance)
        {
            if (!_NavAgent.hasPath || _NavAgent.velocity.sqrMagnitude == 0.0f)
            {
                _isWaiting = true;
                _patrolWaitingTimer = _patrolWaitingTime;
            }
        }
    }
    private void HandleChasing()
    {
        _NavAgent.isStopped = false;

        if (_combat != null && _combat.TargetTransform != null)
        {
            Vector3 destination = _combat.TargetLocation;
            Vector3 ambushCenter = _combat.TargetLocation; 

            if (EntityDirector.Instance != null)
            {
                destination = EntityDirector.Instance.GetTacticalPosition(_combat, _combat.TargetTransform, _combat.TargetVelocity);

                Vector3 flatVel = new(_combat.TargetVelocity.x, 0, _combat.TargetVelocity.z);
                if (flatVel.sqrMagnitude > 0.5f) ambushCenter += flatVel * EntityDirector.Instance.ambushPredictionTime;
            }

            if (_combat.PreferredAttackDistance > 0 && _combat.CurrentPhase != null)
            {
                float optimalRadius = 0f;
                Vector3 dirFromCenter = (destination - ambushCenter).normalized;
                if (dirFromCenter == Vector3.zero) dirFromCenter = (transform.position - ambushCenter).normalized;

                if (_combat.PreferredAttackDistance == 1) optimalRadius = _combat.CurrentPhase.attackPool.closeRangeMax * 0.8f;
                else if (_combat.PreferredAttackDistance == 2) optimalRadius = _combat.CurrentPhase.attackPool.mediumRangeMax * 0.8f;
                else if (_combat.PreferredAttackDistance == 3) optimalRadius = _combat.CurrentPhase.attackPool.mediumRangeMax * 1.5f;

                destination = ambushCenter + (dirFromCenter * optimalRadius);
            }

            if (EntityDirector.Instance != null)
            {
                destination += EntityDirector.Instance.GetSeparationVector(_combat);
            }

            _NavAgent.SetDestination(destination);

            Vector3 dirToTarget = (_combat.TargetLocation - transform.position).normalized;
            dirToTarget.y = 0;
            if (dirToTarget.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToTarget), Time.deltaTime * 8f);
            }
        }
    }
    private void HandleAttacking()
    {
        _NavAgent.isStopped = true;
        _NavAgent.velocity = Vector3.zero;
    }
    private void HandleHurt()
    {
        if (_NavAgent.enabled)
        {
            _NavAgent.isStopped = true;
            _NavAgent.velocity = Vector3.zero;
        }
    }
    private void HandleDamageTaken(float damage)
    {
        if (!_canBeStunned || _brain == null) return;
        if (_combat != null && _combat.IsArmorActive) return;

        if (_stunCoroutine != null) StopCoroutine(_stunCoroutine);
        _stunCoroutine = StartCoroutine(StunAndKnockbackRoutine());
    }
    private IEnumerator StunAndKnockbackRoutine()
    {
        _brain.ChangeState(EnemyState.Hurt);
        _NavAgent.enabled = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        Vector3 knockbackDir = Vector3.zero;
        if (player != null)
        {
            knockbackDir = (transform.position - player.transform.position).normalized;
            knockbackDir.y = 0;
        }
        _rb.linearVelocity = knockbackDir * _knockbackForce;

        float timer = 0f;
        while (timer < _stunDuration)
        {
            Vector3 currentVel = _rb.linearVelocity;
            Vector3 horizontalVel = new(currentVel.x, 0, currentVel.z);
            horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, Time.deltaTime * _knockbackFriction);
            _rb.linearVelocity = new(horizontalVel.x, currentVel.y, horizontalVel.z);
            timer += Time.deltaTime;
            yield return null;
        }

        _rb.isKinematic = true;
        _rb.useGravity = false;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas)) transform.position = hit.position;
        _NavAgent.enabled = true;
        _brain.ChangeState(EnemyState.Chasing);
    }
}