using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
	[Header("Stats")]
	[Min(1)] public int health;
	[Min(0)] public int defense;
	[Min(0.0f)] public float speed;
	[Header("Ranged properties")]
	[Min(0.1f)] public float detectionRadius;
	[Min(0.1f)] public float attackRadius;
	[Min(0.1f)] public float lossRadius;
	public List<EnemyAttack> attacks = new();
}
