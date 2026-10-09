using UnityEngine;
[RequireComponent(typeof(PlayerFSM))]
[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    private PlayerFSM _playerFSM;
    private PlayerInputs _playerInputs;
    private PlayerCombat _playerCombat;

    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _lightAttackHash = Animator.StringToHash("LightAttack");
    private readonly int _heavyAttackHash = Animator.StringToHash("HeavyAttack");
    private readonly int _dodgeHash = Animator.StringToHash("Dodge");
    private readonly int _isBlockingHash = Animator.StringToHash("IsBlocking");
    private void Awake()
    {
        _playerFSM = GetComponent<PlayerFSM>();
        _playerInputs = GetComponent<PlayerInputs>();
        _playerCombat = GetComponent<PlayerCombat>();
    }
    private void OnEnable()
    {
        _playerInputs.OnLightAttack += TriggerLightAttack;
        _playerInputs.OnHeavyAttack += TriggerHeavyAttack;
        _playerInputs.OnDodge += TriggerDodge;
    }
    private void OnDisable()
    {
        _playerInputs.OnLightAttack -= TriggerLightAttack;
        _playerInputs.OnHeavyAttack -= TriggerHeavyAttack;
        _playerInputs.OnDodge -= TriggerDodge;
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
