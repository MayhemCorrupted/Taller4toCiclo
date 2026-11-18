using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
[CreateAssetMenu(fileName = "EntityChargeAttack", menuName = "Rotbound/Entity/Attack/EntityChargeAttack")]
public class EntityChargeAttack : EntityAttack
{
    [Header("Propiedades de Embestida")]
    [Min(0.1f)] public float maxChargeDistance = 10f;
    [Min(0.1f)] public float hitboxRadius = 1.5f;
    public bool trackTarget = true;

    private readonly Dictionary<int, Vector3> _chargeDestByInstance = new();
    private readonly Collider[] _hitBuffer = new Collider[12];
    public override void Prepare(Transform origin, Transform target)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        ClearIndicatorsFor(root);
        if (trackTarget && target != null) RotateRootTowards(root, target.position);

        float agentRadius = root.TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0.5f;
        Vector3 dest = GetSafeChargeDestination(root, root.forward, maxChargeDistance, agentRadius);
        _chargeDestByInstance[root.GetInstanceID()] = dest;

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
            float agentRadius = root.TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0.5f;
            Vector3 dest = GetSafeChargeDestination(root, root.forward, maxChargeDistance, agentRadius);
            _chargeDestByInstance[root.GetInstanceID()] = dest;

            List<GameObject> inds = GetIndicatorsFor(root);
            if (inds != null && inds.Count > 0 && inds[0] != null)
            {
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
        ClearIndicatorsFor(root);
        if (root == null) yield break;

        SpawnImpactVfx(GetTelegraphOrigin(root), root.rotation);

        float agentRadius = root.TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0.5f;
        Vector3 destination = _chargeDestByInstance.TryGetValue(root.GetInstanceID(), out Vector3 saved)
            ? saved
            : GetSafeChargeDestination(root, root.forward, maxChargeDistance, agentRadius);

        if (activeMotionDuration <= 0f)
        {
            root.position = destination;
            yield break;
        }

        Vector3 startPos = root.position;
        float timer = 0f;
        HashSet<PlayerHealth> damagedPlayers = new();

        while (timer < activeMotionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / activeMotionDuration);
            root.position = Vector3.Lerp(startPos, destination, t);

            int hitCount = Physics.OverlapSphereNonAlloc(root.position, hitboxRadius, _hitBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];
                PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
                if (ph != null && damagedPlayers.Add(ph))
                {
                    ApplyDamageAndKnockback(hit, root.forward, root.position);
                }
            }
            yield return null;
        }
    }
}