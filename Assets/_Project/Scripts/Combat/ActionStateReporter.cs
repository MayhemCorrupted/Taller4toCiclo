using UnityEngine;
public enum ActionStateType { Attack, Dodge, Skill, Hurt, Knockout, Death }
public class ActionStateReporter : StateMachineBehaviour
{
    public ActionStateType actionType;
    private PlayerAnimator _playerAnimator;
    private bool _hasExited;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_playerAnimator == null)
        {
            _playerAnimator = animator.GetComponent<PlayerAnimator>();
        }
        _hasExited = false;
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_playerAnimator != null && !_hasExited) 
        {
            _hasExited = true;
            _playerAnimator.OnAnimationStateExit(actionType);
        }
    }
}
