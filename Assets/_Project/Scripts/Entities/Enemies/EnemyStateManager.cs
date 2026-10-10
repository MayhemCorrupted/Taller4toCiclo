using System;
using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
	[SerializeField] private EnemyState state = EnemyState.Idle;

	[SerializeField] private EnemyDetection detector;
	public event Action<EnemyState> OnStateChanged;

	private void Update()
	{
		if (detector.IsInAttackRange)
			ChangeState(EnemyState.Attacking);
		else if (detector.CanSeeTarget)
			ChangeState(EnemyState.Chasing);
		else
			ChangeState(EnemyState.Idle);
	}

	private void ChangeState(EnemyState newState)
	{
		if (state == newState)
			return;

		state = newState;
		OnStateChanged?.Invoke(state);
	}
}
