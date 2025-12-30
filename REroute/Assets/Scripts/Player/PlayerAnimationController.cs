using System.Collections;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    internal Animator anim;
    PlayerController playerCont;

    public bool isRoot = true;

    public Transform rightHandIKTarget;
    public Transform leftHandIKTarget;
    Transform ikTargetParent;
    float handIkBaseX;
    public Vector2 handIkPosWeights = Vector2.zero;
    public Vector2 handIkRotWeights = Vector2.zero;

    Vector3 baseLocalPosition;

    //internal Ledge targetLedge = null;
    internal Vector3 targetPosition = Vector3.zero;
    Vector3 playerVelocityOnLedgeGrab = Vector3.zero;
    Vector3 hipsDeltaPos = Vector3.zero;
    float hipsDeltaPosMaxMag = 0.4f;


    public float handIKDifference_High = -0.175f;
    public float handIKDifference_Med = -0.175f;
    public Vector3 holdLedgePosDiff_High = new Vector3(0, 0.2f, 0);
    public Vector3 holdLedgePosDiff_Med = new Vector3(0, 0, 0);
    public Vector3 holdLedgeIKPosDiff_High = new Vector3(0, 0.025f, -0.1f);
    public Vector3 holdLedgeIKPosDiff_Med = new Vector3(0, 0, 0);

    public bool holdLedgePhysicsImpact = false;


    //// Animation params
    public float ap_standToHang_animHeight = 2.66f;
    public float ap_standToBraceHang_animHeight = 2.58f;


    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.applyRootMotion = false;

        handIkBaseX = rightHandIKTarget.localPosition.x;
    }
    private void Start()
    {
        if (isRoot) { playerCont = GetComponent<PlayerController>(); }
        else { playerCont = transform.parent.GetComponent<PlayerController>(); }
        baseLocalPosition = transform.localPosition;
        ikTargetParent = rightHandIKTarget.parent;
    }
    private void Update()
    {
        // Grab ledge impact physics visualization
        if (holdLedgePhysicsImpact)
        {
            if (playerCont.playerParkour.holdingLedge)
            {
                if (playerVelocityOnLedgeGrab != Vector3.zero)
                {
                    Vector3 frameDeltaPos = playerVelocityOnLedgeGrab * Time.deltaTime;
                    hipsDeltaPos = Vector3.ClampMagnitude(hipsDeltaPos + frameDeltaPos, hipsDeltaPosMaxMag);
                    transform.localPosition = baseLocalPosition + hipsDeltaPos;
                    if (hipsDeltaPos.magnitude >= hipsDeltaPosMaxMag) { playerVelocityOnLedgeGrab = Vector3.zero; }
                    else { playerVelocityOnLedgeGrab -= (playerVelocityOnLedgeGrab * 0.5f * Time.deltaTime); }
                }
                else if (hipsDeltaPos != Vector3.zero)
                {
                    Vector3 frameDeltaPos = -hipsDeltaPos.normalized * Mathf.Clamp(2f * Time.deltaTime, 0f, hipsDeltaPos.magnitude); //Vector3.Lerp(hipsDeltaPos, Vector3.zero, 0.1f * Time.deltaTime);
                    hipsDeltaPos += frameDeltaPos;
                    transform.localPosition = baseLocalPosition + hipsDeltaPos;
                }
            }
        }
    }

    // Hand IK
    void OnAnimatorIK()
    {
        anim.SetIKPositionWeight(AvatarIKGoal.RightHand, handIkPosWeights.y);
        anim.SetIKRotationWeight(AvatarIKGoal.RightHand, handIkRotWeights.y);
        anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, handIkPosWeights.x);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, handIkRotWeights.x);

        if (handIkPosWeights.x > 0f)
        {
            anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIKTarget.position);
        }
        if (handIkPosWeights.y > 0f)
        {
            anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position);
        }

        /*if (handIkRotWeights.x > 0f)
        {
            anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
        }
        if (handIkRotWeights.y > 0f)
        {
            anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
        }*/
    }
    public void SetHandIKWeights(float leftHand, float rightHand, bool includeRotation = false)
    {
        handIkPosWeights.x = leftHand; handIkPosWeights.y = rightHand;
        if (includeRotation) { handIkRotWeights.x = leftHand == 0 ? 0 : 1; handIkRotWeights.y = rightHand == 0 ? 0 : 1; }
    }
    public void ResetHandIKWeights()
    {
        handIkPosWeights.x = 0; handIkPosWeights.y = 0;
        handIkRotWeights.x = 0; handIkRotWeights.y = 0;
        anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
        anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
    }
    public void SetHandIkRotation(Vector3 eulerAngles)
    {
        anim.SetIKRotation(AvatarIKGoal.LeftHand, transform.rotation * Quaternion.Euler(eulerAngles));
        anim.SetIKRotation(AvatarIKGoal.RightHand, transform.rotation * Quaternion.Euler(eulerAngles));
    }
    public bool IsHandIKActive() => handIkPosWeights != Vector2.zero;


    public void GrabOntoLedge(Ledge ledge, LedgeLevel ledgeLvl)
    {
        //if (playerCont.plParkourDet.holdingLedge) { return; }
        Debug.Log("PlayerAnimationController.GrabOntoLedge()");

        // Set target position
        //targetPosition = ledgeTransform.position + (ledgeTransform.rotation * (high ? holdLedgePosDiff_High : holdLedgePosDiff_Med));
        //targetLedge = ledge;

        // Set IK
        //localHeight += (high ? handIKDifference_High : handIKDifference_Med);
        rightHandIKTarget.localPosition = new Vector3(rightHandIKTarget.localPosition.x, 0, 0);
        leftHandIKTarget.localPosition = new Vector3(leftHandIKTarget.localPosition.x, 0, 0);
        //ikTargetParent.localPosition = new Vector3(0, ledge.transform.position.y - targetPosition.y, 0); //+ (high ? holdLedgeIKPosDiff_High : holdLedgeIKPosDiff_Med);
        //ikActive = true;

        // Set animator parameters
        anim.SetBool("holdingLedge", playerCont.playerParkour.holdingLedge);
        anim.SetInteger("holdLedgeLevel", (int)ledgeLvl);

        // Hips physics from impact
        //playerVelocity = plCont.GetCurrentVelocity();
        //Debug.Log("player velocity on impact: " + playerVelocity);
        //hipsDeltaPos = Vector3.zero;
        //playerVelocityOnLedgeGrab = playerVelocity;
    }
    public void DropOffLedge()
    {
        //handIKActive = false;
        anim.SetBool("holdingLedge", playerCont.playerParkour.holdingLedge);

        playerVelocityOnLedgeGrab = Vector3.zero;
        hipsDeltaPos = Vector3.zero;
    }


    //// External animation behviour
    public void SetHandIKPosition(Vector3 value, bool local = true)
    {
        if (local) { ikTargetParent.localPosition = value; }
        else { ikTargetParent.position = value; }
    }
    public void SetHandIKPositionDelta(Vector3 adjustment, bool local = true) 
    { 
        if (local) { ikTargetParent.localPosition += adjustment; }
        else {  ikTargetParent.position += adjustment;}
    }
    public void SetHandIKPositionDeltaX(float value)
    {
        rightHandIKTarget.localPosition = new Vector3(handIkBaseX + value, rightHandIKTarget.localPosition.y, rightHandIKTarget.localPosition.z);
        leftHandIKTarget.localPosition = new Vector3(-handIkBaseX - value, rightHandIKTarget.localPosition.y, rightHandIKTarget.localPosition.z);
    }


    //// Delayed
    IEnumerator LandingAnimationDoneRootMotionSync()
    {
        yield return new WaitUntil(() => !anim.applyRootMotion);
        playerCont.followCameraRotation = true;
    }



    //// External animation event calls
    public void EnableRootMotion(bool keepMovement = false)
    {
        //Debug.Log("EnableRootMotion()");
        //if (anim.applyRootMotion) { return; }
        playerCont.AnimationSolo(true, keepMovement);  //playerCont.RootMotionMovement(true, keepMovement);
        anim.applyRootMotion = true;
    }
    public void DisableRootMotion(bool updatePositions = true)
    {
        Debug.Log("DisableRootMotion()");
        //if (!anim.applyRootMotion) { return; }
        Vector3 rootPosition = transform.localPosition;
        anim.applyRootMotion = false;
        if (updatePositions)
        {
            if (isRoot) { transform.position = rootPosition; }
            else { transform.parent.position += transform.rotation * rootPosition; }
        }
        transform.localPosition = Vector3.zero; //anim.rootPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        playerCont.AnimationSolo(false);  //playerCont.RootMotionMovement(false);
    }
    public void DisableRootMotion_AnimEvent() => DisableRootMotion(true);
    public void LandingAnimationDone()
    {
        Debug.Log("LandingAnimationDone()");
        playerCont.AnimationSolo(false);
        StartCoroutine(LandingAnimationDoneRootMotionSync());
    }
    public void ClimbingFinished_AnimEvent()
    {
        Debug.Log("ClimbingFinished_AnimEvent()");

        playerCont.ClimbedLedge();
    }
    public void SlideStart_AnimEvent() { playerCont.SlideStart(); }
    public void SlideEnd_AnimEvent() { playerCont.SlideEnd(); }
}
