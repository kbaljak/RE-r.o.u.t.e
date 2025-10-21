using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class Anim_LedgeGrab : StateMachineBehaviour
{
    PlayerAnimationController plAnimCont;
    PlayerController playerCont;

    [Header("General")]
    public string name;
    public bool processUpdate = true;
    bool update = true;
    [Header("Root Motion")]
    public bool enableRootMotion = false;
    public bool applyRootMotion = false;
    public bool callDisableRootMotionOnEnd = false;
    [Space(10)]
    [Header("Snapping")]
    public bool enableSnapping = false;
    public bool enableSnapAtEnd = false;
    Vector3? endSnapStartPosition = null;
    public Vector3 snapTargetAdjustment = Vector3.zero;
    public float snapDuration = 0.1f;
    bool endSnapped = false;
    [Space(10)]
    [Header("Hand Inverse Kinematics")]
    public bool enableHandIK = false;
    public bool enableHandIkRotation = false;
    public float staticIkWeights = 1f;
    public bool useCurveForIkWeights = false;
    public AnimationCurve handIKWeightCurve = null;
    public Vector3 handIKAdjustment_During = Vector3.zero;
    public Vector3 handIKAdjustment_End = Vector3.zero;
    public bool continuallyAlignHandIkToEdge = false;
    [Space(10)]
    [Header("Climbing")]
    public bool climbing = false;
    public bool climbingSnap = false;
    public float speedAfterClimb = 0f;
    [Space(10)]
    [Header("Transitions")]
    public float transitionExitNormalizedTime = Mathf.Infinity;


    // Target position vars
    Ledge targetLedge = null;
    Vector3 snapTargetPosition;
    Vector3 deltaToSnapTargetPosition;
    Vector3 deltaToRootPosition = Vector3.zero;
    float timer = 0f;
    bool snappedLastFrame = false;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Debug.Log(name + ".OnStateEnter()");
        // Get references
        plAnimCont = animator.gameObject.GetComponent<PlayerAnimationController>();
        if (plAnimCont.isRoot) { playerCont = animator.gameObject.GetComponent<PlayerController>(); }
        else { playerCont = animator.transform.parent.GetComponent<PlayerController>(); }

        // 
        if (enableRootMotion) { plAnimCont.EnableRootMotion(false); }
        
        // Calculate snap target position from ledge
        targetLedge = plAnimCont.targetLedge;
        snapTargetPosition = targetLedge.transform.position + (targetLedge.transform.rotation * snapTargetAdjustment);
        deltaToSnapTargetPosition = snapTargetPosition - playerCont.transform.position;
        if (enableHandIK) { plAnimCont.SetHandIKPosition(new Vector3(0, targetLedge.transform.position.y - snapTargetPosition.y, 0) + handIKAdjustment_During, true); }
        else { plAnimCont.ResetHandIKWeights(); }
        timer = 0f;
        update = processUpdate;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Debug.Log(name + ".OnStateExit()");
        if (callDisableRootMotionOnEnd && !climbing) { plAnimCont.DisableRootMotion(); }
    }
    void OnStateExitInTransition()
    {
        //Debug.Log(name + ".OnStateExitInTransition()");
        if (enableSnapAtEnd) { playerCont.transform.position = snapTargetPosition; snappedLastFrame = true; }
        if (climbing) 
        {
            plAnimCont.DisableRootMotion();
            playerCont.ClimbedLedge(targetLedge, climbingSnap, speedAfterClimb);
        }
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()  // Implement code that processes and affects root motion
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!update) { return; }

        // Get current time in animation (0 -> 1 is first loop)
        timer = stateInfo.normalizedTime * stateInfo.length;
        // Call transition exit function when transition blend is over
        if (stateInfo.normalizedTime >= transitionExitNormalizedTime) { update = false; OnStateExitInTransition(); return; }
        // Only apply update if it's first loop
        if (stateInfo.normalizedTime > 1f) { return; }
        

        // Snapping
        if (enableSnapping)
        {
            if (timer < snapDuration)
            {
                playerCont.transform.position += deltaToSnapTargetPosition * (Time.deltaTime / snapDuration);
            }
            else if (!endSnapped)
            {
                playerCont.transform.position = snapTargetPosition;
                if (applyRootMotion) { snappedLastFrame = true; }
                endSnapped = true;
            }
        }

        // Root motion
        if (enableRootMotion && applyRootMotion && animator.applyRootMotion)
        {
            // Grab root motion delta position, don't apply it if root was snapped last frame
            Vector3 rootMotionFrameDeltaPosition = animator.deltaPosition;
            if (snappedLastFrame)
            {
                rootMotionFrameDeltaPosition = Vector3.zero;
                snappedLastFrame = false;
            }

            //animator.ApplyBuiltinRootMotion();
            playerCont.transform.position += playerCont.transform.rotation * rootMotionFrameDeltaPosition;
            if (deltaToRootPosition != Vector3.zero) { playerCont.transform.position += deltaToRootPosition * Time.deltaTime; }
            //playerCont.transform.rotation *= animator.deltaRotation;
            animator.transform.localPosition = new Vector3(0, 0, 0);
        }

        if (enableSnapAtEnd)
        {
            if (timer > stateInfo.length - snapDuration) {
                //float adjustedDeltaTime = Mathf.Min(Time.deltaTime, timer - (stateInfo.length - snapDuration));
                //playerCont.transform.position += deltaToSnapTargetPosition * (adjustedDeltaTime / snapDuration);

                if (endSnapStartPosition == null) { endSnapStartPosition = playerCont.transform.position; }
                else
                {
                    playerCont.transform.position = Vector3.Lerp(endSnapStartPosition.Value, snapTargetPosition, (timer - (stateInfo.length - snapDuration)) / snapDuration);
                }
            }
        }
    }

    // OnStateIK is called right after Animator.OnAnimatorIK() // Implement code that sets up animation IK (inverse kinematics)
    override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!update) { return; }

        // Hand IK
        if (enableHandIK)
        {
            float ikWeight = useCurveForIkWeights ? handIKWeightCurve.Evaluate(timer) : staticIkWeights;
            plAnimCont.SetHandIKWeights(ikWeight, ikWeight, enableHandIkRotation);
            if (continuallyAlignHandIkToEdge)
            { plAnimCont.SetHandIKPosition(targetLedge.transform.position + handIKAdjustment_During, false); }
            //{ plAnimCont.SetHandIKPosition(new Vector3(0, targetLedge.transform.position.y - snapTargetPosition.y, 0) + handIKAdjustment_During, true); }
        }
    }
}


