using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
	public float speed;
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] private EnemyStateManager stateManager;
	[SerializeField] private EnemyDetection detector;
	[Min(0.0f)] public float patrolWaitingTime = 1.0f;
	[Min(0.1f)] public float patrolRadius = 5.0f;

	private float patrolWaitingTimer;
	private bool isWaiting;

	private void Start()
	{
		detector.Init();
		stateManager.OnStateChanged += HandleStateChanged;

		if (stateManager.CurrentState == EnemyState.Patroling)
			SetRandomPatrolPoint();
	}

	private void OnDestroy()
	{
		stateManager.OnStateChanged -= HandleStateChanged;
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
		}
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
			agent.isStopped = false;
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
}
