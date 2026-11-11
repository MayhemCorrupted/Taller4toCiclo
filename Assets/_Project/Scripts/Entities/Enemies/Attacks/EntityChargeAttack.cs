using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityChargeAttack", menuName = "Rotbound/Entity/Attack/EntityChargeAttack")]
public class EntityChargeAttack : EntityAttack
{
    [Header("CHARGE PROPERTIES")]
    [Min(0.1f)] public float maxChargeDistance = 10f;
    public float hitboxRadius = 1.5f;
    [Tooltip("Si es verdadero, el enemigo rotará hacia el jugador y el indicador actualizará su posición/orientación durante el telegrafiado.")]
    public bool trackTarget = true;
    public GameObject indicatorPrefab;
    private Vector3 _chargeTargetPosition;
    private GameObject _currentIndicator;
    private EntityIndicator _indicatorComp;
    private readonly Collider[] _hitBuffer = new Collider[10];
    public override void Prepare(Transform origin, Transform target)
    {
        if (origin == null) return;

        if (trackTarget && target != null)
        {
            LookAtTarget(origin, target.position);
        }

        UpdateTargetPosition(origin);
        origin.TryGetComponent(out Health entityHealth);
        if (indicatorPrefab != null)
        {
            Vector3 dir = origin.forward;
            float actualDist = Vector3.Distance(origin.position, _chargeTargetPosition);

            _currentIndicator = Instantiate(indicatorPrefab, origin.position, Quaternion.LookRotation(dir));
            _currentIndicator.transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, actualDist);
            _currentIndicator.transform.position = origin.position + (dir * (actualDist / 2f));

            if (_currentIndicator.TryGetComponent(out _indicatorComp))
            {
                _indicatorComp.Init(entityHealth);
                _indicatorComp.AnimateIndicator(telegraphDuration);
            }
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (trackTarget && origin != null && target != null && normalizedTime <= trackTargetUntil)
        {
            LookAtTarget(origin, target.position);
            UpdateTargetPosition(origin);

            if (_currentIndicator != null)
            {
                Vector3 dir = origin.forward;
                float actualDist = Vector3.Distance(origin.position, _chargeTargetPosition);

                _currentIndicator.transform.rotation = Quaternion.LookRotation(dir);
                _currentIndicator.transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, actualDist);
                _currentIndicator.transform.position = origin.position + (dir * (actualDist / 2f));
            }
        }
    }
    private void UpdateTargetPosition(Transform origin)
    {
        _chargeTargetPosition = origin.position + (origin.forward * maxChargeDistance);
    }
    private void LookAtTarget(Transform origin, Vector3 targetPos)
    {
        Vector3 lookDir = (targetPos - origin.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) origin.rotation = Quaternion.LookRotation(lookDir);
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);

        if (runner != null && attackDuration > 0f)
        {
            yield return runner.StartCoroutine(ChargeMovementRoutine(origin));
        }
        else
        {
            origin.position = _chargeTargetPosition;
            yield return null;
        }
    }
    private IEnumerator ChargeMovementRoutine(Transform origin)
    {
        Vector3 startPos = origin.position;
        float timer = 0f;
        HashSet<PlayerHealth> damagedPlayers = new();

        if (origin.TryGetComponent<Collider>(out var mainCollider)) mainCollider.isTrigger = true;

        while (timer < attackDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / attackDuration);

            origin.position = Vector3.Lerp(startPos, _chargeTargetPosition, t);

            int hitCount = Physics.OverlapSphereNonAlloc(origin.position, hitboxRadius, _hitBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].TryGetComponent(out PlayerHealth ph) && !damagedPlayers.Contains(ph))
                {
                    ph.TakeDamage(damage);
                    damagedPlayers.Add(ph);

                    if (_hitBuffer[i].TryGetComponent(out PlayerMovement playerMov))
                    {
                        playerMov.ApplyKnockback(origin.forward, knockback);
                    }
                }
            }
            yield return null;
        }

        if (mainCollider != null) mainCollider.isTrigger = false;
    }
    public override void CancelAttack()
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);
    }
}