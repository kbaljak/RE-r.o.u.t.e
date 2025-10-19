using UnityEngine;

public class Anim_RootMotion : StateMachineBehaviour
{
    PlayerAnimationController plAnimCont;
    PlayerController playerCont;

    public bool continuallyApplyRootPos = true;
    public bool callOnExit = true;
    public Vector3 positionAdjustment = Vector3.zero;

    // Target position vars
    public bool adjustToTargetPosition = false;
    public float adjustToTargetPositionDuration = 1f;
    public Vector3 endRootLocalPosition = Vector3.zero;
    Vector3 targetPosition = Vector3.zero;
    Vector3 deltaToTargetPosition;
    float timer = 0f;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        plAnimCont = animator.gameObject.GetComponent<PlayerAnimationController>();
        if (plAnimCont.isRoot) { playerCont = animator.gameObject.GetComponent<PlayerController>(); }
        else { playerCont = animator.transform.parent.GetComponent<PlayerController>(); }
        plAnimCont.EnableRootMotion(true);
        if (adjustToTargetPosition) 
        { 
            targetPosition = plAnimCont.targetPosition;
            deltaToTargetPosition = targetPosition - playerCont.transform.position; //- (playerCont.transform.position + (playerCont.transform.rotation * endRootLocalPosition));
            Debug.Log("Animation target position = " + targetPosition + "\n-> delta = " + deltaToTargetPosition);
            timer = 0f;
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (callOnExit) { plAnimCont.DisableRootMotion(!continuallyApplyRootPos); }
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()  // Implement code that processes and affects root motion
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.applyRootMotion && continuallyApplyRootPos)
        {
            //animator.ApplyBuiltinRootMotion();
            animator.transform.localPosition = positionAdjustment + animator.deltaPosition;
            playerCont.transform.position += playerCont.transform.rotation * new Vector3(animator.transform.localPosition.x, 0, animator.transform.localPosition.z);
            animator.transform.localPosition = new Vector3(0, animator.transform.localPosition.y, 0);
        }
        if (adjustToTargetPosition && timer != -1f)
        {
            if (timer < adjustToTargetPositionDuration)
            {
                playerCont.transform.position += deltaToTargetPosition * (Time.deltaTime / adjustToTargetPositionDuration);
                timer += Time.deltaTime;
            }
            else if (timer != -1f) { playerCont.transform.position = targetPosition; timer = -1f; }
        }
    }

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
