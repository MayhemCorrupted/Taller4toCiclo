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

    [Header("Capas y Detección")]
    [SerializeField] private Animator _animator;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _wallLayer;
    [Min(0.05f)][SerializeField] private float _checkInterval = 0.1f;
    [Min(0.1f)][SerializeField] private float _eyeHeight = 1.2f;

    private EntityBrain _brain;
    private Health _health;
    private EntityData _entityData;
    private EnemyPhase _currentPhase;
    private int _currentPhaseIndex = -1;

    private Transform _target;
    private Health _targetHealth;
    private float _lossTimer = 0f;

    private float _closeCooldownTimer = 0f;
    private float _mediumCooldownTimer = 0f;
    private float _farCooldownTimer = 0f;

    private int _closeAttackIndex = 0;
    private int _mediumAttackIndex = 0;
    private int _farAttackIndex = 0;

    private readonly Collider[] _hitBuffer = new Collider[8];
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
    public EnemyRank Rank => _entityData != null ? _entityData.rank : EnemyRank.Weak;
    public EnemyCombatArchetype CombatArchetype => _entityData != null ? _entityData.combatArchetype : EnemyCombatArchetype.Melee;
    public AttackRangeCategory PreferredCombatRange => _entityData != null ? _entityData.preferredCombatRange : AttackRangeCategory.None;
    public EnemyPhase CurrentPhase => _currentPhase;
    public bool IsPerformingAttack => _isPerformingAttack;
    public bool IsAttackAnimationPlaying => _activeAttackAnimationCount > 0;
    public Transform TargetTransform => IsTargetAlive() ? _target : null;
    public Vector3 TargetLocation => IsTargetAlive() ? _target.position : Vector3.positiveInfinity;
    public bool HasTarget => IsTargetAlive();
    public bool CanSeeTarget => IsTargetAlive();
    public bool IsLosingTarget => _lossTimer > 0f;
    public float DistanceToTarget
    {
        get
        {
            if (!IsTargetAlive()) return float.PositiveInfinity;
            Vector3 diff = _target.position - transform.position;
            diff.y = 0f;
            return diff.magnitude;
        }
    }
    public bool CanAttackFromCurrentPosition
    {
        get
        {
            if (_brain == null || _brain.CurrentState == EnemyState.Agonizing || _brain.CurrentState == EnemyState.Death)
                return false;

            if (!IsTargetAlive() || _entityData == null || _currentPhase == null)
                return false;

            return TryResolveAttackBandForCurrentDistance(out _);
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
    public void ForceAlertNearestTarget()
    {
        if (IsTargetAlive() || _brain == null) return;

        float alertRadius = Mathf.Max(_brain.LossDetectionRadius, _brain.LongRangeRadius * 1.5f);
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, alertRadius, _hitBuffer, _targetLayer);
        for (int i = 0; i < hitCount; i++)
        {
            Transform candidate = _hitBuffer[i].transform;
            Health candidateHealth = candidate.GetComponentInParent<Health>();

            if (candidateHealth != null && candidateHealth.CurrentHealth <= 0f)
                continue;

            SetTarget(candidate, candidateHealth);
            break;
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
    public float GetDesiredCombatHoldRadius()
    {
        if (_brain == null) return 2.5f;
        if (_currentPhase == null) return _brain.GetOptimalRadiusForCategory(PreferredCombatRange);

        DetectedRangeBand currentBand = _brain.EvaluateDistanceBand(DistanceToTarget);
        AttackDistancePool pool = _currentPhase.attackPool;

        if (PreferredCombatRange == AttackRangeCategory.None)
        {
            if (TryResolveAttackBandForCurrentDistance(out AttackRangeCategory activeBand))
                return _brain.GetOptimalRadiusForCategory(activeBand);

            if (HasValidAttacks(pool.closeRangeAttacks))
                return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Close);
            if (HasValidAttacks(pool.mediumRangeAttacks))
                return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Medium);
            if (HasValidAttacks(pool.farRangeAttacks))
                return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Far);

            return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Close);
        }

        if (PreferredCombatRange == AttackRangeCategory.Medium)
        {
            if ((currentBand == DetectedRangeBand.Short || currentBand == DetectedRangeBand.PersonalSpace) && HasValidAttacks(pool.closeRangeAttacks))
                return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Close);

            return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Medium);
        }

        if (PreferredCombatRange == AttackRangeCategory.Far)
        {
            if ((currentBand == DetectedRangeBand.Short || currentBand == DetectedRangeBand.PersonalSpace) && HasValidAttacks(pool.closeRangeAttacks))
                return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Close);

            if (currentBand == DetectedRangeBand.Medium && HasValidAttacks(pool.mediumRangeAttacks))
                return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Medium);

            return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Far);
        }

        return _brain.GetOptimalRadiusForCategory(AttackRangeCategory.Close);
    }
    private bool TryResolveAttackBandForCurrentDistance(out AttackRangeCategory resolvedCategory)
    {
        resolvedCategory = AttackRangeCategory.Close;
        if (_brain == null || _currentPhase == null) return false;

        DetectedRangeBand band = _brain.EvaluateDistanceBand(DistanceToTarget);
        AttackDistancePool pool = _currentPhase.attackPool;

        switch (PreferredCombatRange)
        {
            case AttackRangeCategory.None:
                if ((band == DetectedRangeBand.Short || band == DetectedRangeBand.PersonalSpace) && HasValidAttacks(pool.closeRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Close;
                    return true;
                }
                if (band == DetectedRangeBand.Medium && HasValidAttacks(pool.mediumRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Medium;
                    return true;
                }
                if (band == DetectedRangeBand.Long && HasValidAttacks(pool.farRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Far;
                    return true;
                }
                return false;

            case AttackRangeCategory.Close:
                if ((band == DetectedRangeBand.Short || band == DetectedRangeBand.PersonalSpace) && HasValidAttacks(pool.closeRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Close;
                    return true;
                }
                return false;

            case AttackRangeCategory.Medium:
                if (band == DetectedRangeBand.Medium && HasValidAttacks(pool.mediumRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Medium;
                    return true;
                }
                if ((band == DetectedRangeBand.Short || band == DetectedRangeBand.PersonalSpace) && HasValidAttacks(pool.closeRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Close;
                    return true;
                }
                return false;

            case AttackRangeCategory.Far:
                if (band == DetectedRangeBand.Long && HasValidAttacks(pool.farRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Far;
                    return true;
                }
                if (band == DetectedRangeBand.Medium && HasValidAttacks(pool.mediumRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Medium;
                    return true;
                }
                if ((band == DetectedRangeBand.Short || band == DetectedRangeBand.PersonalSpace) && HasValidAttacks(pool.closeRangeAttacks))
                {
                    resolvedCategory = AttackRangeCategory.Close;
                    return true;
                }
                return false;
        }

        return false;
    }
    private bool HasValidAttacks(List<EntityAttack> list)
    {
        if (list == null || list.Count == 0) return false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null) return true;
        }
        return false;
    }
    private bool IsBandCooldownReady(AttackRangeCategory category)
    {
        return category switch
        {
            AttackRangeCategory.Close => _closeCooldownTimer <= 0f,
            AttackRangeCategory.Medium => _mediumCooldownTimer <= 0f,
            AttackRangeCategory.Far => _farCooldownTimer <= 0f,
            _ => true
        };
    }
    private void StartBandCooldown(AttackRangeCategory category)
    {
        if (_currentPhase == null) return;
        AttackDistancePool pool = _currentPhase.attackPool;

        switch (category)
        {
            case AttackRangeCategory.Close:
                _closeCooldownTimer = pool.closeRotationCooldown;
                break;
            case AttackRangeCategory.Medium:
                _mediumCooldownTimer = pool.mediumRotationCooldown;
                break;
            case AttackRangeCategory.Far:
                _farCooldownTimer = pool.farRotationCooldown;
                break;
        }
    }
    private EntityAttack GetNextRotatedAttack(AttackRangeCategory category)
    {
        if (_currentPhase == null) return null;
        AttackDistancePool pool = _currentPhase.attackPool;

        List<EntityAttack> list;
        int currentIndex;

        switch (category)
        {
            case AttackRangeCategory.Close:
                list = pool.closeRangeAttacks;
                currentIndex = _closeAttackIndex;
                break;
            case AttackRangeCategory.Medium:
                list = pool.mediumRangeAttacks;
                currentIndex = _mediumAttackIndex;
                break;
            default:
                list = pool.farRangeAttacks;
                currentIndex = _farAttackIndex;
                break;
        }

        if (!HasValidAttacks(list)) return null;

        for (int attempt = 0; attempt < list.Count; attempt++)
        {
            int idx = (currentIndex + attempt) % list.Count;
            if (list[idx] != null)
            {
                int nextIdx = (idx + 1) % list.Count;
                if (category == AttackRangeCategory.Close) _closeAttackIndex = nextIdx;
                else if (category == AttackRangeCategory.Medium) _mediumAttackIndex = nextIdx;
                else _farAttackIndex = nextIdx;

                return list[idx];
            }
        }

        return null;
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
        _lossTimer = 0f;
        _previousTargetPos = _target.position;
        OnAnyEnemyTargetDetected?.Invoke(this);
    }
    private void ClearTarget()
    {
        if (_target == null && _targetHealth == null) return;

        _target = null;
        _targetHealth = null;
        _lossTimer = 0f;
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

            _currentAttackLogic.CancelAttackFor(transform);
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
        if (_closeCooldownTimer > 0f) _closeCooldownTimer = Mathf.Max(0f, _closeCooldownTimer - Time.deltaTime);
        if (_mediumCooldownTimer > 0f) _mediumCooldownTimer = Mathf.Max(0f, _mediumCooldownTimer - Time.deltaTime);
        if (_farCooldownTimer > 0f) _farCooldownTimer = Mathf.Max(0f, _farCooldownTimer - Time.deltaTime);

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
            if (TryResolveAttackBandForCurrentDistance(out AttackRangeCategory activeBand) && IsBandCooldownReady(activeBand))
            {
                if (EntityDirector.Instance == null || EntityDirector.Instance.RequestAttackPermission(this))
                {
                    _attackCoroutine = StartCoroutine(PerformAttackRoutine(activeBand));
                }
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
            _closeAttackIndex = 0;
            _mediumAttackIndex = 0;
            _farAttackIndex = 0;
        }
    }
    private IEnumerator PerformAttackRoutine(AttackRangeCategory bandCategory)
    {
        _isPerformingAttack = true;
        _activeAttackAnimationCount = 0;
        _attackAnimationStateEntered = false;
        _currentAttackLogic = GetNextRotatedAttack(bandCategory);

        if (_currentAttackLogic == null || !IsTargetAlive())
        {
            StartBandCooldown(bandCategory);
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
            float waitEnterTimer = 0f;
            while (!_attackAnimationStateEntered && waitEnterTimer < 0.35f)
            {
                waitEnterTimer += Time.deltaTime;
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
        StartBandCooldown(bandCategory);
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
            }
        }
    }
    private IEnumerator DetectionRoutine()
    {
        while (true)
        {
            yield return _rangeEvalInterval;
            if (_entityData == null || _brain == null) continue;
            if (_brain.CurrentState == EnemyState.Death) yield break;

            if (_target == null)
            {
                float maxDetect = _brain.MaxDetectionRadius;
                int hitCount = Physics.OverlapSphereNonAlloc(transform.position, maxDetect, _hitBuffer, _targetLayer);
                for (int i = 0; i < hitCount; i++)
                {
                    Transform candidate = _hitBuffer[i].transform;
                    Health candidateHealth = candidate.GetComponentInParent<Health>();

                    if (candidateHealth != null && candidateHealth.CurrentHealth <= 0f)
                        continue;

                    if (HasClearLineOfSight(candidate))
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

                float distance = DistanceToTarget;
                bool isInRangeAndVisible = distance <= _brain.LossDetectionRadius && HasClearLineOfSight(_target);

                if (isInRangeAndVisible)
                {
                    _lossTimer = 0f;
                }
                else
                {
                    _lossTimer += _checkInterval;
                    if (_lossTimer >= _brain.LossDetectionDuration)
                    {
                        ClearTarget();
                    }
                }
            }
        }
    }
    private bool HasClearLineOfSight(Transform targetToCast)
    {
        Vector3 originEye = transform.position + Vector3.up * _eyeHeight;
        Vector3 targetEye = targetToCast.position + Vector3.up * _eyeHeight;
        Vector3 toTarget = targetEye - originEye;
        float distance = toTarget.magnitude;

        if (distance <= 0.05f) return true;
        return !Physics.Raycast(originEye, toTarget / distance, distance, _wallLayer, QueryTriggerInteraction.Ignore);
    }
}