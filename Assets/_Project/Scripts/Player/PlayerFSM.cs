using System;
using UnityEngine;
public enum PlayerState { Idle, Walking, Running, Dodging, Attacking, Skill, Blocking, UsingItem, Hurt, Knockout, Death, StandUp }
[RequireComponent(typeof(PlayerStamina))]
[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerFSM : MonoBehaviour
{
    public PlayerState CurrentState { get; private set; }
    public event Action<PlayerState> OnStateChanged;
    private PlayerInputs _playerInputs;
    private PlayerStamina _playerStamina;
    private PlayerCombat _playerCombat;
    private PlayerAnimator _playerAnimator;
    private void Awake()
    {
        _playerInputs = GetComponent<PlayerInputs>();
        _playerStamina = GetComponent<PlayerStamina>();
        _playerCombat = GetComponent<PlayerCombat>();
        _playerAnimator = GetComponentInChildren<PlayerAnimator>();
    }
    private void OnEnable()
    {
        _playerInputs.OnDodge += CallDodgeRequest;
        _playerInputs.OnSkill += CallSkillRequest;
    }
    private void OnDisable()
    {
        _playerInputs.OnDodge -= CallDodgeRequest;
        _playerInputs.OnSkill -= CallSkillRequest;
    }
    private void Update()
    {
        StateEvaluations();
    }
    private bool IsBlockedByLayingOrStandUp()
    {
        return _playerAnimator != null && (_playerAnimator.IsLaying || _playerAnimator.IsStandingUp);
    }
    private void StateEvaluations()
    {
        if (IsBlockedByLayingOrStandUp() ||
            CurrentState == PlayerState.Attacking ||
            CurrentState == PlayerState.Dodging ||
            CurrentState == PlayerState.Skill ||
            CurrentState == PlayerState.UsingItem ||
            CurrentState == PlayerState.Hurt ||
            CurrentState == PlayerState.Knockout ||
            CurrentState == PlayerState.Death ||
            CurrentState == PlayerState.StandUp)
        {
            return;
        }

        if (CurrentState == PlayerState.Blocking && _playerAnimator != null && _playerAnimator.IsBlockHitPlaying)
        {
            return;
        }

        bool canBlock = _playerCombat == null || _playerCombat.IsWeaponActive;

        if (_playerInputs.InputBlock && canBlock)
        {
            ChangeState(PlayerState.Blocking);
        }
        else if (_playerInputs.MoveInput.magnitude > 0.1f)
        {
            ChangeState(_playerInputs.InputRun && _playerStamina.CurrentStamina > 0 ? PlayerState.Running : PlayerState.Walking);
        }
        else
        {
            ChangeState(PlayerState.Idle);
        }
    }
    private void CallDodgeRequest()
    {
        if (IsBlockedByLayingOrStandUp()) return;

        if (CurrentState == PlayerState.Idle ||
            CurrentState == PlayerState.Walking ||
            CurrentState == PlayerState.Running ||
            CurrentState == PlayerState.Blocking)
        {
            if (_playerStamina.TryConsumeDodgeStamina())
            {
                ChangeState(PlayerState.Dodging);
            }
        }
    }
    private void CallSkillRequest()
    {
        if (IsBlockedByLayingOrStandUp()) return;
        if (_playerCombat != null && !_playerCombat.IsWeaponActive) return;

        if (CurrentState == PlayerState.Idle || CurrentState == PlayerState.Walking || CurrentState == PlayerState.Running)
        {
            if (_playerCombat != null)
            {
                _playerCombat.TrySkillAttack();
            }
        }
    }
    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}