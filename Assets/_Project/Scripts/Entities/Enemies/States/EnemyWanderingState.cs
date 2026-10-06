using UnityEngine;

public class EnemyWanderingState : EnemyState
{
    public override EnemyState Run(EnemyStateController controller)
    {
        return this;
    }
}
