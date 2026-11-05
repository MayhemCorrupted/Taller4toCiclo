using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(EnemyBrain))]
public class EnemyCombat : MonoBehaviour
{
    private EnemyBrain _brain;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private float _checkInterval = 0.1f;

    private float _detectionRadius, _attackRadius, _loseTargetRadius;
    private List<EnemyAttack> _attacks = new();
    private Transform _target;
    private float _atkCountdown = 0.0f;
    private readonly Collider[] _hitBuffer = new Collider[5];
    private WaitForSeconds _rangeEvalInterval;
    public bool CanSeeTarget => _target != null;
    public bool IsInAttackRange { get; private set; }
    public Transform TargetTransform => _target;
    public Vector3 TargetLocation => _target ? _target.position : Vector3.positiveInfinity;
    private void Awake()
    {
        _brain = GetComponent<EnemyBrain>();
    }
    public void SetValues(float detRadius, float atkRadius, float losRadius, List<EnemyAttack> attacks)
    {
        _detectionRadius = detRadius;
        _attackRadius = atkRadius;
        _loseTargetRadius = losRadius;
        _attacks = attacks;

        _rangeEvalInterval = new WaitForSeconds(_checkInterval);
        StartCoroutine(DetectionRoutine());
    }
    private void Update()
    {
        if (_attacks.Count == 0 || _target == null) return;

        bool canAttackNow = (_brain == null && IsInAttackRange) ||
                            (_brain != null && _brain.CurrentState == EnemyState.Attacking);

        if (canAttackNow)
        {
            if (_atkCountdown <= 0.0f)
            {
                int rndIdx = Random.Range(0, _attacks.Count);
                _attacks[rndIdx].Execute(transform, _target);
                _atkCountdown = _attacks[rndIdx].cooldown;
            }
            else
            {
                _atkCountdown = Mathf.Max(_atkCountdown - Time.deltaTime, 0.0f);
            }
        }
    }
    private IEnumerator DetectionRoutine()
    {
        while (true)
        {
            yield return _rangeEvalInterval;

            if (_target == null)
            {
                int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, _hitBuffer, _targetLayer);
                if (hitCount > 0 && HasClearLineOfSight(_hitBuffer[0].transform, _detectionRadius))
                {
                    _target = _hitBuffer[0].transform;
                    IsInAttackRange = Vector3.Distance(transform.position, _target.position) <= _attackRadius;
                }
            }
            else
            {
                float distance = Vector3.Distance(transform.position, _target.position);
                if (distance > _loseTargetRadius || !HasClearLineOfSight(_target, distance))
                {
                    _target = null;
                    IsInAttackRange = false;
                }
                else
                {
                    IsInAttackRange = distance <= _attackRadius;
                }
            }
        }
    }
    private bool HasClearLineOfSight(Transform targetToCast, float maxDistance)
    {
        Vector3 direction = (targetToCast.position - transform.position).normalized;
        return !Physics.Raycast(transform.position, direction, maxDistance, _wallLayer);
    }
}