using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EntityBrain))]
[RequireComponent(typeof(Health))]
public class EntityCombat : MonoBehaviour
{
    public static event Action<EntityCombat> OnAnyEnemyTargetDetected;
    public static event Action<EntityCombat> OnAnyEnemyTargetLost;

    private EntityBrain _brain;
    private Health _health;
    [SerializeField] private Animator _animator;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private float _checkInterval = 0.1f;

    private EntityData _entityData;
    private EnemyPhase _currentPhase;
    private int _currentPhaseIndex = -1;

    private Transform _target;
    private Health _targetHealth;
    private float _atkCountdown = 0.0f;
    private readonly Collider[] _hitBuffer = new Collider[5];
    private WaitForSeconds _rangeEvalInterval;
    private Coroutine _detectionCoroutine;
    private Coroutine _attackCoroutine;
    private bool _isPerformingAttack = false;
    private EntityAttack _currentAttackLogic;

    private int _activeAttackAnimationCount = 0;
    private bool _attackAnimationStateEntered = false;

    private Vector3 _previousTargetPos;
    public Vector3 TargetVelocity { get; private set; }
    public EntityData Data => _entityData;
    public EnemyRank Rank => _entityData != null ? _entityData.rank : EnemyRank.Minion;
    public int PreferredAttackDistance => _entityData != null ? _entityData.preferredAttackDistance : 0;
    public EnemyPhase CurrentPhase => _currentPhase;
    public bool IsPerformingAttack => _isPerformingAttack;
    public bool IsAttackAnimationPlaying => _activeAttackAnimationCount > 0;
    public Transform TargetTransform => IsTargetAlive() ? _target : null;
    public Vector3 TargetLocation => IsTargetAlive() ? _target.position : Vector3.positiveInfinity;
    public bool CanSeeTarget => IsTargetAlive();
    public bool IsInAttackRange
    {
        get
        {
            if (_brain != null && (_brain.CurrentState == EnemyState.Agonizing || _brain.CurrentState == EnemyState.Death))
                return false;

            if (!IsTargetAlive() || _entityData == null) return false;

            if (EntityDirector.Instance != null && EntityDirector.Instance.GetHighestRankPresent() >= EnemyRank.MiniBoss && Rank == EnemyRank.Minion)
            {
                if (!EntityDirector.Instance.RequestAttackPermission(this)) return false;
            }

            float dist = Vector3.Distance(transform.position, _target.position);
            _entityData.GetPreferredRangeBand(_currentPhase, out float minRange, out _, out float maxRange);

            if (dist > maxRange) return false;

            if (PreferredAttackDistance >= 2 && dist < minRange * 0.85f)
            {
                Vector3 awayFromTarget = (transform.position - _target.position).normalized;
                awayFromTarget.y = 0f;
                bool isCorneredAgainstWall = Physics.Raycast(transform.position + Vector3.up * 0.5f, awayFromTarget, 1.5f, _wallLayer);
                if (!isCorneredAgainstWall) return false;
            }

            return true;
        }
    }
    public bool IsArmorActive
    {
        get
        {
            if (!_isPerformingAttack) return false;
            ArmorType currentArmor = (_currentAttackLogic != null && _currentAttackLogic.overrideBaseArmor)
                ? _currentAttackLogic.armorState
                : (_entityData != null ? _entityData.defaultArmorState : ArmorType.None);
            return currentArmor != ArmorType.None;
        }
    }
    private void Awake()
    {
        EnsureComponentsInitialized();
    }
    private void EnsureComponentsInitialized()
    {
        if (_brain == null) _brain = GetComponent<EntityBrain>();
        if (_health == null) _health = GetComponent<Health>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
    }
    private void OnEnable()
    {
        EnsureComponentsInitialized();
        if (_brain != null) _brain.OnStateChanged += HandleBrainStateChanged;
        if (_health != null)
        {
            _health.OnTakeDamage += EvaluatePhaseChange;
            _health.OnDeath += HandleSelfDeath;
        }
        if (EntityDirector.Instance != null) EntityDirector.Instance.RegisterEnemy(this);

        if (_entityData != null && _detectionCoroutine == null)
        {
            _rangeEvalInterval = new WaitForSeconds(_checkInterval);
            _detectionCoroutine = StartCoroutine(DetectionRoutine());
        }
    }
    private void OnDisable()
    {
        if (_brain != null) _brain.OnStateChanged -= HandleBrainStateChanged;
        if (_health != null)
        {
            _health.OnTakeDamage -= EvaluatePhaseChange;
            _health.OnDeath -= HandleSelfDeath;
        }
        if (EntityDirector.Instance != null) EntityDirector.Instance.UnregisterEnemy(this);

        AbortCurrentAttack();
        ClearTarget();
        _detectionCoroutine = null;
    }
    private void Start()
    {
        if (EntityDirector.Instance != null) EntityDirector.Instance.RegisterEnemy(this);
    }
    public void SetData(EntityData data)
    {
        _entityData = data;
        EnsureComponentsInitialized();
        EvaluatePhaseChange(0);
        ApplyCurrentArmorState();
        _rangeEvalInterval = new WaitForSeconds(_checkInterval);

        if (gameObject.activeInHierarchy)
        {
            if (_detectionCoroutine != null) StopCoroutine(_detectionCoroutine);
            _detectionCoroutine = StartCoroutine(DetectionRoutine());
        }
    }
    public void NotifyAttackAnimationStarted()
    {
        if (_brain != null && (_brain.CurrentState == EnemyState.Death || _brain.CurrentState == EnemyState.Agonizing))
            return;

        _activeAttackAnimationCount++;
        _attackAnimationStateEntered = true;
    }
    public void NotifyAttackAnimationEnded()
    {
        _activeAttackAnimationCount = Mathf.Max(0, _activeAttackAnimationCount - 1);
    }
    private bool IsTargetAlive()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
            return false;

