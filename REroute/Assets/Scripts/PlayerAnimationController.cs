using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    internal Animator anim;
    bool ikActive = false;

    public Transform rightHandIKTarget;
    public Transform leftHandIKTarget;
    Transform ikTargetParent;

    Vector3 baseLocalPosition;

    bool holdingLedge = false;
    Vector3 playerVelocityOnLedgeGrab = Vector3.zero;
    Vector3 hipsDeltaPos = Vector3.zero;
    float hipsDeltaPosMaxMag = 0.4f;


    public float handIKDifference_High = -0.175f;
    public float handIKDifference_Med = -0.175f;
    public Vector3 holdLedgePosDiff_High = new Vector3(0, 0.2f, 0);
    public Vector3 holdLedgePosDiff_Med = new Vector3(0, 0, 0);
    Vector3 baseIKTargetLocalPos;
    public Vector3 holdLedgeIKPosDiff_High = new Vector3(0, 0.025f, -0.1f);
    public Vector3 holdLedgeIKPosDiff_Med = new Vector3(0, 0, 0);


    public bool holdLedgePhysicsImpact = false;


    private void Start()
    {
        anim = GetComponent<Animator>();
        baseLocalPosition = transform.localPosition;
        ikTargetParent = rightHandIKTarget.parent;
        baseIKTargetLocalPos = ikTargetParent.localPosition;
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

    public void GrabOntoLedge(float localHeight, bool high, Vector3 playerVelocity)
    {
        if (holdingLedge) { return; }
        Debug.Log("PlayerAnimationController.GrabOntoLedge()");
        // Set IK
        localHeight += (high ? handIKDifference_High : handIKDifference_Med);
        rightHandIKTarget.localPosition = new Vector3(rightHandIKTarget.localPosition.x, localHeight, rightHandIKTarget.localPosition.z);
        leftHandIKTarget.localPosition = new Vector3(leftHandIKTarget.localPosition.x, localHeight, leftHandIKTarget.localPosition.z);
        ikActive = true;

        // Set animator parameters
        holdingLedge = true;  anim.SetBool("holdingLedge", holdingLedge);
        anim.SetInteger("holdLedgeLevel", high ? 2 : 1);

        // Hips physics from impact
        //Debug.Log("player velocity on impact: " + playerVelocity);
        //hipsDeltaPos = Vector3.zero;
        //playerVelocityOnLedgeGrab = playerVelocity;

        // Adjustments
        //transform.localPosition = baseLocalPosition + (high ? holdLedgePosDiff_High : holdLedgePosDiff_Med);
        //ikTargetParent.localPosition = baseIKTargetLocalPos + (high ? holdLedgeIKPosDiff_High : holdLedgeIKPosDiff_Med);
        Debug.Log(transform.position + " + " + (transform.rotation * (high ? holdLedgePosDiff_High : holdLedgePosDiff_Med)));
        transform.position += transform.rotation * (high ? holdLedgePosDiff_High : holdLedgePosDiff_Med);
        Debug.Log(" = " + transform.position);
        ikTargetParent.localPosition = baseIKTargetLocalPos + (high ? holdLedgeIKPosDiff_High : holdLedgeIKPosDiff_Med);

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




    // During animation triggers
    public void EnableRootMotion() 
    { 
        if (anim.applyRootMotion) { return; }
        GetComponent<PlayerController>().moveCharacter = false; GetComponent<CharacterController>().enabled = false; anim.applyRootMotion = true;
    }
    public void DisableRootMotion() 
    {
        if (!anim.applyRootMotion) { return; }
        //Debug.Break();
        Vector3 rootPosDelta = transform.GetChild(0).GetChild(0).localPosition;
        anim.applyRootMotion = false;
        anim.rootPosition = Vector3.zero;
        transform.position += rootPosDelta;
        GetComponent<CharacterController>().enabled = true;
        GetComponent<PlayerController>().moveCharacter = true;
    }
}
