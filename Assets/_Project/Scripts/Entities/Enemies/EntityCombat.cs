using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(EntityBrain))]
[RequireComponent(typeof(Health))]
public class EntityCombat : MonoBehaviour
{
    private EntityBrain _brain;
    [SerializeField] private Animator _animator;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private float _checkInterval = 0.1f;
    private float _detectionRadius, _attackRadius, _loseTargetRadius;
    private List<EntityAttack> _attacks = new();
    private Transform _target;
    private float _atkCountdown = 0.0f;
    private readonly Collider[] _hitBuffer = new Collider[5];
    private WaitForSeconds _rangeEvalInterval;
    private bool _isPerformingAttack = false;
    private EntityAttack _currentAttackLogic;
    private Health _health;
    public bool CanSeeTarget => _target != null;
    public bool IsInAttackRange { get; private set; }
    public Transform TargetTransform => _target;
    public Vector3 TargetLocation => _target ? _target.position : Vector3.positiveInfinity;
    private void Awake()
    {
        _brain = GetComponent<EntityBrain>();
        _health = GetComponent<Health>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
    }
    private void OnEnable()
    {
        if (_brain != null) _brain.OnStateChanged += HandleBrainStateChanged;
    }
    private void OnDisable()
    {
        if (_brain != null) _brain.OnStateChanged -= HandleBrainStateChanged;
    }
    public void SetValues(float detRadius, float atkRadius, float losRadius, List<EntityAttack> attacks)
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

        if (_brain != null && _brain.CurrentState == EnemyState.Attacking && !_isPerformingAttack)
        {
            if (_atkCountdown <= 0.0f)
            {
                StartCoroutine(PerformAttackRoutine());
            }
            else
            {
                _atkCountdown = Mathf.Max(_atkCountdown - Time.deltaTime, 0.0f);
            }
        }
    }
    private IEnumerator PerformAttackRoutine()
    {
        _isPerformingAttack = true;

        int rndIdx = Random.Range(0, _attacks.Count);
        _currentAttackLogic = _attacks[rndIdx];

        if (_currentAttackLogic == null)
        {
            _atkCountdown = 1.0f;
            _isPerformingAttack = false;
            yield break;
        }

        if (_health != null)
        {
            if (_currentAttackLogic.armorState == ArmorType.Hyperarmor)
                _health.SetDamageMultiplier(_currentAttackLogic.hyperarmorDamageMultiplier);
            else if (_currentAttackLogic.armorState == ArmorType.Superarmor)
                _health.SetDamageMultiplier(1.0f);
        }

        if (_animator != null && !string.IsNullOrEmpty(_currentAttackLogic.animationTrigger))
        {
            _animator.SetTrigger(_currentAttackLogic.animationTrigger);
        }

        _currentAttackLogic.Prepare(transform, _target);

        float prepTimer = 0f;
        while (prepTimer < _currentAttackLogic.telegraphDuration)
        {
            prepTimer += Time.deltaTime;
            _currentAttackLogic.UpdatePreparation(transform, _target, prepTimer / _currentAttackLogic.telegraphDuration);
            yield return null;
        }

        if (_target != null)
        {
            _currentAttackLogic.Execute(transform, _target, this);
        }

        if (_currentAttackLogic.attackDuration > 0)
        {
            yield return new WaitForSeconds(_currentAttackLogic.attackDuration);
        }

        if (_currentAttackLogic.armorState != ArmorType.None && _health != null)
        {
            _health.SetDamageMultiplier(1.0f);
        }

        _atkCountdown = _currentAttackLogic.cooldown;
        _isPerformingAttack = false;
    }
    private void HandleBrainStateChanged(EnemyState newState)
    {
        if (newState == EnemyState.Hurt)
        {
            if (_isPerformingAttack && _currentAttackLogic != null)
            {
                if (_currentAttackLogic.armorState != ArmorType.None)
                {
                    _brain.ChangeState(EnemyState.Attacking);
                    return;
                }
                else
                {
                    _currentAttackLogic.CancelAttack();
                }
            }

            _isPerformingAttack = false;
            StopAllCoroutines();
            StartCoroutine(DetectionRoutine());

            _atkCountdown = _attacks.Count > 0 ? _attacks[0].cooldown : 1.0f;
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