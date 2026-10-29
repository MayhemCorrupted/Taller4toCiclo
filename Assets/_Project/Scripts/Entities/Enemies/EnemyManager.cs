using UnityEngine;

public class EnemyManager : MonoBehaviour
{
	[Header("Entry point")]
	[SerializeField] private EnemyData data;
	[Header("Sub-components")]
	[SerializeField] private EnemyHealth health;
	[SerializeField] private EnemyDetection detector;
	[SerializeField] private EnemyMovement movement;
	[SerializeField] private EnemyStateManager stateManager;
	[SerializeField] private EnemyAttackController attacker;

	private void Awake()
	{
		Init();
	}

	private void Init()
	{
		health.Init(data.health, data.defense);
		detector.Init(data.detectionRadius, data.attackRadius, data.lossRadius);
		movement.Init(data.speed);
		attacker.Init(data.attacks);
	}
}
