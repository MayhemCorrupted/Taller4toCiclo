using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityMultiChargeAttack", menuName = "Rotbound/Entity/Attack/EntityMultiChargeAttack")]
public class EntityMultiChargeAttack : EntityAttack
{
    [Header("MULTI-CHARGE PROPERTIES")]
    [Tooltip("Cantidad de embestidas consecutivas en línea recta hacia el jugador.")]
    public int numberOfCharges = 3;
    [Min(0.1f)] public float maxChargeDistance = 10f;
    public float hitboxRadius = 1.5f;
    [Tooltip("Si es verdadero, el enemigo rotará hacia el jugador antes de iniciar cada embestida.")]
    public bool trackTarget = true;
    [Min(0f)] public float delayBetweenCharges = 0.2f;

    [Header("OLEADAS DE CARGA (WAVES)")]
    [Min(1)] public int waveCount = 1;
    [Min(0f)] public float delayBetweenWaves = 0.5f;

    public GameObject indicatorPrefab;

    private readonly List<GameObject> _indicators = new();
    private readonly Collider[] _hitBuffer = new Collider[20];
    private readonly HashSet<PlayerHealth> _damagedPlayers = new();
    public override void Prepare(Transform origin, Transform target)
    {
        ClearIndicators();

        if (target != null && trackTarget) LookAtTarget(origin, target.position);
        origin.TryGetComponent(out Health entityHealth);

        if (indicatorPrefab != null)
        {
            InstantiateIndicator(origin, origin.forward, entityHealth);
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (trackTarget && origin != null && target != null && normalizedTime <= trackTargetUntil)
        {
            LookAtTarget(origin, target.position);

            if (_indicators.Count > 0 && _indicators[0] != null)
            {
                Vector3 worldDir = origin.forward;
                _indicators[0].transform.SetPositionAndRotation(
                    origin.position + (worldDir * (maxChargeDistance / 2f)),
                    Quaternion.LookRotation(worldDir)
                );
            }
        }
    }
    private void InstantiateIndicator(Transform origin, Vector3 worldDir, Health entityHealth)
    {
        GameObject indicatorObj = Instantiate(indicatorPrefab, origin.position, Quaternion.LookRotation(worldDir));
        indicatorObj.transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, maxChargeDistance);
        indicatorObj.transform.position = origin.position + (worldDir * (maxChargeDistance / 2f));

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

            yield return ConsecutiveDashesRoutine(origin, target);

            if (w < waveCount - 1 && delayBetweenWaves > 0) yield return new WaitForSeconds(delayBetweenWaves);
        }
    }
    private IEnumerator ConsecutiveDashesRoutine(Transform origin, Transform target)
    {
        ClearIndicators();
        float dashDuration = attackDuration > 0 ? (attackDuration / numberOfCharges) : 0.1f;
        CharacterController cc = origin.GetComponent<CharacterController>();

        for (int i = 0; i < numberOfCharges; i++)
        {
            if (i > 0)
            {
                if (delayBetweenCharges > 0) yield return new WaitForSeconds(delayBetweenCharges);
                if (trackTarget && target != null) LookAtTarget(origin, target.position);
            }

            Vector3 chargeDir = origin.forward;
            if (cc != null) cc.detectCollisions = false;

            _damagedPlayers.Clear();
            Vector3 startPos = origin.position;
            Vector3 targetPos = origin.position + (chargeDir * maxChargeDistance);
            float timer = 0f;

            while (timer < dashDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / dashDuration);
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

                if (cc != null) cc.Move(currentPos - origin.position);
                else origin.position = currentPos;

                int hitCount = Physics.OverlapSphereNonAlloc(origin.position, hitboxRadius, _hitBuffer);
                for (int h = 0; h < hitCount; h++)
                {
                    if (_hitBuffer[h].TryGetComponent(out PlayerHealth ph) && !_damagedPlayers.Contains(ph))
                    {
                        ph.TakeDamage(damage);
                        _damagedPlayers.Add(ph);
                        if (_hitBuffer[h].TryGetComponent(out PlayerMovement playerMov)) playerMov.ApplyKnockback(chargeDir, knockback);
                    }
                }
                yield return null;
            }

            if (cc != null) cc.detectCollisions = true;
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