using UnityEngine;
using UnityEngine.AI;
public class EntityAnimator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator _animator;
    [SerializeField] private EntityBrain _brain;
    [SerializeField] private EntityCombat _combat;
    [SerializeField] private NavMeshAgent _navAgent;

    [Header("Suavizado de Locomoción")]
    [SerializeField] private float _speedDampTime = 0.1f;
    [SerializeField] private float _directionalLerpSpeed = 10f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int IsLockedOnHash = Animator.StringToHash("IsLockedOn");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int AgonizeHash = Animator.StringToHash("Agonize");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private Transform _entityRoot;
    private float _currentInputX;
    private float _currentInputY;
    private void Awake()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_brain == null) _brain = GetComponentInParent<EntityBrain>();
        if (_combat == null) _combat = GetComponentInParent<EntityCombat>();
        if (_navAgent == null) _navAgent = GetComponentInParent<NavMeshAgent>();

        _entityRoot = _brain != null ? _brain.transform : transform;
    }
    private void OnEnable()
    {
        if (_brain != null)
        {
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnHurtTriggered += HandleHurtTriggered;
        }
    }
    private void OnDisable()
    {
        if (_brain != null)
        {
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnHurtTriggered -= HandleHurtTriggered;
        }
    }
    private void Update()
    {
        if (_animator == null) return;

        if (_brain != null && (_brain.CurrentState == EnemyState.Death || _brain.CurrentState == EnemyState.Agonizing || _brain.CurrentState == EnemyState.Hurt))
        {
            _animator.SetFloat(SpeedHash, 0f);
            _animator.SetFloat(InputXHash, 0f);
            _animator.SetFloat(InputYHash, 0f);
            if (_brain.CurrentState == EnemyState.Death)
                _animator.SetBool(IsLockedOnHash, false);
            return;
        }

        SyncLocomotion();
    }
    private void SyncLocomotion()
    {
        Vector3 horizontalVel = Vector3.zero;
        float maxSpeed = 1f;

        if (_navAgent != null && _navAgent.enabled)
        {
            horizontalVel = _navAgent.velocity;
            horizontalVel.y = 0f;
            maxSpeed = Mathf.Max(_navAgent.speed, 0.01f);
        }

        float speedMagnitude = horizontalVel.magnitude;
        float normalizedSpeed = Mathf.Clamp01(speedMagnitude / maxSpeed);

        bool isLockedOn = _combat != null && _combat.CanSeeTarget;
        _animator.SetBool(IsLockedOnHash, isLockedOn);

        if (isLockedOn)
        {
            _animator.SetFloat(SpeedHash, 0f, _speedDampTime, Time.deltaTime);

            float targetX = 0f;
            float targetY = 0f;

            if (speedMagnitude > 0.05f)
            {
                Vector3 moveDirection = horizontalVel.normalized;
                targetX = Vector3.Dot(moveDirection, _entityRoot.right) * normalizedSpeed;
                targetY = Vector3.Dot(moveDirection, _entityRoot.forward) * normalizedSpeed;
            }

            _currentInputX = Mathf.Lerp(_currentInputX, targetX, Time.deltaTime * _directionalLerpSpeed);
            _currentInputY = Mathf.Lerp(_currentInputY, targetY, Time.deltaTime * _directionalLerpSpeed);
        }
        else
        {
            _animator.SetFloat(SpeedHash, normalizedSpeed, _speedDampTime, Time.deltaTime);

            _currentInputX = Mathf.Lerp(_currentInputX, 0f, Time.deltaTime * _directionalLerpSpeed);
            _currentInputY = Mathf.Lerp(_currentInputY, 0f, Time.deltaTime * _directionalLerpSpeed);
        }

        _animator.SetFloat(InputXHash, _currentInputX);
        _animator.SetFloat(InputYHash, _currentInputY);
    }
    private void HandleHurtTriggered()
    {
        if (_animator == null || (_brain != null && _brain.CurrentState == EnemyState.Death)) return;

        _animator.ResetTrigger(HurtHash);
        _animator.SetTrigger(HurtHash);
    }
    private void HandleStateChanged(EnemyState newState)
    {
        if (_animator == null) return;

        switch (newState)
        {
            case EnemyState.Agonizing:
                _animator.ResetTrigger(HurtHash);
                _animator.SetTrigger(AgonizeHash);
                break;

            case EnemyState.Death:
                _animator.ResetTrigger(AgonizeHash);
                _animator.ResetTrigger(HurtHash);
                _animator.SetBool(IsLockedOnHash, false);
                _animator.SetBool(IsDeadHash, true);
                _animator.SetTrigger(DeathHash);
                break;
        }
    }
    public void OnAnimationStateEnter(EntityActionStateType type)
    {
        switch (type)
        {
            case EntityActionStateType.Attack:
                if (_combat != null) _combat.NotifyAttackAnimationStarted();
                break;

            case EntityActionStateType.Hurt:
                if (_brain != null) _brain.NotifyHurtAnimationStarted();
                break;
        }
    }
    public void OnAnimationStateExit(EntityActionStateType type)
    {
        if (_animator == null) return;

        switch (type)
        {
            case EntityActionStateType.Attack:
                if (_combat != null) _combat.NotifyAttackAnimationEnded();
                break;

            case EntityActionStateType.Agonize:
                _animator.ResetTrigger(AgonizeHash);
                if (_brain != null) _brain.EndAgonize();
                break;

            case EntityActionStateType.Hurt:
                _animator.ResetTrigger(HurtHash);
                if (_brain != null) _brain.NotifyHurtAnimationEnded();
                break;

            case EntityActionStateType.Death:
                if (_brain != null) _brain.NotifyDeathAnimationFinished();
                break;
        }
    }
    public void PlayTrigger(string triggerName)
    {
        if (_animator != null && !string.IsNullOrEmpty(triggerName))
            _animator.SetTrigger(triggerName);
    }
}