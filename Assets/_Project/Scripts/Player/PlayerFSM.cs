using UnityEngine;
using System.Collections;

public enum PlayerState
{
    Idle,
    Walking,
    Running,
    Dodging,
    Attacking,
    Skill,
    Blocking,
    Healing,
    Hurt
}
[RequireComponent(typeof(PlayerInputs))]
public class PlayerFSM : MonoBehaviour
{
    public PlayerState CurrentState { get; private set; }

    private PlayerInputs _playerInputs;
    private PlayerStamina _playerStamina;
    private readonly float DASH_DURATION = 0.5f;

    private void Awake()
    {
        _playerInputs = GetComponent<PlayerInputs>();
        _playerStamina = GetComponent<PlayerStamina>();
    }
    private void OnEnable()
    {
        _playerInputs.OnDodge += HandleDodgeRequest;
        _playerInputs.OnSkill += CallSkillRequest;
        _playerInputs.OnUseItem += CallHealRequest;
    }
    private void OnDisable()
    {
        _playerInputs.OnDodge -= HandleDodgeRequest;
        _playerInputs.OnSkill -= CallSkillRequest;
        _playerInputs.OnUseItem -= CallHealRequest;
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
            CurrentState == PlayerState.Healing ||
            CurrentState == PlayerState.Hurt)
        {
            return;
        }

        if (_playerInputs.InputBlock)
        {
            ChangeState(PlayerState.Blocking);
        }
        else if (_playerInputs.MoveInput.magnitude > 0.1f)
        {
            ChangeState(_playerInputs.InputRun ? PlayerState.Running : PlayerState.Walking);
        }
        else
        {
            ChangeState(PlayerState.Idle);
        }
    }
    private void HandleDodgeRequest()
    {
        if (CurrentState == PlayerState.Idle || CurrentState == PlayerState.Walking || CurrentState == PlayerState.Running || CurrentState == PlayerState.Blocking)
        {
            if (_playerStamina.TryConsumeDodgeStamina())
            {
                StartCoroutine(DodgeRoutine());
            }
        }
    }
    private void CallSkillRequest() => RequestAttackState(PlayerState.Skill);
    private void RequestAttackState(PlayerState attackState)
    {
        if (CurrentState == PlayerState.Idle || CurrentState == PlayerState.Walking || CurrentState == PlayerState.Running)
        {
            ChangeState(attackState);
        }
    }
    private void CallHealRequest()
    {
        if (CurrentState == PlayerState.Idle || CurrentState == PlayerState.Walking)
        {
            ChangeState(PlayerState.Healing);
        }
    }
    private IEnumerator DodgeRoutine()
    {
        ChangeState(PlayerState.Dodging);

        yield return new WaitForSeconds(DASH_DURATION);

        ChangeState(PlayerState.Idle);
    }
    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
    }
}