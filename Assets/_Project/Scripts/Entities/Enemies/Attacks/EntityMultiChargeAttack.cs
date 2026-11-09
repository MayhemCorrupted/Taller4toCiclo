using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityMultiChargeAttack", menuName = "Rotbound/Entity/Attack/EntityMultiChargeAttack")]
public class EntityMultiChargeAttack : EntityAttack
{
    public enum MultiChargeStyle { SimultaneousWave, ConsecutiveDashes }
    [Header("MULTI-CHARGE PROPERTIES")]
    public MultiChargeStyle chargeStyle = MultiChargeStyle.ConsecutiveDashes;
    public int numberOfCharges = 3;
    [Min(0.1f)] public float maxChargeDistance = 10f;
    public float hitboxRadius = 1.5f;
    [Tooltip("Si es Consecutive: mira al jugador de frente en cada carga.\nSi es Simultaneous: el enemigo rota hacia el jugador y los ataques orbitan con él.")]
    public bool trackTarget = true;
    [Tooltip("Tiempo de espera entre cada carga si el estilo es ConsecutiveDashes.")]
    [Min(0f)] public float delayBetweenCharges = 0.2f;
    public GameObject indicatorPrefab;
    private readonly List<Vector3> _localDirections = new();
    private Vector3 _consecutiveDir;
    private readonly List<GameObject> _indicators = new();
    private readonly Collider[] _hitBuffer = new Collider[20];
    private readonly HashSet<PlayerHealth> _damagedPlayers = new();
    public override void Prepare(Transform origin, Transform target)
    {
        _localDirections.Clear();
        ClearIndicators();

        origin.TryGetComponent(out Health entityHealth);
        if (target != null) LookAtTarget(origin, target.position);

        if (chargeStyle == MultiChargeStyle.SimultaneousWave)
        {
            float angleStep = 360f / numberOfCharges;

            for (int i = 0; i < numberOfCharges; i++)
            {
                float angle = i * angleStep;
                Vector3 localDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                _localDirections.Add(localDir);

                if (indicatorPrefab != null)
                {
                    Vector3 worldDir = origin.rotation * localDir;
                    //InstantiateIndicator(origin, worldDir);
                }
            }
        }
        else if (chargeStyle == MultiChargeStyle.ConsecutiveDashes)
        {
            _consecutiveDir = origin.forward;
            if (indicatorPrefab != null) InstantiateIndicator(origin, _consecutiveDir);
        }
    }
    private void InstantiateIndicator(Transform origin, Vector3 worldDir)
    {
        GameObject indicatorObj = Instantiate(indicatorPrefab, origin.position, Quaternion.LookRotation(worldDir));
        indicatorObj.transform.localScale = new Vector3(hitboxRadius * 2, 0.1f, maxChargeDistance);
        indicatorObj.transform.position = origin.position + (worldDir * (maxChargeDistance / 2f));

        if (indicatorObj.TryGetComponent(out EntityIndicator indicatorComp))
        {
            indicatorComp.AnimateIndicator(telegraphDuration);
        }
        _indicators.Add(indicatorObj);
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (trackTarget && origin != null && target != null && normalizedTime <= trackTargetUntil)
        {
            LookAtTarget(origin, target.position);
        }

        if (chargeStyle == MultiChargeStyle.SimultaneousWave)
        {
            for (int i = 0; i < _indicators.Count; i++)
            {
                if (_indicators[i] != null)
                {
                    Vector3 worldDir = origin.rotation * _localDirections[i];
                    _indicators[i].transform.SetPositionAndRotation(origin.position + (worldDir * (maxChargeDistance / 2f)), Quaternion.LookRotation(worldDir));
                }
            }
        }
        else if (chargeStyle == MultiChargeStyle.ConsecutiveDashes)
        {
            _consecutiveDir = origin.forward;
            if (_indicators.Count > 0 && _indicators[0] != null)
            {
                _indicators[0].transform.SetPositionAndRotation(origin.position + (_consecutiveDir * (maxChargeDistance / 2f)), Quaternion.LookRotation(_consecutiveDir));
            }
        }
    }
    private void LookAtTarget(Transform origin, Vector3 targetPos)
    {
        Vector3 lookDir = (targetPos - origin.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            origin.rotation = Quaternion.LookRotation(lookDir);
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (chargeStyle == MultiChargeStyle.SimultaneousWave)
        {
            ExecuteSimultaneousWave(origin);
            yield break;
        }
        else if (chargeStyle == MultiChargeStyle.ConsecutiveDashes)
        {
            yield return ConsecutiveDashesRoutine(origin, target);
        }
    }
    private void ExecuteSimultaneousWave(Transform origin)
    {
        for (int i = 0; i < _localDirections.Count; i++)
        {
            Vector3 worldDir = origin.rotation * _localDirections[i];
            Vector3 boxCenter = origin.position + (worldDir * (maxChargeDistance / 2f));
            Vector3 boxHalfExtents = new(hitboxRadius, 2f, maxChargeDistance / 2f);

            int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, _hitBuffer, Quaternion.LookRotation(worldDir));

            for (int j = 0; j < hitCount; j++)
            {
                if (_hitBuffer[j].TryGetComponent(out PlayerHealth playerHealth))
                {
                    playerHealth.TakeDamage(damage);

                    if (_hitBuffer[j].TryGetComponent(out PlayerMovement playerMov))
                    {
                        playerMov.ApplyKnockback(worldDir, knockback);
                    }
                }
            }
        }
        ClearIndicators();
    }
    private IEnumerator ConsecutiveDashesRoutine(Transform origin, Transform target)
    {
        ClearIndicators();
        float individualDashDuration = attackDuration > 0 ? (attackDuration / numberOfCharges) : 0.1f;

        yield return PerformSingleDash(origin, individualDashDuration);

        for (int i = 1; i < numberOfCharges; i++)
        {
            if (delayBetweenCharges > 0) yield return new WaitForSeconds(delayBetweenCharges);

            if (trackTarget && target != null) LookAtTarget(origin, target.position);
            _consecutiveDir = origin.forward;

            if (indicatorPrefab != null) InstantiateIndicator(origin, _consecutiveDir);

            float prepTimer = 0f;
            while (prepTimer < telegraphDuration)
            {
                prepTimer += Time.deltaTime;
                float normalizedTime = prepTimer / telegraphDuration;

                if (trackTarget && target != null && normalizedTime <= trackTargetUntil)
                {
                    LookAtTarget(origin, target.position);
                    _consecutiveDir = origin.forward;

                    if (_indicators.Count > 0 && _indicators[0] != null)
                    {
                        _indicators[0].transform.SetPositionAndRotation(origin.position + (_consecutiveDir * (maxChargeDistance / 2f)), Quaternion.LookRotation(_consecutiveDir));
                    }
                }
                yield return null;
            }

            ClearIndicators();

            yield return PerformSingleDash(origin, individualDashDuration);
        }
    }
    private IEnumerator PerformSingleDash(Transform origin, float duration)
    {
        _damagedPlayers.Clear();
        Vector3 startPos = origin.position;
        Vector3 targetPosition = origin.position + (_consecutiveDir * maxChargeDistance);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            origin.position = Vector3.Lerp(startPos, targetPosition, t);

            int hitCount = Physics.OverlapSphereNonAlloc(origin.position, hitboxRadius, _hitBuffer);
            for (int h = 0; h < hitCount; h++)
            {
                if (_hitBuffer[h].TryGetComponent(out PlayerHealth ph))
                {
                    if (!_damagedPlayers.Contains(ph))
                    {
                        ph.TakeDamage(damage);
                        _damagedPlayers.Add(ph);

                        if (_hitBuffer[h].TryGetComponent(out PlayerMovement playerMov))
                        {
                            playerMov.ApplyKnockback(_consecutiveDir, knockback);
                        }
                    }
                }
            }
            yield return null;
        }
        origin.position = targetPosition;
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