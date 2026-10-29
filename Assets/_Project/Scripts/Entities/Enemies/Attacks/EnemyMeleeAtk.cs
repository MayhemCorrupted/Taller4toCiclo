using UnityEngine;

[CreateAssetMenu(fileName = "EnemyMeleeAtk", menuName = "Scriptable Objects/EnemyMeleeAtk")]
public class EnemyMeleeAtk : EnemyAttack
{
	[Header("MELEE PROPERTIES")]
	public bool isRadial;
	[Tooltip("If the melee attack isn't radial, this field will be taken as the distance between the attacker and the melee box detector")]
	public float radius;
	public Vector3 hitboxMeasurements = Vector3.one;
	public float verticalOffset = 0.0f;

	public override void Execute(Transform origin, Transform target)
	{
		if (target.gameObject.CompareTag("Player"))
		{
			PlayerHealth playerHealth = target.gameObject.GetComponent<PlayerHealth>();
			playerHealth.TakeDamage(damage);
		}
	}
}
