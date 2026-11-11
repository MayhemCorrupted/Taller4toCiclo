using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(EntityBrain))]
[RequireComponent(typeof(Health))]
public class EntityCombat : MonoBehaviour
{
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
    private float _atkCountdown = 0.0f;
    private readonly Collider[] _hitBuffer = new Collider[5];
    private WaitForSeconds _rangeEvalInterval;
    private bool _isPerformingAttack = false;
    private EntityAttack _currentAttackLogic;

    private Vector3 _previousTargetPos;
    public Vector3 TargetVelocity { get; private set; }
    public EnemyRank Rank => _entityData != null ? _entityData.rank : EnemyRank.Minion;
    public int PreferredAttackDistance => _entityData != null ? _entityData.preferredAttackDistance : 0;
    public EnemyPhase CurrentPhase => _currentPhase;
    public bool IsPerformingAttack => _isPerformingAttack;
    public Transform TargetTransform => _target;
    public Vector3 TargetLocation => _target != null ? _target.position : Vector3.positiveInfinity;
    public bool CanSeeTarget => _target != null;
    public bool IsInAttackRange
    {
        get
        {
            if (_target == null || _entityData == null) return false;

            if (EntityDirector.Instance != null && EntityDirector.Instance.GetHighestRankPresent() >= EnemyRank.MiniBoss && Rank == EnemyRank.Minion)
            {
                if (!EntityDirector.Instance.RequestAttackPermission(this)) return false;
            }

            float dist = Vector3.Distance(transform.position, _target.position);

            float maxRange = _entityData.attackRadius;
            if (_currentPhase != null)
            {
                if (PreferredAttackDistance == 1) maxRange = _currentPhase.attackPool.closeRangeMax;
                else if (PreferredAttackDistance == 2) maxRange = _currentPhase.attackPool.mediumRangeMax;
                else maxRange = _entityData.attackRadius;
            }

            return dist <= maxRange;
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
        if (_health != null) _health.OnTakeDamage += EvaluatePhaseChange;
        if (EntityDirector.Instance != null) EntityDirector.Instance.RegisterEnemy(this);
    }
    private void OnDisable()
    {
        if (_brain != null) _brain.OnStateChanged -= HandleBrainStateChanged;
        if (_health != null) _health.OnTakeDamage -= EvaluatePhaseChange;
        if (EntityDirector.Instance != null) EntityDirector.Instance.UnregisterEnemy(this);
        if (_currentAttackLogic != null) _currentAttackLogic.CancelAttack();
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
        if (gameObject.activeInHierarchy) StartCoroutine(DetectionRoutine());
    }
    private void Update()
    {
        if (_target == null || _entityData == null)
        {
            TargetVelocity = Vector3.zero;
            return;
        }

        TargetVelocity = (_target.position - _previousTargetPos) / Time.deltaTime;
        _previousTargetPos = _target.position;

        if (_brain != null && _brain.CurrentState == EnemyState.Attacking && !_isPerformingAttack)
        {
            if (_atkCountdown <= 0.0f)
            {
                if (EntityDirector.Instance == null || EntityDirector.Instance.RequestAttackPermission(this))
                {
                    StartCoroutine(PerformAttackRoutine());
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
        if (_currentPhase == null || _target == null) return null;
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

        return candidatePool[Random.Range(0, candidatePool.Count)];
    }
    private IEnumerator PerformAttackRoutine()
    {
        _isPerformingAttack = true;
        _currentAttackLogic = SelectAttackByDistance();

        if (_currentAttackLogic == null)
        {
            _atkCountdown = Random.Range(_entityData.minAttackCooldown, _entityData.maxAttackCooldown);
            _isPerformingAttack = false;
            if (EntityDirector.Instance != null) EntityDirector.Instance.NotifyAttackFinished(this);
            yield break;
        }

        ApplyAttackArmorState(_currentAttackLogic);
        if (_animator != null && !string.IsNullOrEmpty(_currentAttackLogic.animationTrigger))
            _animator.SetTrigger(_currentAttackLogic.animationTrigger);

        _currentAttackLogic.Prepare(transform, _target);

        float prepTimer = 0f;
        while (prepTimer < _currentAttackLogic.telegraphDuration)
        {
            prepTimer += Time.deltaTime;
            _currentAttackLogic.UpdatePreparation(transform, _target, prepTimer / Mathf.Max(_currentAttackLogic.telegraphDuration, 0.001f));
            yield return null;
        }

        if (_target != null)
        {
            IEnumerator attackExecution = _currentAttackLogic.Execute(transform, _target, this);
            if (attackExecution != null) yield return StartCoroutine(attackExecution);
        }

        ApplyCurrentArmorState();
        _atkCountdown = Random.Range(_entityData.minAttackCooldown, _entityData.maxAttackCooldown);
        _isPerformingAttack = false;

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
        if (newState == EnemyState.Hurt)
        {
            ArmorType currentArmor = (_currentAttackLogic != null && _currentAttackLogic.overrideBaseArmor)
                ? _currentAttackLogic.armorState
                : (_entityData != null ? _entityData.defaultArmorState : ArmorType.None);

            if (_isPerformingAttack && currentArmor == ArmorType.None)
            {
                if (_currentAttackLogic != null) _currentAttackLogic.CancelAttack();
                _isPerformingAttack = false;
                StopAllCoroutines();
                StartCoroutine(DetectionRoutine());
                _atkCountdown = Random.Range(_entityData.minAttackCooldown, _entityData.maxAttackCooldown);

                if (EntityDirector.Instance != null) EntityDirector.Instance.NotifyAttackFinished(this);
            }
        }
    }
    private IEnumerator DetectionRoutine()
    {
        while (true)
        {
            yield return _rangeEvalInterval;
            if (_entityData == null) continue;

            if (_target == null)
            {
                int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _entityData.detectionRadius, _hitBuffer, _targetLayer);
                if (hitCount > 0 && HasClearLineOfSight(_hitBuffer[0].transform, _entityData.detectionRadius))
                {
                    _target = _hitBuffer[0].transform;
                    _previousTargetPos = _target.position;
                }
            }
            else
            {
                float distance = Vector3.Distance(transform.position, _target.position);
                if (distance > _entityData.lossRadius || !HasClearLineOfSight(_target, distance))
                {
                    _target = null;
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