using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    CharacterController charCont;
    public PlayerAnimationController plAnimCont;
    public PlayerClimbTrigger plClmbTrig;
    public GameObject playerCamera;

    // Input
    InputAction moveAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction attackAction;
    InputAction crouchAction;

    /// Movement
    public bool moveCharacter = true;
    public bool canControlMove = true;
    public Transform groundRaycastPoint;
    public bool isGrounded = false; 
    // Predict landing for roll input
    public bool groundPredicted = false;
    public float predictionTime = 0.1f;
    public bool roll = false;

    public Vector3 groundSlopeNormal = Vector3.up;
    float groundFriction = 1f;
    // Player velocities
    //public Vector3 playerVelocity = Vector3.zero;
    public float moveSpeed = 0f;
    public Vector3 moveDirection = Vector3.forward;
    bool? backwardMovement = null;
    public float fallSpeed = 0f;

    bool followCameraRotation = true;
    // Params
    public float walkSpeed = 2f;
    public float runStartSpeed = 5f;
    public float runMaxSpeed = 12f;
    public float runStartAccelTime = 0.5f;
    public float runAccelTime = 10f;
    public float fullDecelTime = 1f;
    [Tooltip("Jump height in meters.")]
    public float jumpHeight = 0.7f;

    /// Climbing
    public bool holdingLedge = false;
    public bool tryGrabLedge = true;
    public float hardLandingVelThreshold = 2f;

    public Transform head;
    public Transform climbTriggersT;
    float climbTriggersBaseLocalPosZ;

    // Debug
    public float moveSpeedSigned = 0f;
    public float moveAngle = 0f;


    private void Start()
    {
        charCont = GetComponent<CharacterController>();
        // Input
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        // Other
        climbTriggersBaseLocalPosZ = climbTriggersT.localPosition.z;
    }


    private void Update()
    {
        Update_FaceCamera();

        if (holdingLedge)
        {
            Update_Climbing();
        }
        else
        {
            Update_FootMovement();
        }

        Update_ForwardPosDelta();
        Update_ClimbableDetect();
        Update_AnimatorParams();
    }
    void Update_FaceCamera()
    {
        if (followCameraRotation)
        {
            Quaternion targetRotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, playerCamera.transform.eulerAngles.y, transform.eulerAngles.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * 1.2f * Time.deltaTime);
            //Quaternion.Euler(transform.eulerAngles) * Quaternion.AngleAxis(360f * Time.deltaTime, transform.up);
            //transform.eulerAngles = new Vector3(0, playerCamera.transform.eulerAngles.y, 0);
        }
    }
    void Update_FootMovement()
    {
        if (!moveCharacter) { return; }
        Update_Grounded();
        //// Movement
        Vector3 cameraForward = Vector3.ProjectOnPlane(new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z), groundSlopeNormal);
        Vector3 cameraRight = Vector3.ProjectOnPlane(new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z), groundSlopeNormal);
        if (canControlMove)
        {
            // Get vectors
            Vector2 input = moveAction.ReadValue<Vector2>();
            // Get forward and right direction for movement
            Vector3 frameMoveDir = (cameraRight * input.x) + (cameraForward * input.y).normalized;
            // Get angles
            float angleMoveToVel = Vector3.Angle(frameMoveDir, moveDirection);
            float angleLookToVel = Vector3.Angle(cameraForward, moveDirection);
            if (isGrounded && fallSpeed >= 0)
            {
                if (fallSpeed > 0) { fallSpeed = 0; }

                // no input -> slow down to stand still
                if (input == Vector2.zero)
                {
                    if (moveSpeed > 0)
                    {
                        float factor = ((runMaxSpeed / fullDecelTime) * Time.deltaTime) * groundFriction;
                        moveSpeed = Mathf.Lerp(moveSpeed, 0f, factor);
                    }
                }
                // otherwise -> accelerate
                else
                {
                    // Movement start -> check direction
                    backwardMovement = input.y < 0;

                    bool sprinting = sprintAction.IsInProgress();

                    // if walking -> walk speed + directly apply direction
                    if (!sprinting)
                    {
                        if (moveSpeed > walkSpeed)
                        {
                            float factor = ((runMaxSpeed / fullDecelTime) * Time.deltaTime) * groundFriction;
                            moveSpeed = Mathf.Lerp(moveSpeed, 0f, factor);
                        }
                        else { moveSpeed = walkSpeed; moveDirection = frameMoveDir; }
                    }
                    // if sprinting -> accelerate
                    else
                    {
                        float accelFactor = moveSpeed < runStartSpeed ? (runStartSpeed / runStartAccelTime) : ((runMaxSpeed - runStartSpeed) / runAccelTime);
                        float angleToVelFactor = Mathf.Clamp(angleMoveToVel / 90f, 0f, 1f);
                        float curMaxSpeed = input.y > 0 ? runMaxSpeed : runStartSpeed;

                        // Just redirect speed
                        if (angleMoveToVel < 4f)
                        {
                            moveDirection = frameMoveDir;
                            moveSpeed = Mathf.Lerp(moveSpeed, curMaxSpeed, accelFactor * Time.deltaTime);
                        }
                        // Slow down speed depending on angle and smoothly change direction
                        else if (angleMoveToVel < 90f)
                        {
                            // Slow down
                            moveSpeed = Mathf.Lerp(moveSpeed, runStartSpeed, angleToVelFactor * Time.deltaTime);
                            // Change direction
                            //     runstartspeed -> 0.2f, maxspeed -> 0.8f, < run start speed -< 0.05f
                            float timeToTurn;
                            if (moveSpeed < runStartSpeed) { timeToTurn = 0.05f; }
                            else { timeToTurn = 0.1f + (((moveSpeed - runStartSpeed) / (runMaxSpeed - runStartSpeed)) * 0.3f); }
                            moveDirection = Vector3.RotateTowards(moveDirection, frameMoveDir,
                                (Mathf.Deg2Rad * angleMoveToVel) * Time.deltaTime * (1f / timeToTurn), 0f).normalized;
                        }
                        // Quick brake
                        else
                        {
                            // Brake
                            moveSpeed = Mathf.Lerp(moveSpeed, 0f, 6f * Time.deltaTime);
                            // Change direction only if speed low enough
                            if (moveSpeed <= walkSpeed)
                            {
                                float timeToTurn = 0.1f * (moveSpeed / runStartSpeed);
                                moveDirection = Vector3.RotateTowards(moveDirection, frameMoveDir,
                                    (Mathf.Deg2Rad * angleMoveToVel) * Time.deltaTime * (1f / timeToTurn), 0f).normalized;
                            }
                        }
                    }
                }

                // Jump
                if (jumpAction.WasPerformedThisFrame()) { Jump(); }
            }
            // Gravity
            fallSpeed -= Physics.gravity.y * Time.deltaTime;
        }
        
        // Update stats
        moveSpeedSigned = moveSpeed;
        if (moveSpeed > 0) { moveSpeedSigned *= (backwardMovement != null && backwardMovement.Value ? -1 : 1); }  //angleLookToVel > 90f
        moveAngle = Vector3.SignedAngle(cameraForward, moveDirection, groundSlopeNormal);
        if (backwardMovement != null && backwardMovement.Value) { moveAngle = (180f - Mathf.Abs(moveAngle)) * Mathf.Sign(moveAngle); }

        // Combine velocities
        Vector3 frameVelocity = moveDirection * moveSpeed;
        frameVelocity += fallSpeed * -Vector3.up;
        // Execute movement
        charCont.Move(frameVelocity * Time.deltaTime);
    }
    void Update_ForwardPosDelta()
    {
        Vector3 headLocalPos = transform.InverseTransformPoint(head.position);
        float headForwDeltaPos = headLocalPos.z;
        charCont.center = new Vector3(charCont.center.x, charCont.center.y, headForwDeltaPos);
        climbTriggersT.localPosition = new Vector3(climbTriggersT.localPosition.x, climbTriggersT.localPosition.y, climbTriggersBaseLocalPosZ + headForwDeltaPos);
    }
    void Update_ClimbableDetect()
    {
        bool jumpPressed = jumpAction.IsInProgress();
        if (!tryGrabLedge && jumpPressed) { tryGrabLedge = true; }
        bool detectLedge = tryGrabLedge && (jumpPressed || !isGrounded);
        plClmbTrig.checkForLedge = detectLedge;
    }
    void Update_Climbing()
    {
        if (crouchAction.IsPressed())
        {
            DropOffLedge();
        }
    }
    void Update_AnimatorParams()
    {
        Animator anim = plAnimCont.anim;
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("moveSpeed", moveSpeedSigned);
        anim.SetFloat("moveAngle", moveAngle);
    }


    Vector3 GetCurrentVelocity()
    {
        Vector3 totalVelocity = moveDirection * moveSpeed;
        totalVelocity += fallSpeed * -Vector3.up;
        return totalVelocity;
    }
    void Update_Grounded()
    {
        bool value = false;
        if (holdingLedge) { isGrounded = false; return; }
        if (charCont.isGrounded) { value = true; }
        //
        RaycastHit hit;
        bool raycastHit = false;
        Debug.DrawRay(groundRaycastPoint.position + (transform.forward * 0.1f), Vector3.down * 0.2f, Color.sandyBrown);
        if (Physics.Raycast(groundRaycastPoint.position + (transform.forward * 0.1f), Vector3.down, out hit, 0.2f)) { raycastHit = true; }
        else
        {
            Debug.DrawRay(groundRaycastPoint.position - (transform.forward * 0.1f), Vector3.down * 0.2f, Color.sandyBrown);
            if (Physics.Raycast(groundRaycastPoint.position - (transform.forward * 0.1f), Vector3.down, out hit, 0.2f)) { raycastHit = true; }
            else
            {
                Debug.DrawRay(groundRaycastPoint.position + (transform.right * 0.1f), Vector3.down * 0.2f, Color.sandyBrown);
                if (Physics.Raycast(groundRaycastPoint.position + (transform.right * 0.1f), Vector3.down, out hit, 0.2f)) { raycastHit = true; }
                else
                {
                    Debug.DrawRay(groundRaycastPoint.position - (transform.right * 0.1f), Vector3.down * 0.2f, Color.sandyBrown);
                    if (Physics.Raycast(groundRaycastPoint.position - (transform.right * 0.1f), Vector3.down, out hit, 0.2f)) { raycastHit = true; }
                }
            }
        }
        if (raycastHit)
        {
            Vector3 hitNormalDir = hit.normal.normalized;
            if (groundSlopeNormal != hitNormalDir)
            {
                // Update move direction
                moveDirection = Vector3.ProjectOnPlane(moveDirection, hitNormalDir);
                // Slow down
                ////// TODO

                groundSlopeNormal = hitNormalDir;
            }
            if (hit.collider.material != null) { groundFriction = hit.collider.material.dynamicFriction; }
            else { groundFriction = 1f; }
            value = true;
        }
        // Is in air
        else
        {
            // Reset move direction plane
            if (groundSlopeNormal != Vector3.up)
            {
                moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
                groundSlopeNormal = Vector3.up;
            }
            // Predict where player will land
            if (!groundPredicted && fallSpeed > 0f)
            {
                Vector3 predVector = GetCurrentVelocity();
                Vector3 predDirection = predVector.normalized;
                float predDistance = predVector.magnitude * predictionTime;
                Ray predictLandRay = new Ray(groundRaycastPoint.position, predDirection);
                Debug.DrawRay(predictLandRay.origin, predictLandRay.direction * predDistance, Color.orange, 3);
                if (Physics.Raycast(predictLandRay, out hit, predDistance))
                {
                    groundPredicted = true;
                    StartCoroutine(RollInputInterval());
                }
            }
        }

        if (value)
        {
            if (!isGrounded) { Landed(); }
            //if (!followCameraRotation && !holdingLedge) { followCameraRotation = true; }
        }
        isGrounded = value;
    }
    void Jump()
    {
        if (!plClmbTrig.CheckForLedges())
        {
            //if (groundSlopeNormal == Vector3.up || groundSlopeNormal == null) { playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y); }
            //else { playerVelocity += Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y) * groundSlopeNormal; Debug.Log("Slope jump!"); }
            fallSpeed -= Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y);

            plAnimCont.anim.SetTrigger("jump");
            followCameraRotation = false;
        }
    }
    IEnumerator RollInputInterval()
    {
        float timer = 0f;
        roll = false;
        while (timer < predictionTime)
        {
            if (crouchAction.IsPressed()) { roll = true; break; }
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }
    void Landed()
    {
        plAnimCont.Landed();

        if (fallSpeed > 0f)
        {
            // Hard landing
            //Debug.Log("Hard landing? " + fallSpeed);
            bool hardLanding = fallSpeed >= hardLandingVelThreshold;
            Debug.Log("Landing type: " + (hardLanding ? (roll ? "2 (roll)" : "3 (hard)") : "1 (normal)"));
            if (hardLanding) 
            {
                plAnimCont.anim.SetInteger("landingType", roll ? 2 : 3);
                if (roll)
                {
                    moveSpeed *= 0.2f; //StartCoroutine(RollMovement());
                }
                else { moveSpeed *= 0.1f; }
            }
            else
            {
                float factor = 1 - (0.1f * (fallSpeed / hardLandingVelThreshold));
                moveSpeed *= factor; //playerVelocity.x *= factor; playerVelocity.z *= factor;
                plAnimCont.anim.SetInteger("landingType", 1);
            }
            plAnimCont.anim.SetTrigger("landing");
        }
        roll = false;
        groundPredicted = false;
        //followCameraRotation = true;
    }
    IEnumerator RollMovement()
    {
        int stateNameHash = Animator.StringToHash("Roll");
        float timer = 0f;
        float maxDuration = 0.9f;
        while (timer < maxDuration && !plAnimCont.anim.GetCurrentAnimatorStateInfo(0).IsName("Roll"))
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        if (timer < maxDuration) 
        { 
            while (plAnimCont.anim.GetCurrentAnimatorStateInfo(0).IsName("Roll"))
            {
                transform.position += (moveSpeed * moveDirection) * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }
    }


    public void JumpObstacle(float height, LedgeLevel ledgeLvl, Vector3 faceDir)
    {
        if (!holdingLedge)
        {
            if (ledgeLvl == LedgeLevel.Med || ledgeLvl == LedgeLevel.High)
            {
                Debug.Log("JumpObstacle");
                // Ledge
                plClmbTrig.checkForLedge = false;
                charCont.enabled = false;
                float localHeight = height - 1f;
                plAnimCont.GrabOntoLedge(localHeight, ledgeLvl == LedgeLevel.High, GetCurrentVelocity());
                holdingLedge = true;
                isGrounded = false;
                // Camera
                followCameraRotation = false;
                transform.LookAt(new Vector3(transform.position.x + faceDir.x, transform.position.y, transform.position.z + faceDir.z));
            }
        }
    }
    void DropOffLedge()
    {
        tryGrabLedge = false;
        holdingLedge = false;
        moveSpeed = 0f; fallSpeed = 0f;
        charCont.enabled = true;
        plAnimCont.DropOffLedge();
    }



    // Animation calls
    public void LandingAnimationDone()
    {
        followCameraRotation = true;
    }
}
