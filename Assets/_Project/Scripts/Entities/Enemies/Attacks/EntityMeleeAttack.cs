using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityMeleeAttack", menuName = "Rotbound/Entity/Attack/EntityMeleeAttack")]
public class EntityMeleeAttack : EntityAttack
{
    [Header("MELEE PROPERTIES")]
    public bool isRadial;
    public float radius = 2f;
    public Vector3 hitboxMeasurements = Vector3.one;
    public float verticalOffset = 0.0f;
    public GameObject indicatorPrefab;
    private GameObject _currentIndicator;
    private readonly Collider[] _hitBuffer = new Collider[10];
    public override void Prepare(Transform origin, Transform target)
    {
        if (indicatorPrefab == null || origin == null) return;

        origin.TryGetComponent(out Health entityHealth);
        Vector3 spawnPos = origin.position + (Vector3.up * verticalOffset);

        if (isRadial)
        {
            _currentIndicator = Instantiate(indicatorPrefab, spawnPos, Quaternion.identity);
            _currentIndicator.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
        }
        else
        {
            Vector3 boxCenter = spawnPos + (origin.forward * (hitboxMeasurements.z * 0.5f));
            _currentIndicator = Instantiate(indicatorPrefab, boxCenter, Quaternion.LookRotation(origin.forward));
            _currentIndicator.transform.localScale = hitboxMeasurements;
        }

        if (_currentIndicator.TryGetComponent(out EntityIndicator indicatorComp))
        {
            indicatorComp.Init(entityHealth);
            indicatorComp.AnimateIndicator(telegraphDuration);
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (_currentIndicator != null && origin != null && !isRadial)
        {
            Vector3 spawnPos = origin.position + (Vector3.up * verticalOffset);
            Vector3 boxCenter = spawnPos + (origin.forward * (hitboxMeasurements.z * 0.5f));
            _currentIndicator.transform.SetPositionAndRotation(boxCenter, Quaternion.LookRotation(origin.forward));
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);

        Vector3 originPos = origin.position + (Vector3.up * verticalOffset);
        int hitCount;

        if (isRadial)
        {
            hitCount = Physics.OverlapSphereNonAlloc(originPos, radius, _hitBuffer);
        }
        else
        {
            Vector3 boxCenter = originPos + (origin.forward * (hitboxMeasurements.z * 0.5f));
            hitCount = Physics.OverlapBoxNonAlloc(boxCenter, hitboxMeasurements * 0.5f, _hitBuffer, origin.rotation);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hitBuffer[i];
            if (hit.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
                if (hit.TryGetComponent(out PlayerMovement playerMov))
                {
                    Vector3 knockbackDir = (hit.transform.position - origin.position).normalized;
                    playerMov.ApplyKnockback(knockbackDir, knockback);
                }
            }
        }
        yield return null;
    }
    public override void CancelAttack()
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);
    }
}