        if (_targetHealth != null && _targetHealth.CurrentHealth <= 0f)
            return false;

        return true;
    }
    private void SetTarget(Transform newTarget, Health targetHealth)
    {
        if (_target == newTarget) return;

        _target = newTarget;
        _targetHealth = targetHealth;
        _previousTargetPos = _target.position;
        OnAnyEnemyTargetDetected?.Invoke(this);
    }
    private void ClearTarget()
    {
        if (_target == null && _targetHealth == null) return;

        _target = null;
        _targetHealth = null;
        TargetVelocity = Vector3.zero;
        OnAnyEnemyTargetLost?.Invoke(this);
    }
    private void AbortCurrentAttack()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        if (_currentAttackLogic != null)
        {
            if (_animator != null && !string.IsNullOrEmpty(_currentAttackLogic.animationTrigger))
                _animator.ResetTrigger(_currentAttackLogic.animationTrigger);

            _currentAttackLogic.CancelAttack();
            _currentAttackLogic = null;
        }

        _activeAttackAnimationCount = 0;
        _attackAnimationStateEntered = false;

        if (_isPerformingAttack)
        {
            _isPerformingAttack = false;
            ApplyCurrentArmorState();
            if (EntityDirector.Instance != null) EntityDirector.Instance.NotifyAttackFinished(this);
        }
    }
    private void HandleSelfDeath()
    {
        AbortCurrentAttack();
        ClearTarget();
        if (_detectionCoroutine != null)
        {
            StopCoroutine(_detectionCoroutine);
            _detectionCoroutine = null;
        }
        if (EntityDirector.Instance != null) EntityDirector.Instance.UnregisterEnemy(this);
    }
    private void Update()
    {
        if (_brain != null && (_brain.CurrentState == EnemyState.Death || _brain.CurrentState == EnemyState.Agonizing))
            return;

        if (_target != null && !IsTargetAlive())
        {
            AbortCurrentAttack();
            ClearTarget();
            return;
        }

        if (_target == null || _entityData == null)
        {
            TargetVelocity = Vector3.zero;
            return;
        }

        TargetVelocity = (_target.position - _previousTargetPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _previousTargetPos = _target.position;

        if (_brain != null && _brain.CurrentState == EnemyState.Attacking && !_isPerformingAttack)
        {
            if (_atkCountdown <= 0.0f)
            {
                if (EntityDirector.Instance == null || EntityDirector.Instance.RequestAttackPermission(this))
                {
                    _attackCoroutine = StartCoroutine(PerformAttackRoutine());
                }
            }
            else
            {
                _atkCountdown = Mathf.Max(_atkCountdown - Time.deltaTime, 0.0f);
            }
        }
    }
    private void EvaluatePhaseChange(float damageTaken)
    {
        if (_entityData == null || _entityData.phases.Count == 0) return;
        EnsureComponentsInitialized();
        if (_health == null || _health.MaxHealth <= 0) return;

        float currentHpPercent = _health.CurrentHealth / _health.MaxHealth;
        int targetPhaseIndex = 0;

        for (int i = 0; i < _entityData.phases.Count; i++)
        {
            if (currentHpPercent <= _entityData.phases[i].healthThresholdPercentage) targetPhaseIndex = i;
        }

        if (targetPhaseIndex != _currentPhaseIndex)
        {
            _currentPhaseIndex = targetPhaseIndex;
            _currentPhase = _entityData.phases[_currentPhaseIndex];
        }
    }
    private EntityAttack SelectAttackByDistance()
    {
        if (_currentPhase == null || !IsTargetAlive()) return null;
        float dist = Vector3.Distance(transform.position, _target.position);
        List<EntityAttack> candidatePool = null;

        if (PreferredAttackDistance == 1 && _currentPhase.attackPool.closeRangeAttacks.Count > 0)
            candidatePool = _currentPhase.attackPool.closeRangeAttacks;
        else if (PreferredAttackDistance == 2 && _currentPhase.attackPool.mediumRangeAttacks.Count > 0)
            candidatePool = _currentPhase.attackPool.mediumRangeAttacks;
        else if (PreferredAttackDistance == 3 && _currentPhase.attackPool.farRangeAttacks.Count > 0)
            candidatePool = _currentPhase.attackPool.farRangeAttacks;

        if (candidatePool == null || candidatePool.Count == 0)
        {
            if (dist <= _currentPhase.attackPool.closeRangeMax) candidatePool = _currentPhase.attackPool.closeRangeAttacks;
            else if (dist <= _currentPhase.attackPool.mediumRangeMax) candidatePool = _currentPhase.attackPool.mediumRangeAttacks;
            else candidatePool = _currentPhase.attackPool.farRangeAttacks;
        }

        if (candidatePool == null || candidatePool.Count == 0) candidatePool = _currentPhase.attackPool.closeRangeAttacks;
        if (candidatePool == null || candidatePool.Count == 0) return null;

        return candidatePool[UnityEngine.Random.Range(0, candidatePool.Count)];
    }
    private IEnumerator PerformAttackRoutine()
    {
        _isPerformingAttack = true;
        _activeAttackAnimationCount = 0;
        _attackAnimationStateEntered = false;
        _currentAttackLogic = SelectAttackByDistance();

        if (_currentAttackLogic == null || !IsTargetAlive())
        {
            _atkCountdown = UnityEngine.Random.Range(_entityData.minAttackCooldown, _entityData.maxAttackCooldown);
            _isPerformingAttack = false;
            _attackCoroutine = null;
            if (EntityDirector.Instance != null) EntityDirector.Instance.NotifyAttackFinished(this);
            yield break;
        }

        ApplyAttackArmorState(_currentAttackLogic);

        _currentAttackLogic.Prepare(transform, _target);

        float prepTimer = 0f;
        while (prepTimer < _currentAttackLogic.telegraphDuration)
        {
            if (!IsTargetAlive() || (_brain != null && (_brain.CurrentState == EnemyState.Agonizing || _brain.CurrentState == EnemyState.Death)))
            {
                AbortCurrentAttack();
                if (!IsTargetAlive()) ClearTarget();
                yield break;
            }

            prepTimer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(prepTimer / Mathf.Max(_currentAttackLogic.telegraphDuration, 0.001f));
            _currentAttackLogic.UpdatePreparation(transform, _target, normalizedTime);
            yield return null;
        }

        if (!IsTargetAlive() || (_brain != null && (_brain.CurrentState == EnemyState.Agonizing || _brain.CurrentState == EnemyState.Death)))
        {
            AbortCurrentAttack();
            if (!IsTargetAlive()) ClearTarget();
            yield break;
        }

        bool hasAnimationTrigger = _animator != null && !string.IsNullOrEmpty(_currentAttackLogic.animationTrigger);
        if (hasAnimationTrigger)
        {
            _animator.ResetTrigger(_currentAttackLogic.animationTrigger);
            _animator.SetTrigger(_currentAttackLogic.animationTrigger);
        }

        IEnumerator attackExecution = _currentAttackLogic.Execute(transform, _target, this);
        if (attackExecution != null)
        {
            yield return StartCoroutine(attackExecution);
        }

        if (hasAnimationTrigger)
        {
            if (!_attackAnimationStateEntered)
            {
                yield return null;
            }

            while (_activeAttackAnimationCount > 0)
            {
                if (_brain != null && (_brain.CurrentState == EnemyState.Agonizing || _brain.CurrentState == EnemyState.Death))
                {
                    AbortCurrentAttack();
                    yield break;
                }
                yield return null;
            }
        }

        ApplyCurrentArmorState();
        _atkCountdown = UnityEngine.Random.Range(_entityData.minAttackCooldown, _entityData.maxAttackCooldown);
        _isPerformingAttack = false;
        _attackAnimationStateEntered = false;
        _currentAttackLogic = null;
        _attackCoroutine = null;

        if (EntityDirector.Instance != null) EntityDirector.Instance.NotifyAttackFinished(this);
    }
    private void ApplyCurrentArmorState()
    {
        EnsureComponentsInitialized();
        if (_health == null || _entityData == null) return;
        _health.SetDamageMultiplier(_entityData.defaultArmorState == ArmorType.Hyperarmor ? _entityData.defaultHyperarmorMultiplier : 1.0f);
    }
    private void ApplyAttackArmorState(EntityAttack attack)
    {
        EnsureComponentsInitialized();
        if (_health == null) return;
        ArmorType effectiveArmor = attack.overrideBaseArmor ? attack.armorState : _entityData.defaultArmorState;
        float mult = attack.overrideBaseArmor ? attack.hyperarmorDamageMultiplier : _entityData.defaultHyperarmorMultiplier;
        _health.SetDamageMultiplier(effectiveArmor == ArmorType.Hyperarmor ? mult : 1.0f);
    }
    private void HandleBrainStateChanged(EnemyState newState)
    {
        if (newState == EnemyState.Death || newState == EnemyState.Agonizing)
        {
            AbortCurrentAttack();
            if (_entityData != null && newState == EnemyState.Agonizing)
                _atkCountdown = UnityEngine.Random.Range(_entityData.minAttackCooldown, _entityData.maxAttackCooldown);
            return;
        }

        if (newState == EnemyState.Hurt)
        {
            ArmorType currentArmor = (_currentAttackLogic != null && _currentAttackLogic.overrideBaseArmor)
                ? _currentAttackLogic.armorState
                : (_entityData != null ? _entityData.defaultArmorState : ArmorType.None);

            if (_isPerformingAttack && currentArmor == ArmorType.None)
            {
                AbortCurrentAttack();
                if (_entityData != null)
                    _atkCountdown = UnityEngine.Random.Range(_entityData.minAttackCooldown, _entityData.maxAttackCooldown);
            }
        }
    }
    private IEnumerator DetectionRoutine()
    {
        while (true)
        {
            yield return _rangeEvalInterval;
            if (_entityData == null) continue;
            if (_brain != null && _brain.CurrentState == EnemyState.Death) yield break;

            if (_target == null)
            {
                int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _entityData.detectionRadius, _hitBuffer, _targetLayer);
                for (int i = 0; i < hitCount; i++)
                {
                    Transform candidate = _hitBuffer[i].transform;
                    Health candidateHealth = candidate.GetComponentInParent<Health>();

                    if (candidateHealth != null && candidateHealth.CurrentHealth <= 0f)
                        continue;

                    if (HasClearLineOfSight(candidate, _entityData.detectionRadius))
                    {
                        SetTarget(candidate, candidateHealth);
                        break;
                    }
                }
            }
            else
            {
                if (!IsTargetAlive())
                {
                    ClearTarget();
                    continue;
                }

                float distance = Vector3.Distance(transform.position, _target.position);
                if (distance > _entityData.lossRadius || !HasClearLineOfSight(_target, distance))
                {
                    ClearTarget();
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