using UnityEngine;

public class Anim_Snap : StateMachineBehaviour
{
    PlayerAnimationController plAnimCont;
    PlayerController playerCont;

    public bool rootMotion = true;
    public Vector3 targetAdjustment = Vector3.zero;
    public float snapDuration = 0.1f;

    // Target position vars
    Vector3 targetPosition = Vector3.zero;
    Vector3 deltaToTargetPosition;
    float timer = 0f;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        plAnimCont = animator.gameObject.GetComponent<PlayerAnimationController>();
        if (plAnimCont.isRoot) { playerCont = animator.gameObject.GetComponent<PlayerController>(); }
        else { playerCont = animator.transform.parent.GetComponent<PlayerController>(); }

        if (rootMotion) { plAnimCont.EnableRootMotion(true); }

        targetPosition = plAnimCont.targetPosition;
        deltaToTargetPosition = targetPosition - playerCont.transform.position;
        //Debug.Log("Animation target position = " + targetPosition + "\n-> delta = " + deltaToTargetPosition);
        timer = 0f;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()  // Implement code that processes and affects root motion
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (timer != -1f)
        {
            if (timer < snapDuration)
            {
                playerCont.transform.position += deltaToTargetPosition * (Time.deltaTime / snapDuration);
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
