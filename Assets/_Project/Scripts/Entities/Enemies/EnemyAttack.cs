using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private EnemyAttackType attackType;
    private float damage;

    public void Init(EnemyAttackType type, float dmg)
    {
        attackType = type;
        damage = dmg;
    }
}
