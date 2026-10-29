using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
	private float damage;
	private Rigidbody rb;

	public void Init(Vector3 velocity, float dmg, float lifetime)
	{
		rb = GetComponent<Rigidbody>();
		rb.linearVelocity = velocity;
		damage = dmg;
		StartCoroutine(PendingDestruction(lifetime));
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (collider.CompareTag("Enemy"))
			return;

		if (collider.CompareTag("Player"))
		{
			PlayerHealth health = collider.gameObject.GetComponent<PlayerHealth>();
			health.TakeDamage(damage);
		}

		Destroy(gameObject);
	}

	IEnumerator PendingDestruction(float lifetime)
	{
		yield return new WaitForSeconds(lifetime);
		Destroy(gameObject);
	}
}
