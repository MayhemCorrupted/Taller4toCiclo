using System;
using UnityEngine;
public enum PlayerState { Idle, Walking, Running, Dodging, Attacking, Skill, Blocking, UsingItem, Hurt, Knockout, Death }
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
    private void Awake()
    {
        _playerInputs = GetComponent<PlayerInputs>();
        _playerStamina = GetComponent<PlayerStamina>();
        _playerCombat = GetComponent<PlayerCombat>();
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
    private void StateEvaluations()
    {
        if (CurrentState == PlayerState.Attacking ||
            CurrentState == PlayerState.Dodging ||
            CurrentState == PlayerState.Skill ||
            CurrentState == PlayerState.UsingItem ||
            CurrentState == PlayerState.Hurt ||
            CurrentState == PlayerState.Knockout ||
            CurrentState == PlayerState.Death)
        {
            return;
        }

        if (_playerInputs.InputBlock)
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