using UnityEngine;
public class PlayerAnimator : MonoBehaviour
{
    public bool HasWeapon { get; set; } = true;

    [SerializeField] private Animator _animator;
    private PlayerFSM _playerFSM;
    private PlayerInputs _playerInputs;
    private PlayerCombat _playerCombat;
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
    
    private float _currentInputX;
    private float _currentInputY;
    private void Awake()
    {
        _playerFSM = GetComponentInParent<PlayerFSM>();
        _playerInputs = GetComponentInParent<PlayerInputs>();
        _playerCombat = GetComponentInParent<PlayerCombat>();
        if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
    }
    private void OnEnable()
    {
        _playerInputs.OnLightAttack += TriggerLightAttack;
        _playerInputs.OnHeavyAttack += TriggerHeavyAttack;
        _playerInputs.OnDodge += TriggerDodge;
        _playerInputs.OnSkill += TriggerSkill;
    }
    private void OnDisable()
    {
        _playerInputs.OnLightAttack -= TriggerLightAttack;
        _playerInputs.OnHeavyAttack -= TriggerHeavyAttack;
        _playerInputs.OnDodge -= TriggerDodge;
        _playerInputs.OnSkill -= TriggerSkill;
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

                targetX = Vector3.Dot(worldMoveDirection, transform.right) * currentSpeed;
                targetY = Vector3.Dot(worldMoveDirection, transform.forward) * currentSpeed;
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
    private bool CanInterruptMovement()
    {
        return _playerFSM.CurrentState == PlayerState.Idle ||
               _playerFSM.CurrentState == PlayerState.Walking ||
               _playerFSM.CurrentState == PlayerState.Running;
    }
    private void TriggerLightAttack()
    {
        if (CanInterruptMovement())
        {
            _animator.SetTrigger(_lightAttackHash);
        }
    }
    private void TriggerHeavyAttack()
    {
        if (CanInterruptMovement())
        {
            _animator.SetTrigger(_heavyAttackHash);
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
        if (CanInterruptMovement())
        {
            int skillIdToUse = 0;
            _animator.SetInteger(_skillIDHash, skillIdToUse);
            _animator.SetTrigger(_skillTriggerHash);
        }
    }
    public void AE_ActivateLightHitbox()
    {
        _playerCombat.ActivateWeapon(false);
    }
    public void AE_ActivateHeavyHitbox()
    {
        _playerCombat.ActivateWeapon(true);
    }
    public void AE_DeactivatetHitbox()
    {
        _playerCombat.DeactivateWeapon();
    }   
    public void AE_FinishAction()
    {
        _playerFSM.ChangeState(PlayerState.Idle);
    }
}
