using UnityEngine;
public class PlayerAnimator : MonoBehaviour
{
    public bool HasWeapon { get; set; } = true;

    [SerializeField] private Animator _animator;
    private PlayerFSM _playerFSM;
    private PlayerInputs _playerInputs;
    private PlayerCombat _playerCombat;
    private PlayerHealth _playerHealth;
    private PlayerStats _playerStats;
    private InventoryManager _inventoryManager;
    private Transform _mainCameraTransform;

    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _inputXHash = Animator.StringToHash("InputX");
    private readonly int _inputYHash = Animator.StringToHash("InputY");
    private readonly int _isLockedOnHash = Animator.StringToHash("IsLockedOn");
    private readonly int _hasWeaponHash = Animator.StringToHash("HasWeapon");

    private readonly int _lightAttackHash = Animator.StringToHash("LightAttack");
    private readonly int _heavyAttackHash = Animator.StringToHash("HeavyAttack");

    private readonly int _dodgeHash = Animator.StringToHash("Dodge");
    private readonly int _isBlockingHash = Animator.StringToHash("IsBlocking");
    private readonly int _skillTriggerHash = Animator.StringToHash("SkillTrigger");
    private readonly int _skillIDHash = Animator.StringToHash("SkillID");
    private readonly int _blockHitHash = Animator.StringToHash("BlockHit");

    private readonly int _hurtHash = Animator.StringToHash("Hurt");
    private readonly int _itemHash = Animator.StringToHash("Item");
    private readonly int _knockoutHash = Animator.StringToHash("Knockout");
    private readonly int _permadeathHash = Animator.StringToHash("Permadeath");

