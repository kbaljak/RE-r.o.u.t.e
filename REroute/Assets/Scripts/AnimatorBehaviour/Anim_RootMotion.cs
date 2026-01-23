using UnityEngine;

public class Anim_RootMotion : StateMachineBehaviour
{
    PlayerAnimationController plAnimCont;
    PlayerController playerCont;

    public bool continuallyApplyRootPos = true;
    public bool callOnExit = true;
    public Vector3 positionAdjustment = Vector3.zero;
    public bool keepMovement = false;

    // Target position vars
    public bool adjustToTargetPosition = false;
    public float adjustToTargetPositionDuration = 1f;
    public Vector3 endRootLocalPosition = Vector3.zero;
    Vector3 targetPosition = Vector3.zero;
    Vector3 deltaToTargetPosition;
    bool endPosAdjust = false;

    float timer = 0f;
    bool update = true;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        plAnimCont = animator.gameObject.GetComponent<PlayerAnimationController>();
        if (plAnimCont.isRoot) { playerCont = animator.gameObject.GetComponent<PlayerController>(); }
        else { playerCont = animator.transform.parent.GetComponent<PlayerController>(); }
        plAnimCont.EnableRootMotion(keepMovement);
        if (adjustToTargetPosition)
        { 
            targetPosition = plAnimCont.targetPosition;
            deltaToTargetPosition = targetPosition - playerCont.transform.position; //- (playerCont.transform.position + (playerCont.transform.rotation * endRootLocalPosition));
            Debug.Log("Animation target position = " + targetPosition + "\n-> delta = " + deltaToTargetPosition);
        }
        timer = 0f;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (callOnExit) { plAnimCont.DisableRootMotion(!continuallyApplyRootPos); }
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()  // Implement code that processes and affects root motion
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!update) { return; }

        // Get current time in animation (0 -> 1 is first loop)
        timer = stateInfo.normalizedTime * stateInfo.length;
        // Only apply update if it's first loop
        if (stateInfo.normalizedTime > 1f) { return; }

        // Root motion
        if (animator.applyRootMotion && continuallyApplyRootPos)
        {
            Vector3 rootMotionFrameDeltaPosition = animator.deltaPosition;
            //animator.ApplyBuiltinRootMotion();
            playerCont.transform.position += rootMotionFrameDeltaPosition;
            animator.transform.localPosition = new Vector3(0, 0, 0);
        }

        // Adjust target position
        if (adjustToTargetPosition && !endPosAdjust)
        {
            if (timer < adjustToTargetPositionDuration)
            {
                playerCont.transform.position += deltaToTargetPosition * (Time.deltaTime / adjustToTargetPositionDuration);
                timer += Time.deltaTime;
            }
            else { playerCont.transform.position = targetPosition; endPosAdjust = true; }
        }
    }

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
