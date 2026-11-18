using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityMultiWaveAttack", menuName = "Rotbound/Entity/Attack/EntityMultiWaveAttack")]
public class EntityMultiWaveAttack : EntityAttack
{
    public enum MultiWaveStyle { SimultaneousWave, ConsecutiveWaves }
    public enum WavePattern { RadialWave, FanAbanico, CrossStar, ConsecutiveLineal, Random }

    [Header("Propiedades Multi-Onda")]
    public MultiWaveStyle waveStyle = MultiWaveStyle.ConsecutiveWaves;
    public WavePattern pattern = WavePattern.RadialWave;
    [Min(1)] public int numberOfWaves = 3;
    [Min(0.1f)] public float waveDistance = 10f;
    [Min(0.1f)] public float hitboxRadius = 1.5f;
    public bool trackTarget = true;
    [Min(0f)] public float delayBetweenWaves = 0.2f;

    [Header("Oleadas")]
    [Min(1)] public int waveCount = 1;
    [Min(0f)] public float delayBetweenWaveRepeats = 0.5f;

    private readonly Dictionary<int, List<Vector3>> _directionsByInstance = new();
    private readonly Collider[] _hitBuffer = new Collider[20];
    public override void Prepare(Transform origin, Transform target)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        ClearIndicatorsFor(root);
        if (target != null && trackTarget) RotateRootTowards(root, target.position);

        int key = root.GetInstanceID();
        if (!_directionsByInstance.TryGetValue(key, out List<Vector3> dirs))
        {
            dirs = new List<Vector3>();
            _directionsByInstance[key] = dirs;
        }
        dirs.Clear();
        GenerateDirections(dirs);

        Vector3 groundOrigin = GetTelegraphOrigin(root);
        if (waveStyle == MultiWaveStyle.SimultaneousWave)
        {
            for (int i = 0; i < dirs.Count; i++)
            {
                Vector3 worldDir = root.rotation * dirs[i];
                SpawnIndicatorFor(root, groundOrigin + (worldDir * (waveDistance * 0.5f)), Quaternion.LookRotation(worldDir), new Vector3(hitboxRadius * 2f, 0.1f, waveDistance), telegraphDuration);
            }
        }
        else if (dirs.Count > 0)
        {
            Vector3 worldDir = root.rotation * dirs[0];
            SpawnIndicatorFor(root, groundOrigin + (worldDir * (waveDistance * 0.5f)), Quaternion.LookRotation(worldDir), new Vector3(hitboxRadius * 2f, 0.1f, waveDistance), telegraphDuration);
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        if (trackTarget && target != null && normalizedTime <= trackTargetUntil)
        {
            RotateRootTowards(root, target.position);
            Vector3 groundOrigin = GetTelegraphOrigin(root);
            List<GameObject> inds = GetIndicatorsFor(root);
            if (!_directionsByInstance.TryGetValue(root.GetInstanceID(), out List<Vector3> dirs) || inds == null) return;

            if (waveStyle == MultiWaveStyle.SimultaneousWave)
            {
                for (int i = 0; i < inds.Count && i < dirs.Count; i++)
                {
                    if (inds[i] != null)
                    {
                        Vector3 worldDir = root.rotation * dirs[i];
                        inds[i].transform.SetPositionAndRotation(groundOrigin + (worldDir * (waveDistance * 0.5f)), Quaternion.LookRotation(worldDir));
                    }
                }
            }
            else if (inds.Count > 0 && inds[0] != null && dirs.Count > 0)
            {
                Vector3 worldDir = root.rotation * dirs[0];
                inds[0].transform.SetPositionAndRotation(groundOrigin + (worldDir * (waveDistance * 0.5f)), Quaternion.LookRotation(worldDir));
            }
        }
    }
    private void GenerateDirections(List<Vector3> outDirs)
    {
        switch (pattern)
        {
            case WavePattern.FanAbanico:
                float totalAngle = 90f;
                float startAngle = -totalAngle / 2f;
                float step = totalAngle / Mathf.Max(numberOfWaves - 1, 1);
                for (int i = 0; i < numberOfWaves; i++)
                    outDirs.Add(Quaternion.Euler(0, startAngle + (i * step), 0) * Vector3.forward);
                break;

            case WavePattern.CrossStar:
                int count = Mathf.Max(numberOfWaves, 4);
                float angleCross = 360f / count;
                for (int i = 0; i < count; i++)
                    outDirs.Add(Quaternion.Euler(0, i * angleCross, 0) * Vector3.forward);
                break;

            case WavePattern.Random:
                for (int i = 0; i < numberOfWaves; i++)
                    outDirs.Add(Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward);
                break;

            default:
                float angleRadial = 360f / numberOfWaves;
                for (int i = 0; i < numberOfWaves; i++)
                    outDirs.Add(Quaternion.Euler(0, i * angleRadial, 0) * Vector3.forward);
                break;
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
                Prepare(root, target);
                yield return new WaitForSeconds(telegraphDuration);
            }

            if (!_directionsByInstance.TryGetValue(root.GetInstanceID(), out List<Vector3> dirs)) yield break;

            if (waveStyle == MultiWaveStyle.SimultaneousWave)
            {
                HashSet<PlayerHealth> processedPlayers = new();
                Vector3 groundOrigin = GetTelegraphOrigin(root);
                for (int i = 0; i < dirs.Count; i++)
                {
                    Vector3 worldDir = root.rotation * dirs[i];
                    SpawnImpactVfx(groundOrigin, Quaternion.LookRotation(worldDir));

                    Vector3 boxCenter = root.position + (worldDir * (waveDistance * 0.5f));
                    Vector3 boxHalfExtents = new(hitboxRadius, 2f, waveDistance * 0.5f);

                    int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, _hitBuffer, Quaternion.LookRotation(worldDir), ~0, QueryTriggerInteraction.Ignore);
                    for (int j = 0; j < hitCount; j++)
                    {
                        Collider hit = _hitBuffer[j];
                        PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
                        if (ph != null && processedPlayers.Add(ph))
                            ApplyDamageAndKnockback(hit, worldDir, root.position);
                    }
                }
                ClearIndicatorsFor(root);
            }
            else
            {
                ClearIndicatorsFor(root);
                for (int i = 0; i < dirs.Count; i++)
                {
                    if (i > 0 && delayBetweenWaves > 0) yield return new WaitForSeconds(delayBetweenWaves);
                    if (trackTarget && target != null) RotateRootTowards(root, target.position);

                    Vector3 waveDir = root.rotation * dirs[i];
                    SpawnImpactVfx(GetTelegraphOrigin(root), Quaternion.LookRotation(waveDir));

                    Vector3 boxCenter = root.position + (waveDir * (waveDistance * 0.5f));
                    Vector3 boxHalfExtents = new(hitboxRadius, 2f, waveDistance * 0.5f);

                    int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, _hitBuffer, Quaternion.LookRotation(waveDir), ~0, QueryTriggerInteraction.Ignore);
                    HashSet<PlayerHealth> processedPlayers = new();

                    for (int j = 0; j < hitCount; j++)
                    {
                        Collider hit = _hitBuffer[j];
                        PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
                        if (ph != null && processedPlayers.Add(ph))
                            ApplyDamageAndKnockback(hit, waveDir, root.position);
                    }
                    yield return null;
                }
            }
            if (w < waveCount - 1 && delayBetweenWaveRepeats > 0) yield return new WaitForSeconds(delayBetweenWaveRepeats);
        }
    }
}