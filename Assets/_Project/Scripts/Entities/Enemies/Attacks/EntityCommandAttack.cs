using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "NewCommandAttack", menuName = "Rotbound/Entity/Attacks/Command Attack")]
public class EntityCommandAttack : EntityAttack
{
    [Header("Configuración de Orden Jerárquica")]
    [Tooltip("Rango máximo de subordinados al que va dirigida la orden de asalto.")]
    public EnemyRank targetRankToCommand = EnemyRank.Leader;

    [Tooltip("Duración (en segundos) durante la cual el Boss libera la formación pasiva y ordena atacar.")]
    [Min(0.5f)] public float commandDuration = 6.0f;

    [Header("Orientación Visual")]
    [Min(1f)] public float turnSpeedDuringCommand = 8.0f;
    [Min(0.5f)] public float indicatorRadius = 3.0f;
    public override void Prepare(Transform origin, Transform target)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        ClearIndicatorsFor(root);
        Vector3 groundOrigin = GetTelegraphOrigin(root);
        SpawnIndicatorFor(root, groundOrigin, Quaternion.identity, new Vector3(indicatorRadius * 2f, 0.1f, indicatorRadius * 2f), telegraphDuration);
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        Transform root = GetRootTransform(origin);
        if (root == null || target == null) return;

        Vector3 dirToTarget = target.position - root.position;
        dirToTarget.y = 0f;

        if (dirToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);
            root.rotation = Quaternion.Slerp(root.rotation, targetRot, Time.deltaTime * turnSpeedDuringCommand);
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        yield return WaitForImpactTiming(origin);

        Transform root = GetRootTransform(origin);
        ClearIndicatorsFor(root);

        if (EntityDirector.Instance != null)
        {
            EntityDirector.Instance.CommandAlliesToAttack(targetRankToCommand, commandDuration);
        }

        WaitForSeconds waitForSeconds = new(0.25f);
        yield return waitForSeconds;
    }
}