    private float _currentInputX;
    private float _currentInputY;
    private Transform _playerRoot;
    public float DodgeNormalizedTime { get; private set; }
    private int _remainingIframes;
    private bool _areIframesActive = false;
    private void Awake()
    {
        _playerFSM = GetComponentInParent<PlayerFSM>();
        _playerInputs = GetComponentInParent<PlayerInputs>();
        _playerCombat = GetComponentInParent<PlayerCombat>();
        _playerHealth = GetComponentInParent<PlayerHealth>();
        _playerStats = GetComponentInParent<PlayerStats>();

        _inventoryManager = FindAnyObjectByType<InventoryManager>();

        _playerRoot = _playerFSM != null ? _playerFSM.transform : transform;
        if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
        if (_animator != null) _animator.SetBool(_hasWeaponHash, HasWeapon);
    }
    private void OnEnable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnLightAttack += TriggerLightAttack;
            _playerInputs.OnHeavyAttack += TriggerHeavyAttack;
        }

        if (_playerFSM != null)
        {
            _playerFSM.OnStateChanged += HandleFSMStateChanged;
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnHurt += TriggerHurt;
            _playerHealth.OnBlockHit += TriggerBlockHit;
        }
    }
    private void OnDisable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnLightAttack -= TriggerLightAttack;
            _playerInputs.OnHeavyAttack -= TriggerHeavyAttack;
        }

        if (_playerFSM != null)
        {
            _playerFSM.OnStateChanged -= HandleFSMStateChanged;
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnHurt -= TriggerHurt;
            _playerHealth.OnBlockHit -= TriggerBlockHit;
        }
    }
    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.CurrentState == GameState.Gameplay)
        {
            SyncMovement();
            TrackDodgeProgress();
        }
    }
    // LÓGICA DE IFRAMES: Conteo regresivo exacto por ticks lógicos
    private void FixedUpdate()
    {
        if (_areIframesActive)
        {
            _remainingIframes--;
            if (_remainingIframes <= 0)
            {
                CancelIframes();
            }
        }
    }
    private void TrackDodgeProgress()
    {
        if (_playerFSM != null && _playerFSM.CurrentState == PlayerState.Dodging && _animator != null)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(1);

            if (!stateInfo.IsName("Dodge"))
            {
                stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            }

            DodgeNormalizedTime = Mathf.Clamp01(stateInfo.normalizedTime);
        }
        else
        {
            DodgeNormalizedTime = 0f;
        }
    }
    private void SyncMovement()
    {
        if (_animator == null || _playerFSM == null) return;

        float currentSpeed = 0f;
        if (_playerFSM.CurrentState == PlayerState.Walking) currentSpeed = 0.5f;
        else if (_playerFSM.CurrentState == PlayerState.Running) currentSpeed = 1f;

        _animator.SetFloat(_speedHash, currentSpeed, 0.1f, Time.deltaTime);
        _animator.SetBool(_isBlockingHash, _playerFSM.CurrentState == PlayerState.Blocking);
        _animator.SetBool(_hasWeaponHash, HasWeapon);

        bool isLockedOn = _playerCombat != null && _playerCombat.LockOnTarget != null;
        _animator.SetBool(_isLockedOnHash, isLockedOn);

        bool inCombatAction = _playerFSM.CurrentState == PlayerState.Attacking ||
                              _playerFSM.CurrentState == PlayerState.Dodging ||
                              _playerFSM.CurrentState == PlayerState.Skill ||
                              _playerFSM.CurrentState == PlayerState.Blocking ||
                              _playerFSM.CurrentState == PlayerState.UsingItem ||
                              _playerFSM.CurrentState == PlayerState.Hurt ||
                              _playerFSM.CurrentState == PlayerState.Knockout ||
                              _playerFSM.CurrentState == PlayerState.Death;

        float targetWeight = inCombatAction ? 1f : 0f;

        _animator.SetLayerWeight(1, Mathf.Lerp(_animator.GetLayerWeight(1), targetWeight, Time.deltaTime * 15f));

        if (isLockedOn && _playerInputs != null)
        {
            Vector2 moveInput = _playerInputs.MoveInput;
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            float targetX = 0f;
            float targetY = 0f;

            if (inputDirection.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                if (_mainCameraTransform != null)
                {
                    targetAngle += _mainCameraTransform.eulerAngles.y;
                }

                Vector3 worldMoveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                targetX = Vector3.Dot(worldMoveDirection, _playerRoot.right) * (currentSpeed > 0 ? currentSpeed : 0.5f);
                targetY = Vector3.Dot(worldMoveDirection, _playerRoot.forward) * (currentSpeed > 0 ? currentSpeed : 0.5f);
            }

            _currentInputX = Mathf.Lerp(_currentInputX, targetX, Time.deltaTime * 10f);
            _currentInputY = Mathf.Lerp(_currentInputY, targetY, Time.deltaTime * 10f);

            _animator.SetFloat(_inputXHash, _currentInputX);
            _animator.SetFloat(_inputYHash, _currentInputY);
        }
        else
        {
            _currentInputX = Mathf.Lerp(_currentInputX, 0f, Time.deltaTime * 10f);
            _currentInputY = Mathf.Lerp(_currentInputY, 0f, Time.deltaTime * 10f);
            _animator.SetFloat(_inputXHash, _currentInputX);
            _animator.SetFloat(_inputYHash, _currentInputY);
        }
    }
    private void HandleFSMStateChanged(PlayerState newState)
    {
        if (_animator == null) return;

        if (newState == PlayerState.Dodging)
        {
            _animator.SetTrigger(_dodgeHash);
        }
        else if (newState == PlayerState.Skill)
        {
            if (_playerCombat != null)
            {
                _animator.SetInteger(_skillIDHash, _playerCombat.CurrentSkillID);
            }
            _animator.SetTrigger(_skillTriggerHash);
        }
        else if (newState == PlayerState.UsingItem)
        {
            _animator.SetTrigger(_itemHash);
        }
        else if (newState == PlayerState.Knockout)
        {
            _animator.SetTrigger(_knockoutHash);
        }
        else if (newState == PlayerState.Death)
        {
            _animator.SetTrigger(_permadeathHash);
        }
    }
    private bool CanAttack()
    {
        if (_playerFSM == null) return false;
        return _playerFSM.CurrentState == PlayerState.Idle ||
               _playerFSM.CurrentState == PlayerState.Walking ||
               _playerFSM.CurrentState == PlayerState.Running;
    }
    private void TriggerLightAttack()
    {
        if (CanAttack() && _playerCombat != null && _animator != null)
        {
            if (_playerCombat.TryLightAttack()) _animator.SetTrigger(_lightAttackHash);
        }
    }
    private void TriggerHeavyAttack()
    {
        if (CanAttack() && _playerCombat != null && _animator != null)
        {
            if (_playerCombat.TryHeavyAttack()) _animator.SetTrigger(_heavyAttackHash);
        }
    }
    private void TriggerHurt()
    {
        if (_animator != null) _animator.SetTrigger(_hurtHash);
    }
    private void TriggerBlockHit()
    {
        if (_animator != null) _animator.SetTrigger(_blockHitHash);
    }
    public void AE_ApplyItemEffect()
    {
        if (_inventoryManager != null)
        {
            _inventoryManager.ApplyConsumableEffect();
        }
    }
    public void AE_ExecuteSkill()
    {
        if (_playerCombat != null) _playerCombat.ExecuteSkill();
    }
    public void AE_ActivateLightHitbox()
    {
        if (_playerCombat != null) _playerCombat.ActivateWeapon(false);
    }
    public void AE_ActivateHeavyHitbox()
    {
        if (_playerCombat != null) _playerCombat.ActivateWeapon(true);
    }
    public void AE_DeactivateHitbox()
    {
        if (_playerCombat != null) _playerCombat.DeactivateWeapon();
    }
    public void AE_StartIframes()
    {
        if (_playerStats != null && !_areIframesActive)
        {
            _remainingIframes = _playerStats.Iframes;
            _areIframesActive = true;
            if (_playerHealth != null) _playerHealth.IsDodgeInvulnerable = true;
        }
    }
    private void CancelIframes()
    {
        if (!_areIframesActive) return;

        _areIframesActive = false;
        if (_playerHealth != null) _playerHealth.IsDodgeInvulnerable = false;
    }
    public void OnAnimationStateExit(ActionStateType type)
    {
        if (_animator == null || _playerFSM == null) return;

        switch (type)
        {
            case ActionStateType.Attack:
                if (_playerCombat != null) _playerCombat.EndAttackRoutine();
                break;
            case ActionStateType.Dodge:
                CancelIframes();
            goto case ActionStateType.UsingItem;
            case ActionStateType.Skill:
            case ActionStateType.Hurt:
            case ActionStateType.UsingItem:
                _animator.ResetTrigger(_dodgeHash);
                _animator.ResetTrigger(_skillTriggerHash);
                _animator.ResetTrigger(_hurtHash);
                _animator.ResetTrigger(_itemHash);

                if (_playerFSM.CurrentState == PlayerState.Dodging ||
                    _playerFSM.CurrentState == PlayerState.Skill ||
                    _playerFSM.CurrentState == PlayerState.Hurt ||
                    _playerFSM.CurrentState == PlayerState.UsingItem)
                {
                    _playerFSM.ChangeState(PlayerState.Idle);
                }
                break;
            case ActionStateType.Knockout:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ProcessPlayerDeathAfterAnimation();
                }
                break;
            case ActionStateType.Death:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ProcessGameOverAfterAnimation();
                }
                break;
        }
    }
    public void RespawnPlayer()
    {
        if (_animator != null)
        {
            _animator.ResetTrigger(_knockoutHash);
            _animator.ResetTrigger(_permadeathHash);
            _animator.ResetTrigger(_hurtHash);
            _animator.ResetTrigger(_itemHash);
        }

        if (_playerFSM != null && (_playerFSM.CurrentState == PlayerState.Knockout || _playerFSM.CurrentState == PlayerState.Death))
        {
            _playerFSM.ChangeState(PlayerState.Idle);
        }
    }
}