using UnityEngine;

public class Anim_CameraEffect : StateMachineBehaviour
{
    PlayerAnimationController plAnimCont;
    PlayerCameraController plCamCont;

    public float normalizedTiming = 0f;
    //public float effectStrength = 1f;
    public Vector3 effectDelta = Vector3.zero;
    public float effectDuration = 1f;

    bool played = false;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (plAnimCont == null)
        {
            plAnimCont = animator.GetComponent<PlayerAnimationController>();
            if (plAnimCont.isRoot) { plCamCont = animator.gameObject.GetComponent<PlayerController>().playerCamera; }
            else { plCamCont = animator.transform.parent.GetComponent<PlayerController>().playerCamera; }
        }

        if (normalizedTiming == 0f) { plCamCont.PositionEffect(effectDelta, effectDuration); }
        else { played = false; }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!played)
        {
            float time = stateInfo.normalizedTime % 1f;
            if (time > normalizedTiming)
            {
                plCamCont.PositionEffect(effectDelta, effectDuration);
                played = true;
            }
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

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
