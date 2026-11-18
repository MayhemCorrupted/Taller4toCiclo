using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityAreaAttack", menuName = "Rotbound/Entity/Attack/EntityAreaAttack")]
public class EntityAreaAttack : EntityAttack
{
    public enum AreaCenterMode { Attacker, Target, OffsetFromTarget }

    [Header("Propiedades de Área")]
    public AreaCenterMode centerMode = AreaCenterMode.Target;
    [Min(0.1f)] public float radius = 3.0f;
    public Vector3 targetOffset = Vector3.zero;
    public bool trackTarget = true;
    private readonly Dictionary<int, Vector3> _centersByInstance = new();
    private readonly Collider[] _hitBuffer = new Collider[16];
    private Vector3 ComputeCenter(Transform root, Transform target)
    {
        if (centerMode == AreaCenterMode.Attacker || target == null)
            return GetTelegraphOrigin(root);

        Vector3 rawPos = centerMode == AreaCenterMode.Target
            ? target.position
            : target.position + targetOffset;

        return GetGroundPosition(rawPos, root);
    }
    public override void Prepare(Transform origin, Transform target)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        ClearIndicatorsFor(root);
        if (target != null && centerMode == AreaCenterMode.Attacker)
            RotateRootTowards(root, target.position);

        Vector3 center = ComputeCenter(root, target);
        _centersByInstance[root.GetInstanceID()] = center;
        SpawnIndicatorFor(root, center, Quaternion.identity, new Vector3(radius * 2f, 0.1f, radius * 2f), telegraphDuration);
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        if (trackTarget && target != null && normalizedTime <= trackTargetUntil)
        {
            if (centerMode == AreaCenterMode.Attacker)
                RotateRootTowards(root, target.position);

            Vector3 center = ComputeCenter(root, target);
            _centersByInstance[root.GetInstanceID()] = center;

            List<GameObject> inds = GetIndicatorsFor(root);
            if (inds != null && inds.Count > 0 && inds[0] != null)
                inds[0].transform.position = center;
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        yield return WaitForImpactTiming(origin);

        Transform root = GetRootTransform(origin);
        Vector3 center = (root != null && _centersByInstance.TryGetValue(root.GetInstanceID(), out Vector3 saved))
            ? saved
            : ComputeCenter(root, target);

        ClearIndicatorsFor(root);
        SpawnImpactVfx(center, root != null ? root.rotation : Quaternion.identity);

        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, _hitBuffer, ~0, QueryTriggerInteraction.Ignore);
        HashSet<PlayerHealth> processedPlayers = new();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hitBuffer[i];
            PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph != null && processedPlayers.Add(ph))
            {
                Vector3 knockbackDir = ph.transform.position - center;
                ApplyDamageAndKnockback(hit, knockbackDir, center);
            }
        }
    }
}