using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Anim_Sliding : StateMachineBehaviour
{
    PlayerAnimationController plAnimCont;
    PlayerController playerCont;

    public bool startState = false;
    public bool endState = false;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        plAnimCont = animator.gameObject.GetComponent<PlayerAnimationController>();
        if (plAnimCont.isRoot) { playerCont = animator.gameObject.GetComponent<PlayerController>(); }
        else { playerCont = animator.transform.parent.GetComponent<PlayerController>(); }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (startState) { playerCont.SlideStartDone(); }
        else if (endState) { playerCont.SlideEndDone(); }
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()  // Implement code that processes and affects root motion
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
