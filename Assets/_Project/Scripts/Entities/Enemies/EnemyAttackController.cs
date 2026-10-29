using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[RequireComponent(typeof(EnemyStateManager))]

[RequireComponent(typeof(EnemyDetection))]
public class EnemyAttackController : MonoBehaviour
{
	private List<EnemyAttack> attacks = new();
	private float atkCountdown = 0.0f;
	private EnemyStateManager stateManager;
	private EnemyDetection detector;

	private int currentAtkIdx = -1;

	private void Awake()
	{
		stateManager = GetComponent<EnemyStateManager>();
		detector = GetComponent<EnemyDetection>();
	}

	public void Init(List<EnemyAttack> attacks)
	{
		this.attacks = attacks;
	}

	private void Update()
	{
		if (attacks.Count == 0)
			return;

		if (stateManager.CurrentState == EnemyState.Attacking)
		{
			if (atkCountdown <= 0.0f)
			{
				currentAtkIdx = SelectAndAttack();
				if (currentAtkIdx != -1)
					atkCountdown = attacks[currentAtkIdx].cooldown;
			}
			else
				atkCountdown = Mathf.Max(atkCountdown - Time.deltaTime, 0.0f);
		}
		else
		{
			currentAtkIdx = -1;
			atkCountdown = 0.0f;
		}
	}
#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		if (currentAtkIdx != -1)
		{
			if (attacks[currentAtkIdx] is EnemyMeleeAtk meleeAtk)
			{
				if (meleeAtk.isRadial)
					Handles.DrawWireDisc(transform.position, Vector3.up, meleeAtk.radius);
				else
					Gizmos.DrawWireCube(Vector3.forward * meleeAtk.radius + Vector3.up * meleeAtk.verticalOffset, meleeAtk.hitboxMeasurements);
			}
			else if (attacks[currentAtkIdx] is EnemyRangedAtk rangedAtk)
			{
				// ...
			}
		}

		Gizmos.matrix = Matrix4x4.identity;
	}
#endif
	private int SelectAndAttack()
	{
		if (!detector.TargetTransform)
			return -1;

		int rndIdx = Random.Range(0, attacks.Count);
		attacks[rndIdx].Execute(transform, detector.TargetTransform);
		return rndIdx;
	}
}
