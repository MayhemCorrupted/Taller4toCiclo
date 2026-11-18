using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityRangeAttack", menuName = "Rotbound/Entity/Attack/EntityRangeAttack")]
public class EntityRangeAttack : EntityAttack
{
    [Header("Configuración de Proyectil Frontal")]
    public GameObject projectileInstance;
    [Min(0.1f)] public float projectileSpeed = 15f;
    [Min(0.1f)] public float lifetime = 5.0f;
    public float verticalOffset = 1.0f;
    [Min(0f)] public float forwardSpawnOffset = 0.6f;

    [Header("Orientación Previa al Disparo")]
    public bool trackTargetDuringTelegraph = true;
    public override void Prepare(Transform origin, Transform target)
    {
        if (trackTargetDuringTelegraph && origin != null && target != null)
        {
            RotateRootTowards(origin, target.position);
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (trackTargetDuringTelegraph && origin != null && target != null && normalizedTime <= trackTargetUntil)
        {
            RotateRootTowards(origin, target.position);
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        yield return WaitForImpactTiming(origin);

        Transform rootTransform = GetRootTransform(origin);
        if (projectileInstance == null || rootTransform == null) yield break;

        Vector3 shootDirection = rootTransform.forward;
        shootDirection.y = 0f;
        shootDirection = shootDirection.sqrMagnitude > 0.001f ? shootDirection.normalized : Vector3.forward;

        Vector3 spawnPos = rootTransform.position + (Vector3.up * verticalOffset) + (shootDirection * forwardSpawnOffset);
        GameObject projectileObj = Instantiate(projectileInstance, spawnPos, Quaternion.LookRotation(shootDirection));

        if (projectileObj.TryGetComponent(out EntityProjectile projectileComp))
        {
            projectileComp.Init(shootDirection, projectileSpeed, damage, lifetime, rootTransform);
        }
    }
}