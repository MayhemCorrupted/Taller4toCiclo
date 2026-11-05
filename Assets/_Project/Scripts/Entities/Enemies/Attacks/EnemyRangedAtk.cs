using UnityEngine;

[CreateAssetMenu(fileName = "EnemyRangedAtk", menuName = "Rotbound/EnemyRangedAtk")]
public class EnemyRangedAtk : EnemyAttack
{
    [Header("RANGED PROPERTIES")]
    public GameObject projectileInstance;
    [Min(0.1f)] public float projectileSpeed = 10f; 
    [Min(0.0f)] public float lifetime = 10.0f;
    [Min(0.0f)] public float precisionFactor = 1.0f;
    [Min(0.0f)] public float maxDeviation = 1.0f;
    public float verticalOffset = 0.0f;

    public override void Execute(Transform origin, Transform target)
    {
        if (projectileInstance == null) return;

        PlayerMovement plMov = target.GetComponentInParent<PlayerMovement>();

        if (plMov != null)
        {
            float safeSpeed = Mathf.Max(0.1f, projectileSpeed);

            Vector3 targetPosition = TweakPrecision(PredictPosition(target.position, origin.position, plMov.CurrentVelocity, safeSpeed));
            Vector3 shootDirection = (targetPosition - origin.position).normalized;

            if (shootDirection.sqrMagnitude < 0.001f)
            {
                shootDirection = origin.forward;
            }

            GameObject projectile = Instantiate(projectileInstance, origin.position + Vector3.up * verticalOffset, Quaternion.LookRotation(shootDirection));

            if (projectile.TryGetComponent(out Projectile projectileComp))
            {
                projectileComp.Init(shootDirection * safeSpeed, damage, lifetime);
            }
        }
    }
    private Vector3 PredictPosition(Vector3 targetPosition, Vector3 firingPosition, Vector3 targetVelocity, float speed)
    {
        float distance = (targetPosition - firingPosition).magnitude;
        float estimatedTime = distance / speed;
        return targetPosition + (targetVelocity * estimatedTime);
    }
    private Vector3 TweakPrecision(Vector3 initialPrediction)
    {
        if (precisionFactor >= 1.0f)
            return initialPrediction;

        float errorRadius = (1.0f - precisionFactor) * maxDeviation;
        Vector3 deviation = Random.insideUnitSphere * errorRadius;

        return initialPrediction + deviation;
    }
}