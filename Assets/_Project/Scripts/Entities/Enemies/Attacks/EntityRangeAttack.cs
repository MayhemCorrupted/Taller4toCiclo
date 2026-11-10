using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityRangeAttack", menuName = "Rotbound/Entity/Attack/EntityRangeAttack")]
public class EntityRangeAttack : EntityAttack
{
    [Header("RANGED PROPERTIES")]
    public GameObject projectileInstance;
    [Min(0.1f)] public float projectileSpeed = 10f;
    [Min(0.0f)] public float lifetime = 10.0f;
    [Min(0.0f)] public float precisionFactor = 1.0f;
    [Min(0.0f)] public float maxDeviation = 1.0f;
    public float verticalOffset = 1.0f;
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (projectileInstance == null || target == null) yield break;

        Vector3 spawnPos = origin.position + (Vector3.up * verticalOffset);
        PlayerMovement plMov = target.GetComponentInParent<PlayerMovement>();

        Vector3 targetVelocity = plMov != null ? plMov.CurrentVelocity : Vector3.zero;
        Vector3 predictedTarget = PredictPosition(target.position, spawnPos, targetVelocity, projectileSpeed);
        predictedTarget = TweakPrecision(predictedTarget);

        Vector3 shootDirection = (predictedTarget - spawnPos).normalized;
        if (shootDirection.sqrMagnitude < 0.001f) shootDirection = origin.forward;

        GameObject projectileObj = Instantiate(projectileInstance, spawnPos, Quaternion.LookRotation(shootDirection));

        if (projectileObj.TryGetComponent(out Projectile projectileComp))
        {
            projectileComp.Init(shootDirection * projectileSpeed, damage, lifetime, origin);
        }

        yield return null;
    }
    private Vector3 PredictPosition(Vector3 targetPosition, Vector3 firingPosition, Vector3 targetVelocity, float speed)
    {
        float distance = (targetPosition - firingPosition).magnitude;
        float estimatedTime = distance / Mathf.Max(speed, 0.1f);
        return targetPosition + (targetVelocity * estimatedTime);
    }
    private Vector3 TweakPrecision(Vector3 initialPrediction)
    {
        if (precisionFactor >= 1.0f) return initialPrediction;
        float errorRadius = (1.0f - precisionFactor) * maxDeviation;
        return initialPrediction + (Random.insideUnitSphere * errorRadius);
    }
}