using UnityEngine;

public class EnemyHealth : Health
{
	public void Init(int hp, int def)
	{
		_maxHealth = hp;
		_defense = def;
	}
	public override void TakeDamage(float amount)
	{
		if (_currentHealth <= 0)
			return;

		_currentHealth -= CalculateIncomingDamage(amount);
		Debug.Log($"Vida enemigo: {_currentHealth}");

		if (_currentHealth <= 0)
		{
			Debug.Log("Enemigo muere!");
			_currentHealth = 0;
			Die();
		}
	}
	private float CalculateIncomingDamage(float atk)
	{
		return atk * (1.0f - (_defense / (_defense + 100.0f)));
	}

	protected override void Die()
	{
		base.Die();
		Destroy(gameObject);
	}
}