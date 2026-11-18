using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityMultiAreaAttack", menuName = "Rotbound/Entity/Attack/EntityMultiAreaAttack")]
public class EntityMultiAreaAttack : EntityAttack
{
    public enum MultiAreaStyle { Simultaneous, Consecutive }
    public enum AreaOrigin { Attacker, Target }
    public enum PatternType { Random, Linear, CircularRing, CrossShape, StarShape }

    [Header("Propiedades Multi-Área")]
    public MultiAreaStyle executionStyle = MultiAreaStyle.Simultaneous;
    public AreaOrigin areaOrigin = AreaOrigin.Attacker;
    public PatternType patternType = PatternType.Random;
    [Min(1)] public int numberOfAreas = 4;
    [Min(0.1f)] public float areaRadius = 2f;
    [Min(0f)] public float scatterRadius = 8f;
    [Min(0f)] public float delayBetweenAreas = 0.2f;
    public bool trackTarget = true;

    [Header("Oleadas")]
    [Min(1)] public int waveCount = 1;
    [Min(0f)] public float delayBetweenWaves = 0.5f;
    private readonly Dictionary<int, List<Vector3>> _centersByInstance = new();
    private readonly Collider[] _hitBuffer = new Collider[20];
    public override void Prepare(Transform origin, Transform target)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        ClearIndicatorsFor(root);
        int key = root.GetInstanceID();
        if (!_centersByInstance.TryGetValue(key, out List<Vector3> centers))
        {
            centers = new List<Vector3>();
            _centersByInstance[key] = centers;
        }
        centers.Clear();

        if (target != null && areaOrigin == AreaOrigin.Attacker)
            RotateRootTowards(root, target.position);

        Vector3 basePosition = (areaOrigin == AreaOrigin.Attacker || target == null)
            ? GetTelegraphOrigin(root)
            : GetGroundPosition(target.position, root);

        GeneratePatternCenters(centers, basePosition, root.forward, root);

        if (executionStyle == MultiAreaStyle.Simultaneous && indicatorPrefab != null)
        {
            for (int i = 0; i < centers.Count; i++)
            {
                SpawnIndicatorFor(root, centers[i], Quaternion.identity, new Vector3(areaRadius * 2f, 0.1f, areaRadius * 2f), telegraphDuration);
            }
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        if (trackTarget && normalizedTime <= trackTargetUntil)
        {
            if (target != null && areaOrigin == AreaOrigin.Attacker)
                RotateRootTowards(root, target.position);

            Vector3 basePosition = (areaOrigin == AreaOrigin.Attacker || target == null)
                ? GetTelegraphOrigin(root)
                : GetGroundPosition(target.position, root);

            int key = root.GetInstanceID();
            if (!_centersByInstance.TryGetValue(key, out List<Vector3> centers))
            {
                centers = new List<Vector3>();
                _centersByInstance[key] = centers;
            }
            centers.Clear();
            GeneratePatternCenters(centers, basePosition, root.forward, root);

            if (executionStyle == MultiAreaStyle.Simultaneous)
            {
                List<GameObject> inds = GetIndicatorsFor(root);
                if (inds != null)
                {
                    for (int i = 0; i < inds.Count && i < centers.Count; i++)
                    {
                        if (inds[i] != null) inds[i].transform.position = centers[i];
                    }
                }
            }
        }
    }
    private void GeneratePatternCenters(List<Vector3> outCenters, Vector3 center, Vector3 forwardDir, Transform root)
    {
        switch (patternType)
        {
            case PatternType.Linear:
                for (int i = 0; i < numberOfAreas; i++)
                {
                    float dist = (i - (numberOfAreas - 1) / 2f) * (scatterRadius / Mathf.Max(1, numberOfAreas));
                    outCenters.Add(GetGroundPosition(center + (forwardDir * dist), root));
                }
                break;

            case PatternType.CircularRing:
                float angleStep = 360f / numberOfAreas;
                for (int i = 0; i < numberOfAreas; i++)
                {
                    Vector3 dir = Quaternion.Euler(0, i * angleStep, 0) * forwardDir;
                    outCenters.Add(GetGroundPosition(center + (dir * scatterRadius), root));
                }
                break;

            case PatternType.CrossShape:
                Vector3[] crossDirs = { forwardDir, -forwardDir, Quaternion.Euler(0, 90, 0) * forwardDir, Quaternion.Euler(0, -90, 0) * forwardDir };
                for (int i = 0; i < numberOfAreas; i++)
                {
                    Vector3 dir = crossDirs[i % 4];
                    float step = ((i / 4) + 1) * (scatterRadius / 2f);
                    outCenters.Add(GetGroundPosition(center + (dir * step), root));
                }
                break;

            case PatternType.StarShape:
                int points = Mathf.Max(numberOfAreas, 5);
                float starAngle = 360f / points;
                for (int i = 0; i < points; i++)
                {
                    Vector3 dir = Quaternion.Euler(0, i * starAngle, 0) * forwardDir;
                    outCenters.Add(GetGroundPosition(center + (dir * scatterRadius), root));
                }
                break;

            default:
                for (int i = 0; i < numberOfAreas; i++)
                {
                    Vector2 rnd = Random.insideUnitCircle * scatterRadius;
                    Vector3 rawPoint = i == 0 ? center : center + new Vector3(rnd.x, 0f, rnd.y);
                    outCenters.Add(GetGroundPosition(rawPoint, root));
                }
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
                SetIndicatorsImminentFor(root);
            }

            _centersByInstance.TryGetValue(root.GetInstanceID(), out List<Vector3> centers);
            if (centers == null) yield break;

            if (executionStyle == MultiAreaStyle.Simultaneous)
            {
                for (int i = 0; i < centers.Count; i++) ApplyAreaDamage(centers[i]);
                ClearIndicatorsFor(root);
            }
            else
            {
                float individualTelegraph = telegraphDuration / Mathf.Max(numberOfAreas, 1);
                for (int i = 0; i < centers.Count; i++)
                {
                    Vector3 center = centers[i];
                    SpawnIndicatorFor(root, center, Quaternion.identity, new Vector3(areaRadius * 2f, 0.1f, areaRadius * 2f), individualTelegraph);
                    yield return new WaitForSeconds(individualTelegraph);
                    SetIndicatorsImminentFor(root);
                    ApplyAreaDamage(center);
                    ClearIndicatorsFor(root);
                    if (i < centers.Count - 1 && delayBetweenAreas > 0) yield return new WaitForSeconds(delayBetweenAreas);
                }
            }

            if (w < waveCount - 1 && delayBetweenWaves > 0) yield return new WaitForSeconds(delayBetweenWaves);
        }
    }
    private void ApplyAreaDamage(Vector3 center)
    {
        SpawnImpactVfx(center, Quaternion.identity);

        int hitCount = Physics.OverlapSphereNonAlloc(center, areaRadius, _hitBuffer, ~0, QueryTriggerInteraction.Ignore);
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