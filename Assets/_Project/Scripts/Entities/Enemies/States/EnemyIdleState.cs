using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public override EnemyState Run()
    {
        return this;
    }
}
