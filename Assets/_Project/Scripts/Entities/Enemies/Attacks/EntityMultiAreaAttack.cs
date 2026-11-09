using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityMultiAreaAttack", menuName = "Rotbound/Entity/Attack/EntityMultiAreaAttack")]
public class EntityMultiAreaAttack : EntityAttack
{
    public enum MultiAreaStyle { Simultaneous, Consecutive }
    public enum AreaOrigin { Attacker, Target }

    [Header("MULTI-AREA PROPERTIES")]
    public MultiAreaStyle executionStyle = MultiAreaStyle.Simultaneous;
    public AreaOrigin areaOrigin = AreaOrigin.Attacker;
    public int numberOfAreas = 3;
    [Min(0.1f)] public float areaRadius = 2f;
    public float scatterRadius = 8f;
    [Tooltip("Tiempo entre explosiones si el estilo es Consecutive.")]
    [Min(0f)] public float delayBetweenAreas = 0.3f;
    public GameObject indicatorPrefab;
    private readonly List<Vector3> _calculatedCenters = new();
    private readonly List<GameObject> _indicators = new();
    private readonly Collider[] _hitBuffer = new Collider[20];
    public override void Prepare(Transform origin, Transform target)
    {
        _calculatedCenters.Clear();
        _indicators.Clear();

        Vector3 basePosition = (areaOrigin == AreaOrigin.Attacker) ? origin.position : target.position;
        basePosition.y = origin.position.y;
        origin.TryGetComponent(out Health _entityHealth);
        for (int i = 0; i < numberOfAreas; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * Random.Range(0, scatterRadius);

            Vector3 center = (i == 0) ? basePosition : basePosition + randomOffset;
            _calculatedCenters.Add(center);

            if (executionStyle == MultiAreaStyle.Simultaneous && indicatorPrefab != null)
            {
                InstantiateIndicator(center, telegraphDuration);
            }
        }
    }
    private void InstantiateIndicator(Vector3 center, float duration)
    {
        GameObject indicatorObj = Instantiate(indicatorPrefab, center, Quaternion.identity);
        indicatorObj.transform.localScale = new Vector3(areaRadius * 2, 0.1f, areaRadius * 2);
        
        if (indicatorObj.TryGetComponent(out EntityIndicator indicatorComp))
        {
            indicatorComp.Init(_entityHealth);
            indicatorComp.AnimateIndicator(duration);
        }
        _indicators.Add(indicatorObj);
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (executionStyle == MultiAreaStyle.Simultaneous)
        {
            ExecuteSimultaneous();
            yield break; 
        }
        else if (executionStyle == MultiAreaStyle.Consecutive)
        {
            yield return ConsecutiveAreasRoutine();
        }
    }
    private void ExecuteSimultaneous()
    {
        foreach (var center in _calculatedCenters)
        {
            ApplyAreaDamage(center);
        }
        ClearIndicators();
    }
    private IEnumerator ConsecutiveAreasRoutine()
    {
        for (int i = 0; i < numberOfAreas; i++)
        {
            Vector3 center = _calculatedCenters[i];

            if (indicatorPrefab != null)
            {
                float individualTelegraph = telegraphDuration / numberOfAreas;
                //InstantiateIndicator(center, individualTelegraph, _entityHealth);
                yield return new WaitForSeconds(individualTelegraph);
            }

            ApplyAreaDamage(center);
            ClearIndicators();

            if (i < numberOfAreas - 1)
            {
                yield return new WaitForSeconds(delayBetweenAreas);
            }
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
        foreach (var ind in _indicators)
        {
            if (ind != null) Destroy(ind);
        }
        _indicators.Clear();
    }
    public override void CancelAttack()
    {
        ClearIndicators();
    }
}