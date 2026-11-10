using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityMultiAreaAttack", menuName = "Rotbound/Entity/Attack/EntityMultiAreaAttack")]
public class EntityMultiAreaAttack : EntityAttack
{
    public enum MultiAreaStyle { Simultaneous, Consecutive }
    public enum AreaOrigin { Attacker, Target }
    public enum PatternType { Random, Linear, CircularRing, CrossShape, StarShape }

    [Header("MULTI-AREA PROPERTIES")]
    public MultiAreaStyle executionStyle = MultiAreaStyle.Simultaneous;
    public AreaOrigin areaOrigin = AreaOrigin.Attacker;
    public PatternType patternType = PatternType.Random;
    public int numberOfAreas = 4;
    [Min(0.1f)] public float areaRadius = 2f;
    public float scatterRadius = 8f;
    [Min(0f)] public float delayBetweenAreas = 0.2f;
    [Tooltip("Si es verdadero, las áreas seguirán la posición/orientación del objetivo durante el telegrafiado.")]
    public bool trackTarget = true;

    [Header("OLEADAS DE ATAQUE (WAVES)")]
    [Min(1)] public int waveCount = 1;
    [Min(0f)] public float delayBetweenWaves = 0.5f;

    public GameObject indicatorPrefab;
    private readonly List<Vector3> _calculatedCenters = new();
    private readonly List<GameObject> _indicators = new();
    private readonly Collider[] _hitBuffer = new Collider[20];
    public override void Prepare(Transform origin, Transform target)
    {
        _calculatedCenters.Clear();
        ClearIndicators();

        Vector3 basePosition = (areaOrigin == AreaOrigin.Attacker || target == null) ? origin.position : target.position;
        basePosition.y = origin.position.y;
        origin.TryGetComponent(out Health entityHealth);

        GeneratePatternCenters(basePosition, origin.forward);

        if (executionStyle == MultiAreaStyle.Simultaneous && indicatorPrefab != null)
        {
            foreach (var center in _calculatedCenters)
            {
                InstantiateIndicator(center, telegraphDuration, entityHealth);
            }
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (trackTarget && origin != null && normalizedTime <= trackTargetUntil)
        {
            if (target != null && areaOrigin == AreaOrigin.Attacker)
            {
                Vector3 lookDir = (target.position - origin.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero) origin.rotation = Quaternion.LookRotation(lookDir);
            }

            Vector3 basePosition = (areaOrigin == AreaOrigin.Attacker || target == null) ? origin.position : target.position;
            basePosition.y = origin.position.y;

            _calculatedCenters.Clear();
            GeneratePatternCenters(basePosition, origin.forward);

            if (executionStyle == MultiAreaStyle.Simultaneous)
            {
                for (int i = 0; i < _indicators.Count && i < _calculatedCenters.Count; i++)
                {
                    if (_indicators[i] != null)
                    {
                        _indicators[i].transform.position = _calculatedCenters[i];
                    }
                }
            }
        }
    }
    private void GeneratePatternCenters(Vector3 center, Vector3 forwardDir)
    {
        switch (patternType)
        {
            case PatternType.Linear:
                for (int i = 0; i < numberOfAreas; i++)
                {
                    float dist = (i - (numberOfAreas - 1) / 2f) * (scatterRadius / numberOfAreas);
                    _calculatedCenters.Add(center + (forwardDir * dist));
                }
                break;

            case PatternType.CircularRing:
                float angleStep = 360f / numberOfAreas;
                for (int i = 0; i < numberOfAreas; i++)
                {
                    Vector3 dir = Quaternion.Euler(0, i * angleStep, 0) * forwardDir;
                    _calculatedCenters.Add(center + (dir * scatterRadius));
                }
                break;

            case PatternType.CrossShape:
                Vector3[] crossDirs = { forwardDir, -forwardDir, Quaternion.Euler(0, 90, 0) * forwardDir, Quaternion.Euler(0, -90, 0) * forwardDir };
                for (int i = 0; i < numberOfAreas; i++)
                {
                    Vector3 dir = crossDirs[i % 4];
                    float step = ((i / 4) + 1) * (scatterRadius / 2f);
                    _calculatedCenters.Add(center + (dir * step));
                }
                break;

            case PatternType.StarShape:
                int points = Mathf.Max(numberOfAreas, 5);
                float starAngle = 360f / points;
                for (int i = 0; i < points; i++)
                {
                    Vector3 dir = Quaternion.Euler(0, i * starAngle, 0) * forwardDir;
                    _calculatedCenters.Add(center + (dir * scatterRadius));
                }
                break;

            default:
                for (int i = 0; i < numberOfAreas; i++)
                {
                    Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * Random.Range(0, scatterRadius);
                    _calculatedCenters.Add(i == 0 ? center : center + offset);
                }
                break;
        }
    }
    private void InstantiateIndicator(Vector3 center, float duration, Health entityHealth)
    {
        GameObject indicatorObj = Instantiate(indicatorPrefab, center, Quaternion.identity);
        indicatorObj.transform.localScale = new Vector3(areaRadius * 2f, 0.1f, areaRadius * 2f);

        if (indicatorObj.TryGetComponent(out EntityIndicator indicatorComp))
        {
            indicatorComp.Init(entityHealth);
            indicatorComp.AnimateIndicator(duration);
        }
        _indicators.Add(indicatorObj);
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        origin.TryGetComponent(out Health entityHealth);

        for (int w = 0; w < waveCount; w++)
        {
            if (w > 0)
            {
                Prepare(origin, target);
                yield return new WaitForSeconds(telegraphDuration);
            }

            if (executionStyle == MultiAreaStyle.Simultaneous)
            {
                foreach (var center in _calculatedCenters) ApplyAreaDamage(center);
                ClearIndicators();
            }
            else
            {
                float individualTelegraph = telegraphDuration / numberOfAreas;
                for (int i = 0; i < _calculatedCenters.Count; i++)
                {
                    Vector3 center = _calculatedCenters[i];
                    if (indicatorPrefab != null) InstantiateIndicator(center, individualTelegraph, entityHealth);
                    yield return new WaitForSeconds(individualTelegraph);
                    ApplyAreaDamage(center);
                    ClearIndicators();
                    if (i < _calculatedCenters.Count - 1 && delayBetweenAreas > 0) yield return new WaitForSeconds(delayBetweenAreas);
                }
            }

            if (w < waveCount - 1 && delayBetweenWaves > 0) yield return new WaitForSeconds(delayBetweenWaves);
        }
    }
    private void ApplyAreaDamage(Vector3 center)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(center, areaRadius, _hitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i].TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
    private void ClearIndicators()
    {
        foreach (var ind in _indicators) if (ind != null) Destroy(ind);
        _indicators.Clear();
    }
    public override void CancelAttack() => ClearIndicators();
}