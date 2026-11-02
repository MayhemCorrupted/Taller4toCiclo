using System;
using UnityEngine;
public enum EnemyState
{
    Patroling,
    Chasing,
    Attacking,
	Hurt
}

public class EnemyStateManager : MonoBehaviour
{
	[SerializeField] private EnemyDetection detector;
	public event Action<EnemyState> OnStateChanged;
	private EnemyState state = EnemyState.Patroling;
	public EnemyState CurrentState => state;
	private void Update()
	{
        if (state == EnemyState.Hurt)
            return;

        if (detector.IsInAttackRange)
            ChangeState(EnemyState.Attacking);
        else if (detector.CanSeeTarget)
            ChangeState(EnemyState.Chasing);
        else
            ChangeState(EnemyState.Patroling);
    }
    public void ChangeState(EnemyState newState)
    {
        if (state == newState)
            return;

        state = newState;
        OnStateChanged?.Invoke(state);
    }
}
