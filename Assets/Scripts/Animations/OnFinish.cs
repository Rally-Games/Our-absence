using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class OnFinish : StateMachineBehaviour
{
    [SerializeField] private string animation = "";
    [SerializeField] private float crossfade = 0.2f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var controller = animator.GetComponentInParent<IAnimationController>();
        controller?.ChangeAnimation(animation, crossfade, stateInfo.length);
    }

}
