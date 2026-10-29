using UnityEngine;

[CreateAssetMenu(fileName = "EnemyRangedAtk", menuName = "Scriptable Objects/EnemyRangedAtk")]
public class EnemyRangedAtk : EnemyAttack
{
	[Header("RANGED PROPERTIES")]
	public GameObject projectileInstance;
	[Min(0.0f)] public float projectileSpeed;
	[Min(0.0f)] public float lifetime = 10.0f;
	[Min(0.0f)] public float precisionFactor = 1.0f;
	[Min(0.0f)] public float maxDeviation = 1.0f;
	public float verticalOffset = 0.0f;

	public override void Execute(Transform origin, Transform target)
	{
		if (target.gameObject.CompareTag("Player"))
		{
			PlayerMovement plMov = target.gameObject.GetComponent<PlayerMovement>();

			Vector3 targetPosition = TweakPrecision(PredictPosition(target.position, origin.position, plMov.CurrentVelocity));

			Vector3 shootDirection = (targetPosition - origin.position).normalized;

			GameObject projectile = Instantiate(projectileInstance, origin.position + Vector3.up * verticalOffset, Quaternion.LookRotation(shootDirection));
			Projectile projectileComp = projectile.GetComponent<Projectile>();
			projectileComp.Init(shootDirection * projectileSpeed, damage, lifetime);
		}
	}

	private Vector3 PredictPosition(Vector3 targetPosition, Vector3 firingPosition, Vector3 targetVelocity)
	{
		float distance = (targetPosition - firingPosition).magnitude;

		float estimatedTime = distance / projectileSpeed;

		return targetPosition + (targetVelocity * estimatedTime);
	}

	private Vector3 TweakPrecision(Vector3 initialPrediction)
	{
		if (precisionFactor >= 1.0f)
			return initialPrediction;

		float errorRadius = (1.0f - precisionFactor) * maxDeviation;
		Vector3 deviation = Random.insideUnitSphere * errorRadius;

		return initialPrediction + deviation;
	}
}
