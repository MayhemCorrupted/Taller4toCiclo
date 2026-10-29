using UnityEngine;

public class EnemyHealth : HealthSystem
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

		if (_currentHealth <= 0)
		{
			_currentHealth = 0;
			Die();
		}
	}

	private float CalculateIncomingDamage(float atk)
	{
		return atk * (1.0f - (_defense / (_defense + 100.0f)));
	}
}
