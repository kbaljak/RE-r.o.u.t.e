using System.Numerics;
using UnityEngine;

public class Anim_RunningAnimSpeed : StateMachineBehaviour
{
    public float speedFactor = 0.2f;
    PlayerController playerController;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerAnimationController plAnimCont = animator.gameObject.GetComponent<PlayerAnimationController>();
        if (plAnimCont.isRoot) { playerController = animator.gameObject.GetComponent<PlayerController>(); }
        else { playerController = animator.transform.parent.GetComponent<PlayerController>(); }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float playerSpeed = playerController.moveSpeed; //animator.gameObject.GetComponent<PlayerController>().moveSpeed;
        float runStartSpeed = playerController.runStartSpeed;  //animator.gameObject.GetComponent<PlayerController>().runStartSpeed;
        float runMaxSpeed = playerController.runMaxSpeed;  //animator.gameObject.GetComponent<PlayerController>().runMaxSpeed;
        if (playerSpeed > runStartSpeed)
        {
            animator.speed = 1 + ((playerSpeed - runStartSpeed) / (runMaxSpeed - runStartSpeed) * speedFactor);
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1;
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
