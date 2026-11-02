using UnityEngine;
public class PlayerAnimator : MonoBehaviour
{
    public bool HasWeapon { get; set; } = true;

    [SerializeField] private Animator _animator;
    private PlayerFSM _playerFSM;
    private PlayerInputs _playerInputs;
    private PlayerCombat _playerCombat;
    private PlayerHealth _playerHealth;
    private Transform _mainCameraTransform;

    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _inputXHash = Animator.StringToHash("InputX");
    private readonly int _inputYHash = Animator.StringToHash("InputY");
    private readonly int _isLockedOnHash = Animator.StringToHash("IsLockedOn");
    private readonly int _hasWeaponHash = Animator.StringToHash("HasWeapon");

    private readonly int _lightAttackHash = Animator.StringToHash("LightAttack");
    private readonly int _heavyAttackHash = Animator.StringToHash("HeavyAttack");
    private readonly int _lightComboHash = Animator.StringToHash("LightCombo");
    private readonly int _heavyComboHash = Animator.StringToHash("HeavyCombo");

    private readonly int _dodgeHash = Animator.StringToHash("Dodge");
    private readonly int _isBlockingHash = Animator.StringToHash("IsBlocking");
    private readonly int _skillTriggerHash = Animator.StringToHash("SkillTrigger");
    private readonly int _skillIDHash = Animator.StringToHash("SkillID");
    private readonly int _blockHitHash = Animator.StringToHash("BlockHit");
    private readonly int _knockoutHash = Animator.StringToHash("Knockout");
    private readonly int _permadeathHash = Animator.StringToHash("Permadeath");

    private float _currentInputX;
    private float _currentInputY;
    private Transform _playerRoot;
    private void Awake()
    {
        _playerFSM = GetComponentInParent<PlayerFSM>();
        _playerInputs = GetComponentInParent<PlayerInputs>();
        _playerCombat = GetComponentInParent<PlayerCombat>();
        _playerHealth = GetComponentInParent<PlayerHealth>();
        _playerRoot = _playerFSM != null ? _playerFSM.transform : transform;
        if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
        if (_animator != null) _animator.SetBool(_hasWeaponHash, HasWeapon);
    }
    private void OnEnable()
    {
        _playerInputs.OnLightAttack += TriggerLightAttack;
        _playerInputs.OnHeavyAttack += TriggerHeavyAttack;
        _playerInputs.OnDodge += TriggerDodge;
        _playerInputs.OnSkill += TriggerSkill;

        if (_playerHealth != null)
        {
            _playerHealth.OnLifeLost += TriggerKnockout;
            _playerHealth.OnPermadeath += TriggerPermadeath;
            _playerHealth.OnBlockHit += TriggerBlockHit;
        }
    }
    private void OnDisable()
    {
        _playerInputs.OnLightAttack -= TriggerLightAttack;
        _playerInputs.OnHeavyAttack -= TriggerHeavyAttack;
        _playerInputs.OnDodge -= TriggerDodge;
        _playerInputs.OnSkill -= TriggerSkill;

        if (_playerHealth != null)
        {
            _playerHealth.OnLifeLost -= TriggerKnockout;
            _playerHealth.OnPermadeath -= TriggerPermadeath;
            _playerHealth.OnBlockHit -= TriggerBlockHit;
        }
    }
    private void Update()
    {
        SyncMovement();
    }
    private void SyncMovement()
    {
        float currentSpeed = 0f;
        if (_playerFSM.CurrentState == PlayerState.Walking) currentSpeed = 0.5f;
        else if (_playerFSM.CurrentState == PlayerState.Running) currentSpeed = 1f;

        _animator.SetFloat(_speedHash, currentSpeed, 0.1f, Time.deltaTime);
        _animator.SetBool(_isBlockingHash, _playerFSM.CurrentState == PlayerState.Blocking);
        _animator.SetBool(_hasWeaponHash, HasWeapon);

        bool isLockedOn = _playerCombat.LockOnTarget != null;
        _animator.SetBool(_isLockedOnHash, isLockedOn);

        bool inCombatAction = _playerFSM.CurrentState == PlayerState.Attacking ||
                              _playerFSM.CurrentState == PlayerState.Dodging ||
                              _playerFSM.CurrentState == PlayerState.Skill ||
                              _playerFSM.CurrentState == PlayerState.Blocking ||
                              _playerFSM.CurrentState == PlayerState.Hurt;

        float targetWeight = inCombatAction ? 1f : 0f;

        _animator.SetLayerWeight(1, Mathf.Lerp(_animator.GetLayerWeight(1), targetWeight, Time.deltaTime * 15f));
        if (isLockedOn)
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
    private bool CanAttack()
    {
        return _playerFSM.CurrentState == PlayerState.Idle ||
               _playerFSM.CurrentState == PlayerState.Walking ||
               _playerFSM.CurrentState == PlayerState.Running ||
               _playerFSM.CurrentState == PlayerState.Attacking;
    }
    private void TriggerLightAttack()
    {
        if (CanAttack())
        {
            if (_playerCombat.TryLightAttack(out int comboIndex))
            {
                _animator.SetInteger(_lightComboHash, comboIndex);
                _animator.SetTrigger(_lightAttackHash);
            }
        }
    }
    private void TriggerHeavyAttack()
    {
        if (CanAttack())
        {
            if (_playerCombat.TryHeavyAttack(out int comboIndex))
            {
                _animator.SetInteger(_heavyComboHash, comboIndex);
                _animator.SetTrigger(_heavyAttackHash);
            }
        }
    }
    private void TriggerDodge()
    {
        if (_playerFSM.CurrentState != PlayerState.Attacking &&
            _playerFSM.CurrentState != PlayerState.Skill &&
            _playerFSM.CurrentState != PlayerState.Hurt)
        {
            _animator.SetTrigger(_dodgeHash);
        }
    }
    private void TriggerSkill()
    {
        if (_playerFSM.CurrentState == PlayerState.Idle || _playerFSM.CurrentState == PlayerState.Walking || _playerFSM.CurrentState == PlayerState.Running)
        {
            int skillIdToUse = 0;
            _animator.SetInteger(_skillIDHash, skillIdToUse);
            _animator.SetTrigger(_skillTriggerHash);
        }
    }
    private void TriggerKnockout() => _animator.SetTrigger(_knockoutHash);
    private void TriggerPermadeath() => _animator.SetTrigger(_permadeathHash);
    private void TriggerBlockHit() => _animator.SetTrigger(_blockHitHash);
    public void AE_ActivateLightHitbox() => _playerCombat.ActivateWeapon(false);
    public void AE_ActivateHeavyHitbox() => _playerCombat.ActivateWeapon(true);
    public void AE_DeactivateHitbox() => _playerCombat.DeactivateWeapon();
    public void AE_FinishAction()
    {
        _playerFSM.ChangeState(PlayerState.Idle);
        _playerCombat.EndAttackRoutine();
    }
}