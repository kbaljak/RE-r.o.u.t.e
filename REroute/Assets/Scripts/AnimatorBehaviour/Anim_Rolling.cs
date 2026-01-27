using UnityEngine;

public class Anim_Rolling : StateMachineBehaviour
{
    PlayerAnimationController plAnimCont;
    PlayerController playerCont;

    public bool continuallyApplyRootPos = true;
    public bool callDisableRootOnExit = true;
    public bool callLandingAnimDoneOnExit = false;

    // void Awake()
    // {
    //     if (!playerCont.IsOwner) { return; }
    // }
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        plAnimCont = animator.gameObject.GetComponent<PlayerAnimationController>();
        if (plAnimCont.isRoot) { playerCont = animator.gameObject.GetComponent<PlayerController>(); }
        else { playerCont = animator.transform.parent.GetComponent<PlayerController>(); }
        plAnimCont.EnableRootMotion(true);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!playerCont.isGrounded)
        {
            //Debug.Log("Roll break!");
            //animator.SetTrigger("break");
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (callDisableRootOnExit) { plAnimCont.DisableRootMotion(!continuallyApplyRootPos); }
        if (callLandingAnimDoneOnExit) { plAnimCont.LandingAnimationDone(); }
        animator.ResetTrigger("break");
        playerCont.AddScoreAfterRoll();
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()  // Implement code that processes and affects root motion
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.applyRootMotion && continuallyApplyRootPos)
        {
            //Vector3 rootMotionFrameDeltaPosition = animator.deltaPosition;
            //playerCont.transform.position += rootMotionFrameDeltaPosition;
            //animator.transform.localPosition = new Vector3(0, 0, 0);

            animator.ApplyBuiltinRootMotion();
            playerCont.transform.position += playerCont.transform.rotation * new Vector3(animator.transform.localPosition.x, 0, animator.transform.localPosition.z);
            animator.transform.localPosition = new Vector3(0, animator.transform.localPosition.y, 0);
        }
    }

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
