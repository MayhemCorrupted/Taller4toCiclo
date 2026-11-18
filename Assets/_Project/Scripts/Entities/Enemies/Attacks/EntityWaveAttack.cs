using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityWaveAttack", menuName = "Rotbound/Entity/Attack/EntityWaveAttack")]
public class EntityWaveAttack : EntityAttack
{
    [Header("Propiedades de Onda")]
    [Min(0.1f)] public float waveDistance = 10f;
    [Min(0.1f)] public float hitboxRadius = 1.5f;
    public bool trackTarget = true;

    private readonly Collider[] _hitBuffer = new Collider[12];
    public override void Prepare(Transform origin, Transform target)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        ClearIndicatorsFor(root);
        if (trackTarget && target != null) RotateRootTowards(root, target.position);

        Vector3 groundOrigin = GetTelegraphOrigin(root);
        Vector3 dir = root.forward;
        SpawnIndicatorFor(root, groundOrigin + (dir * (waveDistance * 0.5f)), Quaternion.LookRotation(dir), new Vector3(hitboxRadius * 2f, 0.1f, waveDistance), telegraphDuration);
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
                Vector3 groundOrigin = GetTelegraphOrigin(root);
                Vector3 dir = root.forward;
                inds[0].transform.SetPositionAndRotation(groundOrigin + (dir * (waveDistance * 0.5f)), Quaternion.LookRotation(dir));
                inds[0].transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, waveDistance);
            }
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        yield return WaitForImpactTiming(origin);

        Transform root = GetRootTransform(origin);
        ClearIndicatorsFor(root);
        if (root == null) yield break;

        Vector3 dir = root.forward;
        Vector3 groundOrigin = GetTelegraphOrigin(root);
        SpawnImpactVfx(groundOrigin, Quaternion.LookRotation(dir));

        Vector3 boxCenter = root.position + (dir * (waveDistance * 0.5f));
        Vector3 boxHalfExtents = new(hitboxRadius, 2f, waveDistance * 0.5f);

        int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, _hitBuffer, Quaternion.LookRotation(dir), ~0, QueryTriggerInteraction.Ignore);
        HashSet<PlayerHealth> processedPlayers = new();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hitBuffer[i];
            PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph != null && processedPlayers.Add(ph))
            {
                ApplyDamageAndKnockback(hit, dir, root.position);
            }
        }
    }
}