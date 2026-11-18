using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
[CreateAssetMenu(fileName = "EntityMultiChargeAttack", menuName = "Rotbound/Entity/Attack/EntityMultiChargeAttack")]
public class EntityMultiChargeAttack : EntityAttack
{
    [Header("Propiedades Multi-Embestida")]
    [Min(1)] public int numberOfCharges = 3;
    [Min(0.1f)] public float maxChargeDistance = 10f;
    [Min(0.1f)] public float hitboxRadius = 1.5f;
    public bool trackTarget = true;
    [Min(0f)] public float delayBetweenCharges = 0.2f;

    [Header("Oleadas")]
    [Min(1)] public int waveCount = 1;
    [Min(0f)] public float delayBetweenWaves = 0.5f;

    private readonly Collider[] _hitBuffer = new Collider[20];
    private readonly HashSet<PlayerHealth> _damagedPlayers = new();
    public override void Prepare(Transform origin, Transform target)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        ClearIndicatorsFor(root);
        if (target != null && trackTarget) RotateRootTowards(root, target.position);

        float agentRadius = root.TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0.5f;
        Vector3 dest = GetSafeChargeDestination(root, root.forward, maxChargeDistance, agentRadius);
        float actualDist = Mathf.Max(0.2f, Vector3.Distance(root.position, dest));
        Vector3 groundOrigin = GetTelegraphOrigin(root);

        SpawnIndicatorFor(root, groundOrigin + (root.forward * (actualDist * 0.5f)), Quaternion.LookRotation(root.forward), new Vector3(hitboxRadius * 2f, 0.1f, actualDist), telegraphDuration);
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        if (trackTarget && target != null && normalizedTime <= trackTargetUntil)
        {
            RotateRootTowards(root, target.position);
            List<GameObject> inds = GetIndicatorsFor(root);
            if (inds != null && inds.Count > 0 && inds[0] != null)
            {
                float agentRadius = root.TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0.5f;
                Vector3 dest = GetSafeChargeDestination(root, root.forward, maxChargeDistance, agentRadius);
                float actualDist = Mathf.Max(0.2f, Vector3.Distance(root.position, dest));
                Vector3 groundOrigin = GetTelegraphOrigin(root);

                inds[0].transform.SetPositionAndRotation(groundOrigin + (root.forward * (actualDist * 0.5f)), Quaternion.LookRotation(root.forward));
                inds[0].transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, actualDist);
            }
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        yield return WaitForImpactTiming(origin);

        Transform root = GetRootTransform(origin);
        if (root == null) yield break;

        for (int w = 0; w < waveCount; w++)
        {
            if (w > 0)
            {
                yield return TelegraphRoutine(root, target);
            }

            yield return ConsecutiveDashesRoutine(root, target);

            if (w < waveCount - 1 && delayBetweenWaves > 0) yield return new WaitForSeconds(delayBetweenWaves);
        }
    }
    private IEnumerator ConsecutiveDashesRoutine(Transform root, Transform target)
    {
        float dashDuration = activeMotionDuration > 0 ? (activeMotionDuration / numberOfCharges) : 0.15f;
        float agentRadius = root.TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0.5f;

        for (int i = 0; i < numberOfCharges; i++)
        {
            if (i > 0)
            {
                if (delayBetweenCharges > 0) yield return new WaitForSeconds(delayBetweenCharges);
                yield return TelegraphRoutine(root, target);
            }

            ClearIndicatorsFor(root);
            Vector3 chargeDir = root.forward;
            Vector3 startPos = root.position;
            Vector3 targetPos = GetSafeChargeDestination(root, chargeDir, maxChargeDistance, agentRadius);

            _damagedPlayers.Clear();
            float timer = 0f;

            while (timer < dashDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / dashDuration);
                root.position = Vector3.Lerp(startPos, targetPos, t);

                int hitCount = Physics.OverlapSphereNonAlloc(root.position, hitboxRadius, _hitBuffer, ~0, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hitCount; h++)
                {
                    Collider hit = _hitBuffer[h];
                    PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
                    if (ph != null && _damagedPlayers.Add(ph))
                    {
                        ApplyDamageAndKnockback(hit, chargeDir, root.position);
                    }
                }
                yield return null;
            }
        }
    }
    private IEnumerator TelegraphRoutine(Transform root, Transform target)
    {
        Prepare(root, target);
        float prepTimer = 0f;
        while (prepTimer < telegraphDuration)
        {
            prepTimer += Time.deltaTime;
            UpdatePreparation(root, target, prepTimer / Mathf.Max(telegraphDuration, 0.001f));
            yield return null;
        }
    }
}