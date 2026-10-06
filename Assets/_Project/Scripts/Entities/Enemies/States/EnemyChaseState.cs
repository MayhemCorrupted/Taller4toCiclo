using Unity.Cinemachine;
using UnityEngine;

public class EnemyChaseState : EnemyState
{
	[SerializeField] private EnemyAttackState attackState;
	[SerializeField] private EnemyIdleState idleState;

	public override EnemyState Run(EnemyStateController controller)
	{
		if (!controller.detector.CanSeeTarget)
			return idleState;
		else
		{
			if (controller.detector.IsInAttackRange)
				return attackState;
			else
				return this;
		}
	}
}
