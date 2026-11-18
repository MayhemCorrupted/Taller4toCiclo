using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityMeleeAttack", menuName = "Rotbound/Entity/Attack/EntityMeleeAttack")]
public class EntityMeleeAttack : EntityAttack
{
    [Header("Propiedades Melee")]
    public bool isRadial = false;
    [Min(0.1f)] public float radius = 2f;
    public Vector3 hitboxMeasurements = new(2f, 1.5f, 2.5f);
    public float hitboxVerticalOffset = 0.8f;
    private readonly Collider[] _hitBuffer = new Collider[12];
    public override void Prepare(Transform origin, Transform target)
    {
        ClearIndicatorsFor(origin);
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        if (target != null) RotateRootTowards(root, target.position);

        Vector3 groundOrigin = GetTelegraphOrigin(root);
        if (isRadial)
        {
            SpawnIndicatorFor(root, groundOrigin, Quaternion.identity, new Vector3(radius * 2f, 0.1f, radius * 2f), telegraphDuration);
        }
        else
        {
            Vector3 boxCenter = groundOrigin + (root.forward * (hitboxMeasurements.z * 0.5f));
            SpawnIndicatorFor(root, boxCenter, Quaternion.LookRotation(root.forward), new Vector3(hitboxMeasurements.x, 0.1f, hitboxMeasurements.z), telegraphDuration);
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        if (target != null && normalizedTime <= trackTargetUntil)
        {
            RotateRootTowards(root, target.position);
        }

        List<GameObject> inds = GetIndicatorsFor(root);
        if (inds != null && inds.Count > 0 && inds[0] != null && !isRadial)
        {
            Vector3 groundOrigin = GetTelegraphOrigin(root);
            Vector3 boxCenter = groundOrigin + (root.forward * (hitboxMeasurements.z * 0.5f));
            inds[0].transform.SetPositionAndRotation(boxCenter, Quaternion.LookRotation(root.forward));
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        yield return WaitForImpactTiming(origin);

        Transform root = GetRootTransform(origin);
        ClearIndicatorsFor(root);
        if (root == null) yield break;

        Vector3 groundOrigin = GetTelegraphOrigin(root);
        Vector3 hitboxOrigin = root.position + (Vector3.up * hitboxVerticalOffset);
        int hitCount;

        if (isRadial)
        {
            SpawnImpactVfx(groundOrigin, root.rotation);
            hitCount = Physics.OverlapSphereNonAlloc(hitboxOrigin, radius, _hitBuffer, ~0, QueryTriggerInteraction.Ignore);
        }
        else
        {
            Vector3 vfxCenter = groundOrigin + (root.forward * (hitboxMeasurements.z * 0.5f));
            SpawnImpactVfx(vfxCenter, root.rotation);
            Vector3 boxCenter = hitboxOrigin + (root.forward * (hitboxMeasurements.z * 0.5f));
            hitCount = Physics.OverlapBoxNonAlloc(boxCenter, hitboxMeasurements * 0.5f, _hitBuffer, root.rotation, ~0, QueryTriggerInteraction.Ignore);
        }

        HashSet<PlayerHealth> processedPlayers = new();
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hitBuffer[i];
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && processedPlayers.Add(playerHealth))
            {
                Vector3 knockbackDir = playerHealth.transform.position - root.position;
                ApplyDamageAndKnockback(hit, knockbackDir, root.position);
            }
        }
    }
}