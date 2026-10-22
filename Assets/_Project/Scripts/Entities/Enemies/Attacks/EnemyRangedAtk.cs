using UnityEngine;

[CreateAssetMenu(fileName = "EnemyRangedAtk", menuName = "Scriptable Objects/EnemyRangedAtk")]
public class EnemyRangedAtk : EnemyAttack
{
	[Header("RANGED PROPERTIES")]
	public GameObject projectileInstance;
	[Min(0.0f)] public float angle;

	public override void Execute(Transform origin, Transform target)
	{
		// ...
	}

	private Vector3 CalculateLaunchForce(Vector3 muzzle, Vector3 target, float angle)
	{
		/*
		 * PHI = atan2(y, x)
		 */
	}
}
