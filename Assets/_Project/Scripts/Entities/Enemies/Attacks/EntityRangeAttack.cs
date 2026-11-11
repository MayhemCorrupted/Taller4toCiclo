using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityRangeAttack", menuName = "Rotbound/Entity/Attack/EntityRangeAttack")]
public class EntityRangeAttack : EntityAttack
{
    [Header("RANGED PROPERTIES")]
    public GameObject projectileInstance;
    [Min(0.1f)] public float projectileSpeed = 15f;
    [Min(0.0f)] public float lifetime = 5.0f;
    public float verticalOffset = 1.0f;

    [Header("PREDICTION & PROBABILITY")]
    [Tooltip("0 = Dispara directo al jugador. 1 = Interceptación predictiva total.")]
    [Range(0f, 1f)] public float predictionIntensity = 1.0f;

    [Tooltip("Distancia máxima a la que el enemigo considera viable el disparo para calcular la probabilidad.")]
    public float maxEvaluationDistance = 15f;
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (projectileInstance == null || target == null) yield break;

        Vector3 spawnPos = origin.position + (Vector3.up * verticalOffset);
        PlayerMovement plMov = target.GetComponentInParent<PlayerMovement>();

        Vector3 flatTargetPos = target.position;
        flatTargetPos.y = spawnPos.y;

        Vector3 targetVelocity = plMov != null ? plMov.CurrentVelocity : Vector3.zero;
        targetVelocity.y = 0f;

        Vector3 perfectlyPredictedTarget = PredictKinematicPosition(flatTargetPos, spawnPos, targetVelocity, projectileSpeed);

        Vector3 finalAimPoint = Vector3.Lerp(flatTargetPos, perfectlyPredictedTarget, predictionIntensity);

        Vector3 shootDirection = (finalAimPoint - spawnPos).normalized;
        if (shootDirection.sqrMagnitude < 0.001f) shootDirection = origin.forward;

        shootDirection.y = 0f;
        shootDirection.Normalize();

        GameObject projectileObj = Instantiate(projectileInstance, spawnPos, Quaternion.LookRotation(shootDirection));

        if (projectileObj.TryGetComponent(out EntityProjectile projectileComp))
        {
            projectileComp.Init(shootDirection, projectileSpeed, damage, lifetime, origin);
        }

        yield return null;
    }
    private float CalculateHitProbability(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVel)
    {
        float distance = Vector3.Distance(shooterPos, targetPos);

        if (distance > maxEvaluationDistance) return 0.1f;

        float distanceFactor = 1f - Mathf.Clamp01(distance / maxEvaluationDistance);

        float speedMagnitude = targetVel.magnitude;
        float speedFactor = Mathf.Clamp01(1f - (speedMagnitude / 10f));

        float finalProbability = (distanceFactor * 0.7f) + (speedFactor * 0.3f);

        return Mathf.Clamp(finalProbability, 0.05f, 1.0f);
    }
    private Vector3 PredictKinematicPosition(Vector3 targetPos, Vector3 shooterPos, Vector3 targetVel, float projSpeed)
    {
        Vector3 displacement = targetPos - shooterPos;

        float a = targetVel.sqrMagnitude - (projSpeed * projSpeed);
        float b = 2f * Vector3.Dot(displacement, targetVel);
        float c = displacement.sqrMagnitude;

        if (Mathf.Abs(a) < 0.001f)
        {
            float t = -c / Mathf.Max(b, 0.001f);
            if (t > 0) return targetPos + (targetVel * t);
        }
        else
        {
            float discriminant = (b * b) - (4f * a * c);

            if (discriminant >= 0)
            {
                float t1 = (-b + Mathf.Sqrt(discriminant)) / (2f * a);
                float t2 = (-b - Mathf.Sqrt(discriminant)) / (2f * a);

                float t = -1f;
                if (t1 > 0 && t2 > 0) t = Mathf.Min(t1, t2);
                else if (t1 > 0) t = t1;
                else if (t2 > 0) t = t2;

                if (t > 0) return targetPos + (targetVel * t);
            }
        }
        float fallbackTime = displacement.magnitude / Mathf.Max(projSpeed, 0.1f);
        return targetPos + (targetVel * fallbackTime);
    }
}