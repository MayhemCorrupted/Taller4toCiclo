using System;
using UnityEngine;
public enum EnemyState { Patroling, Chasing, Attacking, Hurt}
[RequireComponent(typeof(Health))]
public class EnemyBrain : MonoBehaviour
{
	[SerializeField] private EnemyData _data;
	[Header("Sub-components")]
	[SerializeField] private Health _health;
	[SerializeField] private EnemyCombat _combat;
	[SerializeField] private EnemyMovement _movement;

    public event Action<EnemyState> OnStateChanged;
    private EnemyState _currentState = EnemyState.Patroling;
    public EnemyState CurrentState => _currentState;
    private void Awake()
	{
		_health.SetHealthValues(_data.health, _data.defense);
		if (_combat != null) _combat.SetValues(_data.detectionRadius, _data.attackRadius, _data.lossRadius, _data.attacks);
		if (_movement != null) _movement.SetSpeed(_data.speed);
	}
    private void Update()
    {
        if (_currentState == EnemyState.Hurt) return;
		if (_combat == null)
		{
			ChangeState(EnemyState.Patroling);
			return;
		}
        if (_combat.IsInAttackRange)
            ChangeState(EnemyState.Attacking);
        else if (_combat.CanSeeTarget)
            ChangeState(EnemyState.Chasing);
        else
            ChangeState(EnemyState.Patroling);
    }
	public void ChangeState(EnemyState state)
	{
		if (_currentState == state) return;	
		_currentState = state;
		OnStateChanged?.Invoke(state);
	}
}
