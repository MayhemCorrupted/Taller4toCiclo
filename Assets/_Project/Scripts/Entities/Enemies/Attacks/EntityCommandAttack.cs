using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityCommandAttack", menuName = "Rotbound/Entity/Attack/EntityCommandAttack")]
public class EntityCommandAttack : EntityAttack
{
    [Header("COMMAND PROPERTIES")]
    [Tooltip("El rango de aliados a los que el Boss ordenará atacar.")]
    public EnemyRank targetRankToCommand = EnemyRank.Minion;

    [Tooltip("Duración de la orden. Durante este tiempo, los elegidos irán en masa al jugador ignorando el bloqueo del Director.")]
    [Min(1f)] public float commandDuration = 5f;
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (EntityDirector.Instance != null)
        {
            EntityDirector.Instance.CommandAlliesToAttack(targetRankToCommand, commandDuration);
        }
        float timer = 0f;
        while (timer < attackDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }
}