[CustomEditor(typeof(Anim_LedgeGrab))]
public class Anim_LedgeGrab_Inspector : Editor
{
    Anim_LedgeGrab ledgeGrab;

    UnityEditor.Animations.StateMachineBehaviourContext[] context;
    float clipLength = 0f;
    List<AnimatorStateTransition> transitions = new List<AnimatorStateTransition>();

    public void OnEnable()
    {
        context = UnityEditor.Animations.AnimatorController.FindStateMachineBehaviourContext(target as StateMachineBehaviour);

        if (context != null)
        {
            // animatorObject can be an AnimatorState or AnimatorStateMachine
            UnityEditor.Animations.AnimatorState state = context[0].animatorObject as UnityEditor.Animations.AnimatorState;
            if (state != null)
            {
                Anim_LedgeGrab behaviour = target as Anim_LedgeGrab;
                clipLength = state.motion.averageDuration;
                foreach (AnimatorStateTransition t in state.transitions)
                { transitions.Add(t); }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ledgeGrab = (Anim_LedgeGrab)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("---- State info");
        EditorGUILayout.LabelField("Motion length:", clipLength.ToString());
        for (int i = 0; i < transitions.Count; i++)
        {
            EditorGUILayout.LabelField("#" + i.ToString() + "\ttransition end time:", (clipLength * transitions[i].exitTime).ToString());
        }
        
    }
}
