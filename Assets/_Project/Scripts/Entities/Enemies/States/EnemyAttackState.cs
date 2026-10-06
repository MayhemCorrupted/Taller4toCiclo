using UnityEngine;

public class EnemyAttackState : EnemyState
{
	[SerializeField] private EnemyChaseState chaseState;

	public override EnemyState Run(EnemyStateController controller)
	{
		if (controller.detector.IsInAttackRange)
			return this;
		else
			return chaseState;
	}
}
