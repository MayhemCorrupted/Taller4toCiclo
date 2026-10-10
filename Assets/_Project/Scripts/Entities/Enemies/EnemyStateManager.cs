using System;
using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
	[SerializeField] private EnemyDetection detector;
	public event Action<EnemyState> OnStateChanged;

	private EnemyState state = EnemyState.Patroling;

	public EnemyState CurrentState
	{
		get => state;
	}

	private void Update()
	{
		if (detector.IsInAttackRange)
			ChangeState(EnemyState.Attacking);
		else if (detector.CanSeeTarget)
			ChangeState(EnemyState.Chasing);
		else
			ChangeState(EnemyState.Patroling);
	}

	private void ChangeState(EnemyState newState)
	{
		if (state == newState)
			return;

		state = newState;
		OnStateChanged?.Invoke(state);
	}
}
