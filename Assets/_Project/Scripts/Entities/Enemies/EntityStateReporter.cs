using UnityEngine;
public enum EntityActionStateType { Attack, Hurt, Agonize, Death }
public class EntityStateReporter : StateMachineBehaviour
{
    public EntityActionStateType actionType;
    private EntityAnimator _entityAnimator;
    private bool _hasExited;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_entityAnimator == null)
        {
            _entityAnimator = animator.GetComponent<EntityAnimator>();
            if (_entityAnimator == null)
                _entityAnimator = animator.GetComponentInParent<EntityAnimator>();
        }

        _hasExited = false;

        if (_entityAnimator != null)
            _entityAnimator.OnAnimationStateEnter(actionType);
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (actionType == EntityActionStateType.Death && !_hasExited)
        {
            if (stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(layerIndex))
            {
                _hasExited = true;
                if (_entityAnimator != null)
                    _entityAnimator.OnAnimationStateExit(actionType);
            }
        }
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_entityAnimator != null && !_hasExited)
        {
            _hasExited = true;
            _entityAnimator.OnAnimationStateExit(actionType);
        }
    }
}