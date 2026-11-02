using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private EnemyStateManager stateManager;
    [SerializeField] private EnemyDetection detector;
    [SerializeField] private Health health; 

    [Header("Patrol Settings")]
    [Min(0.0f)] public float patrolWaitingTime = 1.0f;
    [Min(0.1f)] public float patrolRadius = 5.0f;

    [Header("Hit Reaction & Game Feel")]
    [SerializeField] private bool canBeStunned = true; 
    [SerializeField] private float stunDuration = 0.5f;
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private float knockbackFriction = 8f;

    private float speed = 0.0f;
    private float patrolWaitingTimer;
    private bool isWaiting;
    private Coroutine stunCoroutine;

    private void Start()
    {
        stateManager.OnStateChanged += HandleStateChanged;
        if (health != null) health.OnTakeDamage += HandleDamageTaken;

        if (stateManager.CurrentState == EnemyState.Patroling)
            SetRandomPatrolPoint();
    }

    private void OnDestroy()
    {
        stateManager.OnStateChanged -= HandleStateChanged;
        if (health != null) health.OnTakeDamage -= HandleDamageTaken;
    }

    void Update()
    {
        agent.speed = speed;

        switch (stateManager.CurrentState)
        {
            case EnemyState.Patroling:
                HandlePatroling();
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Attacking:
                HandleAttacking();
                break;
            case EnemyState.Hurt:
                HandleHurt();
                break;
        }
    }

    public void Init(float speed)
    {
        this.speed = speed;
    }

    private void SetRandomPatrolPoint()
    {
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * patrolRadius;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    private void HandleStateChanged(EnemyState newState)
    {
        if (newState == EnemyState.Patroling)
        {
            isWaiting = false;
            SetRandomPatrolPoint();
        }
        else if (newState == EnemyState.Chasing)
        {
            agent.isStopped = false;
        }
    }

    private void HandlePatroling()
    {
        if (isWaiting)
        {
            patrolWaitingTimer -= Time.deltaTime;
            if (patrolWaitingTimer <= 0.0f)
            {
                isWaiting = false;
                SetRandomPatrolPoint();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0.0f)
            {
                isWaiting = true;
                patrolWaitingTimer = patrolWaitingTime;
            }
        }
    }

    private void HandleDamageTaken(float damage)
    {
        if (!canBeStunned) return;

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunAndKnockbackRoutine());
    }

    private void HandleChasing()
    {
        agent.isStopped = false;

        if (detector.TargetLocation != Vector3.positiveInfinity)
            agent.SetDestination(detector.TargetLocation);
    }

    private void HandleAttacking()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private void HandleHurt()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private IEnumerator StunAndKnockbackRoutine()
    {
        stateManager.ChangeState(EnemyState.Hurt);
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        Vector3 knockbackDir = Vector3.zero;

        if (player != null)
        {
            knockbackDir = (transform.position - player.transform.position).normalized;
            knockbackDir.y = 0;
        }

        Vector3 currentKnockbackVelocity = knockbackDir * knockbackForce;

        float timer = 0f;
        while (timer < stunDuration)
        {
            agent.Move(currentKnockbackVelocity * Time.deltaTime);

            currentKnockbackVelocity = Vector3.Lerp(currentKnockbackVelocity, Vector3.zero, Time.deltaTime * knockbackFriction);

            timer += Time.deltaTime;
            yield return null;
        }

        stateManager.ChangeState(EnemyState.Chasing);
    }
}