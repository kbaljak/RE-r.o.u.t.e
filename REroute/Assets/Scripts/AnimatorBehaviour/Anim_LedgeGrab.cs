using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class Anim_LedgeGrab : StateMachineBehaviour
{
    PlayerAnimationController plAnimCont;
    PlayerController playerCont;
    PlayerParkourDetection playerParkour;

    [Header("General")]
    public string name;
    public bool processUpdate = true;
    bool update = false;
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
    public Vector3 snapDurationPerAxis = Vector3.zero; float maxSnapDuration = 0;
    bool endSnapped = false;
    [Space(10)]
    [Header("Hand Inverse Kinematics")]
    public bool enableHandIK = false;
    //public bool enableHandIkRotation = false;
    public float staticIkWeights = 1f;
    public bool useCurveForIkWeights = false;
    public AnimationCurve handIKWeightCurve = null;
    public Vector3 handIKAdjustment_During = Vector3.zero;
    public Vector3 handIKAdjustment_End = Vector3.zero;
    public bool continuallyAlignHandIkToEdge = false;
    public bool enableHandIkRotation = false;
    public Vector3 handIkRotation = Vector3.zero;
    public bool disableIkOnEnd = false;
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
    Vector3 targetGrabPoint;
    Vector3 snapTargetPosition;
    Vector3 deltaToSnapTargetPosition;
    Vector3 deltaToRootPosition = Vector3.zero;
    float timer = 0f;
    bool snappedLastFrame = false;



    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Debug.Log(name + ".OnStateEnter()");

        // Get main references
        if (plAnimCont == null)
        {
            plAnimCont = animator.gameObject.GetComponent<PlayerAnimationController>();
            if (plAnimCont.isRoot) { playerCont = animator.gameObject.GetComponent<PlayerController>(); }
            else { playerCont = animator.transform.parent.GetComponent<PlayerController>(); }
            playerParkour = playerCont.playerParkour;
        }
        if (!playerCont.IsOwner) { update = false; return; }

        // Get other references
        targetLedge = playerParkour.targetLedge;
        float targetGrabXDelta = playerParkour.targetGrabXDelta.Value;

        // 
        if (enableRootMotion) { plAnimCont.EnableRootMotion(false); }
        /*if (enableHandIK)
        {
            plAnimCont.SetHandIKPosition(new Vector3(0, targetLedge.transform.position.y - snapTargetPosition.y, 0) + new Vector3(0, handIKAdjustment_During.y, handIKAdjustment_During.z), true);
            plAnimCont.SetHandIKPositionDeltaX(handIKAdjustment_During.x);
            if (enableHandIkRotation) { plAnimCont.SetHandIkRotation(handIkRotation); }
        }*/
        else { plAnimCont.ResetHandIKWeights(); }

        // Calculate snap target position from ledge
        targetGrabPoint = targetLedge.transform.position + (targetLedge.transform.right * targetGrabXDelta);
        snapTargetPosition = targetGrabPoint + targetLedge.transform.TransformVector(snapTargetAdjustment);
        //(targetLedge.transform.rotation * snapTargetAdjustment);    // targetLedge.transform.position + ...
        deltaToSnapTargetPosition = snapTargetPosition - playerCont.transform.position;

        if (snapDurationPerAxis != Vector3.zero) { maxSnapDuration = Mathf.Max(snapDurationPerAxis.x, snapDurationPerAxis.y, snapDurationPerAxis.z); }
        
        timer = 0f;
        update = processUpdate;
        endSnapped = false;
    }



    // OnState Update is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!update) { return; }

        // Get current time in animation (0 -> 1 is first loop)
        timer = stateInfo.normalizedTime * stateInfo.length;
        // Call transition exit function when transition blend is over
        if (stateInfo.normalizedTime >= transitionExitNormalizedTime) { update = false; OnStateExitInTransition(); return; }
    }



    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!playerCont.IsOwner) { return; }


        //Debug.Log(name + ".OnStateExit()");
        if (callDisableRootMotionOnEnd && !climbing) { plAnimCont.DisableRootMotion(); }
    }
    void OnStateExitInTransition()
    {
        Debug.Log(name + ".OnStateExitInTransition()");
        if (disableIkOnEnd) { plAnimCont.ResetHandIKWeights(); }
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
        //if (stateInfo.normalizedTime >= transitionExitNormalizedTime) { update = false; OnStateExitInTransition(); return; }
        // Only apply update if it's first loop
        if (stateInfo.normalizedTime > 1f) { return; }
        

        // Snapping
        if (enableSnapping)
        {
            if (snapDurationPerAxis != Vector3.zero)
            {
                if (!endSnapped)
                {
                    if (timer >= maxSnapDuration)
                    {
                        //playerCont.transform.position = snapTargetPosition;
                        //if (applyRootMotion) { snappedLastFrame = true; }
                        endSnapped = true;
                    }
                    else
                    {
                        for (int a = 0; a < 3; ++a)
                        {
                            if (timer < snapDurationPerAxis[a])
                            {
                                Vector3 axis = Vector3.zero; axis[a] = 1;
                                playerCont.transform.position += axis * deltaToSnapTargetPosition[a] * (Time.deltaTime / snapDurationPerAxis[a]);
                            }
                        }
                    }
                }
            }
            else
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
            playerCont.transform.position += rootMotionFrameDeltaPosition;
            //if (deltaToRootPosition != Vector3.zero) { playerCont.transform.position += deltaToRootPosition * Time.deltaTime; }
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
            float ikWeight = useCurveForIkWeights ? handIKWeightCurve.Evaluate(stateInfo.normalizedTime % 1.0f) : staticIkWeights;
            if (ikWeight > 0)
            {
                plAnimCont.SetHandIKWeights(ikWeight, ikWeight, enableHandIkRotation);
                if (enableHandIkRotation) { plAnimCont.SetHandIkRotation(handIkRotation); }
                if (continuallyAlignHandIkToEdge)
                {
                    plAnimCont.SetHandIKPosition(targetGrabPoint + new Vector3(0, handIKAdjustment_During.y, handIKAdjustment_During.z), false);
                    plAnimCont.SetHandIKPositionDeltaX(handIKAdjustment_During.x);
                }
                //{ plAnimCont.SetHandIKPosition(new Vector3(0, targetLedge.transform.position.y - snapTargetPosition.y, 0) + handIKAdjustment_During, true); }
            }
            else { plAnimCont.ResetHandIKWeights(); }
        }
        else if (plAnimCont.IsHandIKActive()) { plAnimCont.ResetHandIKWeights(); }
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
            if (context.Length > 0)
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
