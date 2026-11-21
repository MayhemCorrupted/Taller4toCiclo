using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
	[SerializeField] private List<GameObject> enemyInstances = new();
	[SerializeField][Min(0.0f)] private float spawnInterval = 5.0f;
	[SerializeField][Min(0.1f)] private float spawnRadius = 1.0f;

	private WaitForSeconds spawnWFSInterval;

	private void Awake()
	{
		spawnWFSInterval = new WaitForSeconds(spawnInterval);

		StartCoroutine(SpawnEnemies());
	}

	private IEnumerator SpawnEnemies()
	{
		while (true)
		{
			Instantiate(enemyInstances[Random.Range(0, enemyInstances.Count)], Random.insideUnitCircle * spawnRadius, Quaternion.identity);
			yield return spawnWFSInterval;
		}
	}
}
