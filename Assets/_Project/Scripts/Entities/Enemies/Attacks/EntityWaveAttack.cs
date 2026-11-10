using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityWaveAttack", menuName = "Rotbound/Entity/Attack/EntityWaveAttack")]
public class EntityWaveAttack : EntityAttack
{
    [Header("WAVE PROPERTIES")]
    [Min(0.1f)] public float waveDistance = 10f;
    public float hitboxRadius = 1.5f;
    public bool trackTarget = true;
    public GameObject indicatorPrefab;

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

        origin.TryGetComponent(out Health entityHealth);

        if (indicatorPrefab != null)
        {
            Vector3 dir = origin.forward;
            _currentIndicator = Instantiate(indicatorPrefab, origin.position, Quaternion.LookRotation(dir));
            _currentIndicator.transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, waveDistance);
            _currentIndicator.transform.position = origin.position + (dir * (waveDistance / 2f));

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

            if (_currentIndicator != null)
            {
                Vector3 dir = origin.forward;
                _currentIndicator.transform.SetPositionAndRotation(origin.position + (dir * (waveDistance / 2f)), Quaternion.LookRotation(dir));
                _currentIndicator.transform.localScale = new Vector3(hitboxRadius * 2f, 0.1f, waveDistance);
            }
        }
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);

        Vector3 dir = origin.forward;
        Vector3 boxCenter = origin.position + (dir * (waveDistance / 2f));
        Vector3 boxHalfExtents = new(hitboxRadius, 2f, waveDistance / 2f);

        int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, _hitBuffer, Quaternion.LookRotation(dir));
        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i].TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
                if (_hitBuffer[i].TryGetComponent(out PlayerMovement playerMov))
                {
                    playerMov.ApplyKnockback(dir, knockback);
                }
            }
        }

        yield return null;
    }
    private void LookAtTarget(Transform origin, Vector3 targetPos)
    {
        Vector3 lookDir = (targetPos - origin.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) origin.rotation = Quaternion.LookRotation(lookDir);
    }
    public override void CancelAttack()
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);
    }
}