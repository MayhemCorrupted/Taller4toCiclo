using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
	private float damage;
	private float knockback;
	private Rigidbody rb;

	public void Init(Vector3 velocity, float dmg, float lifetime, float knockbackForce = 0f)
	{
		rb = GetComponent<Rigidbody>();
		rb.linearVelocity = velocity;
		damage = dmg;
		knockback = knockbackForce;
		StartCoroutine(PendingDestruction(lifetime));
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (collider.CompareTag("Enemy"))
			return;

		if (collider.CompareTag("Player"))
		{
			PlayerHealth health = collider.GetComponentInParent<PlayerHealth>();
			if (health != null && health.TakeDamage(damage) && knockback > 0f)
			{
				IKnockbackReceiver receiver = collider.GetComponentInParent<IKnockbackReceiver>();
				receiver?.ApplyKnockback(collider.transform.position - transform.position, knockback);
			}
		}

		Destroy(gameObject);
	}

	IEnumerator PendingDestruction(float lifetime)
	{
		yield return new WaitForSeconds(lifetime);
		Destroy(gameObject);
	}
}
