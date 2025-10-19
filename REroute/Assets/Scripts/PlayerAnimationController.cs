using System.Collections;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    internal Animator anim;
    PlayerController playerCont;
    bool ikActive = false;

    public bool isRoot = true;

    public Transform rightHandIKTarget;
    public Transform leftHandIKTarget;
    Transform ikTargetParent;

    Vector3 baseLocalPosition;

    bool holdingLedge = false;
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
            if (holdingLedge)
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

    void OnAnimatorIK()
    {
        if (ikActive)
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position);
            //anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);

            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIKTarget.position);
            //anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            anim.SetLookAtWeight(0);
        }
    }

    public void GrabOntoLedge(Transform ledgeTransform, bool high, Vector3 playerVelocity)
    {
        if (holdingLedge) { return; }
        Debug.Log("PlayerAnimationController.GrabOntoLedge()");

        // Set target position
        targetPosition = ledgeTransform.position + (ledgeTransform.rotation * (high ? holdLedgePosDiff_High : holdLedgePosDiff_Med));

        // Set IK
        //localHeight += (high ? handIKDifference_High : handIKDifference_Med);
        rightHandIKTarget.localPosition = new Vector3(rightHandIKTarget.localPosition.x, 0, 0);
        leftHandIKTarget.localPosition = new Vector3(leftHandIKTarget.localPosition.x, 0, 0);
        ikTargetParent.localPosition = new Vector3(0, ledgeTransform.position.y - targetPosition.y, 0) + (high ? holdLedgeIKPosDiff_High : holdLedgeIKPosDiff_Med);
        ikActive = true;

        // Set animator parameters
        holdingLedge = true; anim.SetBool("holdingLedge", holdingLedge);
        anim.SetInteger("holdLedgeLevel", high ? 2 : 1);

        // Hips physics from impact
        //Debug.Log("player velocity on impact: " + playerVelocity);
        //hipsDeltaPos = Vector3.zero;
        //playerVelocityOnLedgeGrab = playerVelocity;
    }
    public void DropOffLedge()
    {
        ikActive = false;
        holdingLedge = false; anim.SetBool("holdingLedge", holdingLedge);

        playerVelocityOnLedgeGrab = Vector3.zero;
        hipsDeltaPos = Vector3.zero;
    }
    public void Landed()
    {
        //transform.localPosition = baseLocalPosition;
    }


    IEnumerator LandingAnimationDoneRootMotionSync()
    {
        yield return new WaitUntil(() => !anim.applyRootMotion);
        playerCont.followCameraRotation = true;
    }



    //// External animation calls
    public void EnableRootMotion(bool keepMovement = false)
    {
        //Debug.Log("EnableRootMotion()");
        //if (anim.applyRootMotion) { return; }
        playerCont.RootMotionMovement(true, keepMovement);
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
        playerCont.RootMotionMovement(false);
    }
    public void DisableRootMotion_AnimEvent() => DisableRootMotion(true);
    public void LandingAnimationDone()
    {
        Debug.Log("LandingAnimationDone()");
        StartCoroutine(LandingAnimationDoneRootMotionSync());
    }
}
