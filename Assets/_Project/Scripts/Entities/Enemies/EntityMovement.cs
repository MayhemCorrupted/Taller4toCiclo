using System.Collections;
using UnityEngine;
using UnityEngine.AI;
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
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Patrol Settings")]
    [Min(0.0f)] public float _patrolWaitingTime = 1.0f;
    [Min(0.1f)] public float _patrolRadius = 5.0f;

    [Header("Orbital & Preferred Distance Settings")]
    [Tooltip("Activa el desplazamiento orbital lateral alrededor del jugador cuando lo detecta.")]
    [SerializeField] private bool _enableOrbitalMovement = true;
    [Tooltip("Desplazamiento tangencial lateral para rodear al jugador mientras mantiene su rango preferido.")]
    [SerializeField] private float _orbitStrafeDistance = 2.2f;
    [Tooltip("Tiempo promedio en segundos antes de cambiar el sentido de órbita (izquierda/derecha).")]
    [SerializeField] private float _orbitDirectionChangeInterval = 3.5f;

    [Header("Hit Reaction & Death Settings")]
    [Tooltip("Duración del retroceso físico (knockback) y duración de respaldo del stun si no hay animación Hurt.")]
    [SerializeField] private float _stunDuration = 0.45f;
    [SerializeField] private float _knockbackForce = 12f;
    [SerializeField] private float _knockbackFriction = 10f;
    [Tooltip("Desactiva los colliders del enemigo al morir para no bloquear al jugador.")]
    [SerializeField] private bool _disableCollidersOnDeath = true;

    private EntityData _entityData;
    private float _speed = 0.0f;
    private float _patrolWaitingTimer;
    private bool _isWaiting;
    private Coroutine _stunCoroutine;

    private int _orbitDirection = 1;
    private float _orbitSwitchTimer;
    public bool IsKnockbackActive => _stunCoroutine != null;
    private void Awake()
    {
        _NavAgent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _rb = GetComponent<Rigidbody>();

        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.freezeRotation = true;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

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
    public void SetSpeed(float speed) => _speed = speed;
    private void Update()
    {
        FlattenRotation();

        if (!_NavAgent.enabled || _brain == null) return;
        _NavAgent.speed = _speed;

        switch (_brain.CurrentState)
        {
            case EnemyState.Patroling: HandlePatroling(); break;
            case EnemyState.Chasing: HandleChasing(); break;
            case EnemyState.Attacking: HandleAttacking(); break;
            case EnemyState.Hurt: HandleHurt(); break;
            case EnemyState.Agonizing: HandleAgonizing(); break;
            case EnemyState.Death: HandleDeath(); break;
        }
    }
    private void FlattenRotation()
    {
        Vector3 euler = transform.eulerAngles;
        if (!Mathf.Approximately(euler.x, 0f) || !Mathf.Approximately(euler.z, 0f))
        {
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }
    }
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

            if (_NavAgent.enabled)
            {
                _NavAgent.isStopped = true;
                _NavAgent.velocity = Vector3.zero;
                _NavAgent.enabled = false;
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

            if (!_NavAgent.enabled)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    transform.position = hit.position;
                _NavAgent.enabled = true;
            }

            _NavAgent.isStopped = true;
            _NavAgent.velocity = Vector3.zero;
            return;
        }

        if (!_NavAgent.enabled) return;

        if (newState == EnemyState.Hurt)
        {
            _NavAgent.isStopped = true;
            _NavAgent.velocity = Vector3.zero;
            return;
        }

        if (newState == EnemyState.Patroling)
        {
            _isWaiting = false;
            SetRandomPatrolPoint();
        }
        else if (newState == EnemyState.Chasing)
        {
            _NavAgent.isStopped = false;
            _orbitDirection = Random.value > 0.5f ? 1 : -1;
            _orbitSwitchTimer = Random.Range(_orbitDirectionChangeInterval * 0.6f, _orbitDirectionChangeInterval * 1.4f);
        }
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
        MaintainPreferredCombatDistance(allowOrbitalStrafe: true);
    }
    private void HandleAttacking()
    {
        if (_combat != null && _combat.IsPerformingAttack)
        {
            _NavAgent.isStopped = true;
            _NavAgent.velocity = Vector3.zero;
            return;
        }

        MaintainPreferredCombatDistance(allowOrbitalStrafe: true);
    }
    private void MaintainPreferredCombatDistance(bool allowOrbitalStrafe)
    {
        if (_combat == null || _combat.TargetTransform == null) return;

        _NavAgent.isStopped = false;

        Vector3 targetPos = _combat.TargetLocation;
        Vector3 ambushCenter = targetPos;

        if (EntityDirector.Instance != null)
        {
            Vector3 flatVel = new(_combat.TargetVelocity.x, 0f, _combat.TargetVelocity.z);
            if (flatVel.sqrMagnitude > 0.5f)
                ambushCenter += flatVel * EntityDirector.Instance.ambushPredictionTime;
        }

        float minRange = 1.2f;
        float optimalRange = 2.5f;

        if (_entityData != null)
            _entityData.GetPreferredRangeBand(_combat.CurrentPhase, out minRange, out optimalRange, out _);

        Vector3 radialFromTarget = transform.position - targetPos;
        radialFromTarget.y = 0f;
        float currentDist = radialFromTarget.magnitude;
        Vector3 radialDir = currentDist > 0.01f ? radialFromTarget / currentDist : -transform.forward;

        Vector3 anchorCenter = (currentDist < minRange && _combat.PreferredAttackDistance > 0) ? targetPos : ambushCenter;
        float desiredRadius = optimalRange;

        Vector3 destination = anchorCenter + (radialDir * desiredRadius);

        if (_enableOrbitalMovement && allowOrbitalStrafe)
        {
            _orbitSwitchTimer -= Time.deltaTime;
            if (_orbitSwitchTimer <= 0f)
            {
                _orbitDirection *= -1;
                _orbitSwitchTimer = Random.Range(_orbitDirectionChangeInterval * 0.6f, _orbitDirectionChangeInterval * 1.4f);
            }

            Vector3 tangentDir = Vector3.Cross(Vector3.up, radialDir) * _orbitDirection;
            Vector3 orbitedOffset = (radialDir * desiredRadius) + (tangentDir * _orbitStrafeDistance);
            orbitedOffset = orbitedOffset.normalized * desiredRadius;
            destination = anchorCenter + orbitedOffset;
        }

        if (EntityDirector.Instance != null)
        {
            destination += EntityDirector.Instance.GetSeparationVector(_combat);
        }

        if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 3.5f, NavMesh.AllAreas))
        {
            _NavAgent.SetDestination(navHit.position);
        }
        else if (NavMesh.SamplePosition(targetPos + radialDir * minRange, out NavMeshHit fallbackHit, 3.5f, NavMesh.AllAreas))
        {
            _NavAgent.SetDestination(fallbackHit.position);
        }

        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        dirToTarget.y = 0f;
        if (dirToTarget.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToTarget), Time.deltaTime * 10f);
        }
    }
    private void HandleHurt()
    {
        if (_NavAgent.enabled)
        {
            _NavAgent.isStopped = true;
            _NavAgent.velocity = Vector3.zero;
        }
    }
    private void HandleAgonizing()
    {
        if (_NavAgent.enabled)
        {
            _NavAgent.isStopped = true;
            _NavAgent.velocity = Vector3.zero;
        }
    }
    private void HandleDeath()
    {
        if (_NavAgent.enabled)
        {
            _NavAgent.isStopped = true;
            _NavAgent.velocity = Vector3.zero;
        }
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
        _NavAgent.enabled = false;
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

        _NavAgent.enabled = true;
        _NavAgent.isStopped = true;
        _NavAgent.velocity = Vector3.zero;
        _stunCoroutine = null;

        if (_brain != null && _brain.CurrentState == EnemyState.Hurt && !_brain.IsHurtAnimationActive)
        {
            _brain.EndHurt();
        }
    }
}