using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityChargeAttack", menuName = "Rotbound/Entity/Attack/EntityChargeAttack")]
public class EntityChargeAttack : EntityAttack
{
    [Header("CHARGE PROPERTIES")]
    [Min(0.1f)] public float maxChargeDistance = 10f;
    public float hitboxRadius = 1.5f;
    [Tooltip("Si es verdadero, el indicador se actualizará siguiendo la rotación frontal del enemigo durante la preparación.")]
    public bool trackTarget = true;
    public GameObject indicatorPrefab;
    private Vector3 _chargeTargetPosition;
    private GameObject _currentIndicator;
    private EntityIndicator _indicatorComp;
    private readonly Collider[] _hitBuffer = new Collider[10];
    public override void Prepare(Transform origin, Transform target)
    {
        if (origin == null) return;

        UpdateTargetPosition(origin);

        if (indicatorPrefab != null)
        {
            Vector3 dir = origin.forward;
            float actualDist = Vector3.Distance(origin.position, _chargeTargetPosition);

            _currentIndicator = Instantiate(indicatorPrefab, origin.position, Quaternion.LookRotation(dir));
            _currentIndicator.transform.localScale = new Vector3(hitboxRadius * 2, 0.1f, actualDist);
            _currentIndicator.transform.position = origin.position + (dir * (actualDist / 2f));

            if (_currentIndicator.TryGetComponent(out _indicatorComp))
            {
                _indicatorComp.AnimateIndicator(telegraphDuration);
            }
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (trackTarget && origin != null)
        {
            UpdateTargetPosition(origin);

            if (_currentIndicator != null)
            {
                Vector3 dir = origin.forward;
                float actualDist = Vector3.Distance(origin.position, _chargeTargetPosition);

                _currentIndicator.transform.rotation = Quaternion.LookRotation(dir);
                _currentIndicator.transform.localScale = new Vector3(hitboxRadius * 2, 0.1f, actualDist);
                _currentIndicator.transform.position = origin.position + (dir * (actualDist / 2f));
            }
        }
    }
    private void UpdateTargetPosition(Transform origin)
    {
        Vector3 dir = origin.forward;
        _chargeTargetPosition = origin.position + (dir * maxChargeDistance);
    }

    public override void Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);

        if (runner != null && attackDuration > 0f)
        {
            runner.StartCoroutine(ChargeMovementRoutine(origin));
        }
        else
        {
            origin.position = _chargeTargetPosition;
        }
    }
    private IEnumerator ChargeMovementRoutine(Transform origin)
    {
        Vector3 startPos = origin.position;
        float timer = 0f;

        HashSet<PlayerHealth> damagedPlayers = new();

        while (timer < attackDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / attackDuration);
            origin.position = Vector3.Lerp(startPos, _chargeTargetPosition, t);

            int hitCount = Physics.OverlapSphereNonAlloc(origin.position, hitboxRadius, _hitBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].TryGetComponent(out PlayerHealth ph))
                {
                    if (!damagedPlayers.Contains(ph))
                    {
                        ph.TakeDamage(damage);
                        damagedPlayers.Add(ph);

                        if (_hitBuffer[i].TryGetComponent(out PlayerMovement playerMov))
                        {
                            Vector3 knockbackDir = origin.forward;
                            playerMov.ApplyKnockback(knockbackDir, knockback);
                        }
                    }
                }
            }
            yield return null;
        }
        origin.position = _chargeTargetPosition;
    }
    public override void CancelAttack()
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);
    }
}