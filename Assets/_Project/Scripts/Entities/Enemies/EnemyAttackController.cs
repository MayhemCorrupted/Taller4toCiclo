using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnemyAttackController : MonoBehaviour
{
	readonly private List<EnemyMeleeAtk> meleeAtks = new();
	readonly private List<EnemyRangedAtk> rangedAtks = new();

	public void Init(List<EnemyAttack> attacks)
	{
		foreach (EnemyAttack attack in attacks)
		{
			if (attack is EnemyMeleeAtk meleeAtk)
				meleeAtks.Add(meleeAtk);
			else if (attack is EnemyRangedAtk rangedAtk)
				rangedAtks.Add(rangedAtk);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		foreach (EnemyMeleeAtk atk in meleeAtks)
		{
			if (atk.isRadial)
#if UNITY_EDITOR
				Handles.DrawWireDisc(transform.position, Vector3.up, atk.radius);
#endif
			else
				Gizmos.DrawWireCube(Vector3.forward * atk.radius + Vector3.up * atk.verticalOffset, atk.hitboxMeasurements);
		}

		Gizmos.matrix = Matrix4x4.identity;
	}
}
