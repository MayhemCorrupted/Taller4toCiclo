using UnityEngine;

public abstract class EnemyState : MonoBehaviour
{
    public abstract EnemyState Run(EnemyStateController controller);
}
