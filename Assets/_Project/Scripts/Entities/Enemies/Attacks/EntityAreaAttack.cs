using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "EntityAreaAttack", menuName = "Rotbound/Entity/Attack/EntityAreaAttack")]
public class EntityAreaAttack : EntityAttack
{
    public enum AreaOrigin { Attacker, Target }
    [Header("AREA PROPERTIES")]
    public AreaOrigin areaOrigin = AreaOrigin.Attacker;
    [Min(0.1f)] public float radius = 3f;
    [Tooltip("Si es verdadero, el centro del área seguirá al jugador o la posición del enemigo durante el telegrafiado.")]
    public bool trackTarget = true;
    public GameObject indicatorPrefab;
    private Vector3 _calculatedCenter;
    private GameObject _currentIndicator;
    private readonly Collider[] _hitBuffer = new Collider[10];
    public override void Prepare(Transform origin, Transform target)
    {
        UpdateCalculatedCenter(origin, target);
        origin.TryGetComponent(out Health entityHealth);

        if (indicatorPrefab != null)
        {
            _currentIndicator = Instantiate(indicatorPrefab, _calculatedCenter, Quaternion.identity);
            _currentIndicator.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);

            if (_currentIndicator.TryGetComponent(out EntityIndicator indicator))
            {
                indicator.Init(entityHealth);
                indicator.AnimateIndicator(telegraphDuration);
            }
        }
    }
    public override void UpdatePreparation(Transform origin, Transform target, float normalizedTime)
    {
        if (trackTarget && origin != null && normalizedTime <= trackTargetUntil)
        {
            UpdateCalculatedCenter(origin, target);

            if (_currentIndicator != null)
            {
                _currentIndicator.transform.position = _calculatedCenter;
                _currentIndicator.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
            }
        }
    }

    private void UpdateCalculatedCenter(Transform origin, Transform target)
    {
        _calculatedCenter = (areaOrigin == AreaOrigin.Attacker || target == null) ? origin.position : target.position;
        _calculatedCenter.y = origin.position.y;
    }
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(_calculatedCenter, radius, _hitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i].TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
            }
        }

        if (_currentIndicator != null) Destroy(_currentIndicator);
        yield return null;
    }
    public override void CancelAttack()
    {
        if (_currentIndicator != null) Destroy(_currentIndicator);
    }
}