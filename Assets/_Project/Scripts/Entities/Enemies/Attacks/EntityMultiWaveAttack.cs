using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityMultiWaveAttack", menuName = "Rotbound/Entity/Attack/EntityMultiWaveAttack")]
public class EntityMultiWaveAttack : EntityAttack
{
    public enum MultiWaveStyle { SimultaneousWave, ConsecutiveWaves }
    public enum WavePattern { RadialWave, FanAbanico, CrossStar, ConsecutiveLineal, Random }

    [Header("MULTI-WAVE PROPERTIES")]
    public MultiWaveStyle waveStyle = MultiWaveStyle.ConsecutiveWaves;
    public WavePattern pattern = WavePattern.RadialWave;
    public int numberOfWaves = 3;
    [Min(0.1f)] public float waveDistance = 10f;
    public float hitboxRadius = 1.5f;
    public bool trackTarget = true;
    [Min(0f)] public float delayBetweenWaves = 0.2f;

    [Header("OLEADAS DE RÁFAGA (WAVE REPEATS)")]
    [Min(1)] public int waveCount = 1;
    [Min(0f)] public float delayBetweenWaveRepeats = 0.5f;

    public GameObject indicatorPrefab;

    private readonly List<Vector3> _localDirections = new();
    private readonly List<GameObject> _indicators = new();
    private readonly Collider[] _hitBuffer = new Collider[20];
    public override void Prepare(Transform origin, Transform target)
    {
        _localDirections.Clear();
        ClearIndicators();

        if (target != null && trackTarget) LookAtTarget(origin, target.position);
        origin.TryGetComponent(out Health entityHealth);

        GenerateDirections();

        if (waveStyle == MultiWaveStyle.SimultaneousWave && indicatorPrefab != null)
        {
            foreach (var localDir in _localDirections)
            {
                Vector3 worldDir = origin.rotation * localDir;
                InstantiateIndicator(origin, worldDir, entityHealth);
            }
        }
        else if (waveStyle == MultiWaveStyle.ConsecutiveWaves && indicatorPrefab != null && _localDirections.Count > 0)
        {
            InstantiateIndicator(origin, origin.rotation * _localDirections[0], entityHealth);
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (trackTarget && origin != null && target != null && normalizedTime <= trackTargetUntil)
        {
            LookAtTarget(origin, target.position);

            if (waveStyle == MultiWaveStyle.SimultaneousWave)
            {
                for (int i = 0; i < _indicators.Count && i < _localDirections.Count; i++)
                {
                    if (_indicators[i] != null)
                    {
                        Vector3 worldDir = origin.rotation * _localDirections[i];
                        _indicators[i].transform.SetPositionAndRotation(
                            origin.position + (worldDir * (waveDistance / 2f)),
                            Quaternion.LookRotation(worldDir)
                        );
                        _indicators[i].transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, waveDistance);
                    }
                }
            }
            else if (waveStyle == MultiWaveStyle.ConsecutiveWaves)
            {
                if (_indicators.Count > 0 && _indicators[0] != null && _localDirections.Count > 0)
                {
                    Vector3 worldDir = origin.rotation * _localDirections[0];
                    _indicators[0].transform.SetPositionAndRotation(
                        origin.position + (worldDir * (waveDistance / 2f)),
                        Quaternion.LookRotation(worldDir)
                    );
                    _indicators[0].transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, waveDistance);
                }
            }
        }
    }
    private void GenerateDirections()
    {
        switch (pattern)
        {
            case WavePattern.FanAbanico:
                float totalAngle = 90f;
                float startAngle = -totalAngle / 2f;
                float step = totalAngle / Mathf.Max(numberOfWaves - 1, 1);
                for (int i = 0; i < numberOfWaves; i++)
                {
                    _localDirections.Add(Quaternion.Euler(0, startAngle + (i * step), 0) * Vector3.forward);
                }
                break;

            case WavePattern.CrossStar:
                int count = Mathf.Max(numberOfWaves, 4);
                float angleCross = 360f / count;
                for (int i = 0; i < count; i++)
                {
                    _localDirections.Add(Quaternion.Euler(0, i * angleCross, 0) * Vector3.forward);
                }
                break;

            case WavePattern.Random:
                for (int i = 0; i < numberOfWaves; i++)
                {
                    _localDirections.Add(Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward);
                }
                break;

            default:
                float angleRadial = 360f / numberOfWaves;
                for (int i = 0; i < numberOfWaves; i++)
                {
                    _localDirections.Add(Quaternion.Euler(0, i * angleRadial, 0) * Vector3.forward);
                }
                break;
        }
    }
    private void InstantiateIndicator(Transform origin, Vector3 worldDir, Health entityHealth)
    {
        GameObject indicatorObj = Instantiate(indicatorPrefab, origin.position, Quaternion.LookRotation(worldDir));
        indicatorObj.transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, waveDistance);
        indicatorObj.transform.position = origin.position + (worldDir * (waveDistance / 2f));

        if (indicatorObj.TryGetComponent(out EntityIndicator indicatorComp))
        {
            indicatorComp.Init(entityHealth);
            indicatorComp.AnimateIndicator(telegraphDuration);
        }
        _indicators.Add(indicatorObj);
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        for (int w = 0; w < waveCount; w++)
        {
            if (w > 0)
            {
                Prepare(origin, target);
                yield return new WaitForSeconds(telegraphDuration);
            }

            if (waveStyle == MultiWaveStyle.SimultaneousWave)
            {
                ExecuteSimultaneousWave(origin);
            }
            else
            {
                yield return ConsecutiveWavesRoutine(origin, target);
            }

            if (w < waveCount - 1 && delayBetweenWaveRepeats > 0) yield return new WaitForSeconds(delayBetweenWaveRepeats);
        }
    }
    private void ExecuteSimultaneousWave(Transform origin)
    {
        foreach (var localDir in _localDirections)
        {
            Vector3 worldDir = origin.rotation * localDir;
            Vector3 boxCenter = origin.position + (worldDir * (waveDistance / 2f));
            Vector3 boxHalfExtents = new(hitboxRadius, 2f, waveDistance / 2f);

            int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, _hitBuffer, Quaternion.LookRotation(worldDir));
            for (int j = 0; j < hitCount; j++)
            {
                if (_hitBuffer[j].TryGetComponent(out PlayerHealth playerHealth))
                {
                    playerHealth.TakeDamage(damage);
                    if (_hitBuffer[j].TryGetComponent(out PlayerMovement playerMov)) playerMov.ApplyKnockback(worldDir, knockback);
                }
            }
        }
        ClearIndicators();
    }
    private IEnumerator ConsecutiveWavesRoutine(Transform origin, Transform target)
    {
        ClearIndicators();

        for (int i = 0; i < _localDirections.Count; i++)
        {
            if (i > 0 && delayBetweenWaves > 0) yield return new WaitForSeconds(delayBetweenWaves);
            if (trackTarget && target != null) LookAtTarget(origin, target.position);

            Vector3 waveDir = origin.rotation * _localDirections[i];
            Vector3 boxCenter = origin.position + (waveDir * (waveDistance / 2f));
            Vector3 boxHalfExtents = new(hitboxRadius, 2f, waveDistance / 2f);

            int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, _hitBuffer, Quaternion.LookRotation(waveDir));
            for (int j = 0; j < hitCount; j++)
            {
                if (_hitBuffer[j].TryGetComponent(out PlayerHealth playerHealth))
                {
                    playerHealth.TakeDamage(damage);
                    if (_hitBuffer[j].TryGetComponent(out PlayerMovement playerMov)) playerMov.ApplyKnockback(waveDir, knockback);
                }
            }

            yield return null;
        }
    }
    private void LookAtTarget(Transform origin, Vector3 targetPos)
    {
        Vector3 lookDir = (targetPos - origin.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) origin.rotation = Quaternion.LookRotation(lookDir);
    }
    private void ClearIndicators()
    {
        foreach (var ind in _indicators) if (ind != null) Destroy(ind);
        _indicators.Clear();
    }
    public override void CancelAttack() => ClearIndicators();
}