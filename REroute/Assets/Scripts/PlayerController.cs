using System;
using System.Collections;
using System.Linq;
using FishNet.Object;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    public CharacterController charCont;
    public PlayerAnimationController plAnimCont;
    public PlayerParkourDetection playerParkour;
    public PlayerCameraController playerCamera;
    public Transform head;
    public Transform climbTriggersT;
    private VirtualChild virtualChild;
    private GameObject tpCamera;

    // Input
    InputAction freeLookAction;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction attackAction;
    InputAction slideAction;
    bool freeLook = false;

    /// Movement
    // Is the character moved each frame
    bool moveCharacter = true;
    bool applyGravity = true;
    // Is the player currently controllable
    private bool canControlMove = true;
    [SerializeField] private Transform groundRaycastPoint;
    bool isGroundedFrame = false; 
    bool isGroundedAnimBlock = false;
    [Header("Movement")]
    public bool isGrounded = false;
    bool landingPass = false;
    [SerializeField] private float groundedPadTime = 0.6f;
    float groundedPadTimer = 0f;
    // Predict landing for roll input
    bool groundPredicted = false;
    [SerializeField] private float predictionTime = 0.1f;
    bool roll = false;
    bool slide = false; bool currentlySliding = false;

    Vector3 groundSlopeNormal = Vector3.up;
    float groundFriction = 1f;
    // Player velocities
    public float moveSpeed { get; private set; } = 0f;
    float frameMoveSpeedFactor = 1f;
    bool MoveSpeedIsZero() => moveSpeed < 0.001f && moveSpeed > -0.001f;
    /// <summary>false = linear; true = quadratic</summary>
    [SerializeField] private bool linearOrQuadraticRunAccel = false;
    public Vector3 moveDirection { get; private set; } = Vector3.forward;
    bool backwardMovement = false;
    public float fallSpeed { get; private set; } = 0f;
    public bool followCameraRotation = true;

    // Params
    [Header("Serialized parameters")]
    [SerializeField] float walkSpeed = 2f;
    public float runStartSpeed = 5f;
    public float runMaxSpeed = 12f;
    public float runStartAccelTime = 0.5f;
    public float runAccelTime = 10f;
    public float fullDecelTime = 1f;
    public float slideDecelAmount = 4f;
    public float slideAccelAmount = 10f;
    float accelerationFactor = 1f;
    [Tooltip("Jump height in meters.")]
    public float jumpHeight = 0.7f;

    /// Climbing
    //public bool holdingLedge = false;
    bool tryGrabLedge = true;
    public float hardLandingVelThreshold = 2f;

    float climbTriggersBaseLocalPosZ;

    /// Smooth transitions
    [Header("Smoothing parameters")]
    // Roll
    public float smooth_roll_targetSpeedDiff;
    public float smooth_roll_duration;
    public float smooth_roll_startDelay;
    // Hard landing acceleration
    public float smooth_hardlanding_startDelay;
    public float smooth_hardlanding_accelFactor;
    public float smooth_hardlanding_startSpeed = 0f;
    // Braced hang drop
    public float smooth_bracedHand_dropDelay;

    /// Vaulting
    // Vault
    bool isVaulting = false;

    [Space(20)]
    [Header("DEBUG")]
    // Debug
    public float moveSpeedSigned = 0f;
    public float moveAngle = 0f;
    public float lookAngleSigned = 0f;
    public float lookAngleSignedAnim = 0f;


    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            // find cameraPoint gameObject so that we can add later instatiated gameObject player as it's virtual parent
            var camPoint = GameObject.FindGameObjectWithTag("camPoint");
            if (camPoint != null)
            {
                virtualChild = camPoint.GetComponent<VirtualChild>();
                if (virtualChild != null)
                {
                    virtualChild.SetVirtualParent(gameObject);
                    Debug.Log("Assigned player prefb as virtual parent to CameraPoint");
                }

                // get player camera controller from cameraPoint
                playerCamera = camPoint.GetComponent<PlayerCameraController>();

                // assign cameraPoint to TPCamera to follow
                tpCamera = GameObject.FindGameObjectWithTag("TPCamera");
                CinemachineCamera cineCam = tpCamera.GetComponent<CinemachineCamera>();
                cineCam.Follow = camPoint.transform;   
            }
        }
    }

    private void Start()
    {
        charCont = GetComponent<CharacterController>();
        // Input
        freeLookAction = InputSystem.actions.FindAction("Free look");
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        slideAction = InputSystem.actions.FindAction("Slide");
        // Other
        climbTriggersBaseLocalPosZ = climbTriggersT.localPosition.z;
    }

    private void LateUpdate()
    {
        if (followCameraRotation && freeLook && MoveSpeedIsZero())
        {
            float targetYAngle = playerCamera.transform.eulerAngles.y - transform.eulerAngles.y;
            if (targetYAngle > 180f) { targetYAngle = targetYAngle - 360f; }
            if (targetYAngle < -180f) { targetYAngle = 360f + targetYAngle; }
            targetYAngle = Mathf.Clamp(targetYAngle, -90f, 90f);
            head.transform.localEulerAngles = new Vector3(0, targetYAngle, 0);
        }
        else { head.transform.localEulerAngles = Vector3.zero; }
    }

    //// Update
    private void Update()
    {
        if (!IsOwner) return;

        Update_FaceCamera();

        if (playerParkour.holdingLedge)
        {
            
        }
        else
        {
            Update_ClimbableDetect();
        }
        // Check if grounded no matter if we can move
        Update_Grounded();

        Update_Movement();
        Update_Sliding();

        Update_ForwardPosDelta();
        Update_AnimatorParams();
    }
    void Update_FaceCamera()
    {
        freeLook = freeLookAction.IsInProgress();
        if (followCameraRotation)
        {
            // Gameobject rotation
            if (freeLook)
            {
                float targetYAngle = playerCamera.transform.eulerAngles.y - transform.eulerAngles.y;
                if (targetYAngle > 180f) { targetYAngle = targetYAngle - 360f; }
                if (targetYAngle < -180f) { targetYAngle = 360f + targetYAngle; }
                targetYAngle = Mathf.Clamp(targetYAngle, -90f, 90f);
                // Rotate to camera look
                if (!MoveSpeedIsZero())
                {
                    // If moving rotate whole body
                    //Debug.Log(playerCamera.transform.eulerAngles.y + " - " + transform.eulerAngles.y + " = " + targetYAngle);
                    Vector3 targetLocalEulAng = new Vector3(0, targetYAngle, 0);
                    plAnimCont.transform.localEulerAngles = targetLocalEulAng; //Vector3.RotateTowards(plAnimCont.transform.localEulerAngles, targetLocalEulAng, 360f * 1.2f * Time.deltaTime, 0);
                }
                else
                {
                    // Otherwise rotate only head
                    plAnimCont.transform.localEulerAngles = Vector3.zero;
                }
                
            }
            else
            {
                Quaternion targetRotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, playerCamera.transform.eulerAngles.y, transform.eulerAngles.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * 1.2f * Time.deltaTime);

                plAnimCont.transform.localRotation = Quaternion.identity;
            }

            // Look animator parameter
            if (freeLook && MoveSpeedIsZero())
            {
                plAnimCont.anim.SetFloat("lookAngleSign", 0);
                plAnimCont.anim.SetFloat("moveAngle", 0);
            }
            else
            {
                float lookAngleSign;
                if (Mathf.Abs(lookAngleSigned) < 0.1f) { lookAngleSign = 0; }
                else { lookAngleSign = Mathf.Sign(lookAngleSigned); }
                if (lookAngleSign != 0)
                {
                    lookAngleSignedAnim = lookAngleSign;
                }
                else
                {
                    lookAngleSignedAnim = Mathf.Lerp(lookAngleSignedAnim, 0, 2f * Time.deltaTime);
                }
                plAnimCont.anim.SetFloat("lookAngleSign", lookAngleSignedAnim);
                plAnimCont.anim.SetFloat("moveAngle", moveAngle);
            }
        }
        else
        {
            plAnimCont.anim.SetFloat("lookAngleSign", 0);
            plAnimCont.anim.SetFloat("moveAngle", 0);
        }
    }
    void Update_Movement()
    {
        //// Movement
        // Get relevant orientation vectors on the plane we are standing on
        Vector3 cameraForward = Vector3.ProjectOnPlane(new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z), groundSlopeNormal);
        if (moveCharacter && !playerParkour.holdingLedge)
        {
            if (canControlMove)
            {
                Vector3 transformForward = Vector3.ProjectOnPlane(new Vector3(transform.forward.x, 0, transform.forward.z), groundSlopeNormal);
                Vector2 input = moveAction.ReadValue<Vector2>();
                Vector3 frameMoveDir;
                if (!followCameraRotation || freeLook)
                {
                    Vector3 transformRight = Vector3.ProjectOnPlane(new Vector3(transform.right.x, 0, transform.right.z), groundSlopeNormal);
                    frameMoveDir = (transformRight * input.x) + (transformForward * input.y).normalized;
                }
                else
                {
                    Vector3 cameraRight = Vector3.ProjectOnPlane(new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z), groundSlopeNormal);
                    // Get forward and right direction for movement
                    frameMoveDir = (cameraRight * input.x) + (cameraForward * input.y).normalized;
                }
                // Get angles
                float angleMoveToVel = Vector3.Angle(frameMoveDir, moveDirection);
                lookAngleSigned = Vector3.SignedAngle(cameraForward, transformForward, groundSlopeNormal);

                // Fall speed correction
                if (isGroundedFrame && fallSpeed > 0) { fallSpeed = 0.1f; }

                if (isGrounded && fallSpeed >= 0)  // = if we are grounded and our current vertical direction is downward or zero/none (otherwise we are going up)
                {
                    // no input -> slow down to stand still
                    if (input == Vector2.zero)
                    {
                        if (moveSpeed > 0)
                        {
                            float factor = ((runMaxSpeed / fullDecelTime) * Time.deltaTime * (1.0f / 0.25f)) * groundFriction;
                            moveSpeed = Mathf.Clamp(moveSpeed - factor, 0f, Mathf.Infinity); //Mathf.Lerp(moveSpeed, 0f, factor);
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
                            else 
                            {
                                moveDirection = frameMoveDir;
                                if (accelerationFactor == 1) { moveSpeed = walkSpeed; }
                                else { moveSpeed = Mathf.Clamp(moveSpeed + (walkSpeed * Time.deltaTime * accelerationFactor), 0, walkSpeed); }
                            }
                        }
                        // if sprinting -> accelerate
                        else
                        {
                            float angleToVelFactor = Mathf.Clamp(angleMoveToVel / 90f, 0f, 1f);
                            float curMaxSpeed = input.y > 0 ? runMaxSpeed : runStartSpeed;

                            //Debug.Log(moveSpeed + " + " + accelFactor + " -> " + Mathf.Clamp(moveSpeed + accelFactor * Time.deltaTime, 0, curMaxSpeed) + " / " + curMaxSpeed);

                            // Just redirect speed
                            if (angleMoveToVel < 4f)
                            {
                                moveDirection = frameMoveDir;
                                if (moveSpeed < runStartSpeed)
                                { moveSpeed = Mathf.Clamp(moveSpeed + (runStartSpeed * (Time.deltaTime / runStartAccelTime) * accelerationFactor), 0, curMaxSpeed); }
                                else
                                {
                                    float t = (moveSpeed - runStartSpeed) / (runMaxSpeed - runStartSpeed);
                                    // Acceleration function
                                    float c = linearOrQuadraticRunAccel ? (2.0f * (1.0f - t)) : (1.0f);
                                    moveSpeed = Mathf.Clamp(moveSpeed + ((Time.deltaTime / runAccelTime) * c * (runMaxSpeed - runStartSpeed) * accelerationFactor), 0, curMaxSpeed);
                                    //moveSpeed = runStartSpeed + (c * (runMaxSpeed - runStartSpeed));
                                }
                            }
                            // Slow down speed depending on angle and smoothly change direction
                            else if (angleMoveToVel < 90f)
                            {
                                // Slow down
                                if (moveSpeed > runStartSpeed)
                                { moveSpeed = Mathf.Clamp(moveSpeed - (angleToVelFactor * Time.deltaTime), runStartSpeed, runMaxSpeed); } //Mathf.Lerp(moveSpeed, runStartSpeed, angleToVelFactor * Time.deltaTime);
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
            }
            else
            {
                if (currentlySliding)
                {
                    if (moveDirection.y > -0.1f) {
                        moveSpeed = Mathf.Clamp(moveSpeed - (slideDecelAmount * groundFriction * Time.deltaTime), 0, Mathf.Infinity);
                        if (moveSpeed < 0.1f) { SlideStop(); }
                    }
                    else
                    {
                        moveSpeed = Mathf.Clamp(moveSpeed + (slideAccelAmount * groundFriction * Time.deltaTime), 0, runMaxSpeed);

                        //float downwardsAngle = Mathf.Abs(Mathf.Atan2(moveDirection.y, new Vector3(moveDirection.x, 0, moveDirection.z).magnitude) * Mathf.Rad2Deg);
                        //plAnimCont.transform.localEulerAngles = new Vector3(downwardsAngle, plAnimCont.transform.localEulerAngles.y, plAnimCont.transform.localEulerAngles.z);
                    }
                }
            }
            // Gravity
            if (applyGravity) { fallSpeed -= Physics.gravity.y * Time.deltaTime; }

            // Combine velocities and move
            Vector3 frameVelocity = Vector3.zero;
            frameVelocity += moveDirection * moveSpeed;
            if (applyGravity) { frameVelocity += fallSpeed * -Vector3.up; }
            charCont.Move(frameVelocity * Time.deltaTime * frameMoveSpeedFactor);
            frameMoveSpeedFactor = 1f;
        }

        // Update stats
        moveSpeedSigned = moveSpeed;
        if (moveSpeed > 0) { moveSpeedSigned *= (backwardMovement ? -1 : 1); }  //angleLookToVel > 90f
        moveAngle = Vector3.SignedAngle(cameraForward, moveDirection, groundSlopeNormal);
        if (backwardMovement) { moveAngle = (180f - Mathf.Abs(moveAngle)) * Mathf.Sign(moveAngle); }
    }
    void Update_ForwardPosDelta()
    {
        Vector3 headLocalPos = transform.InverseTransformPoint(head.position);
        float headForwDeltaPos = headLocalPos.z;
        //charCont.center = new Vector3(charCont.center.x, charCont.center.y, headForwDeltaPos);
        climbTriggersT.localPosition = new Vector3(climbTriggersT.localPosition.x, climbTriggersT.localPosition.y, climbTriggersBaseLocalPosZ + headForwDeltaPos);
    }
    void Update_Sliding()
    {
        // Sliding
        if (!slide && !currentlySliding && isGrounded && canControlMove && !backwardMovement)
        {
            if (slideAction.WasPressedThisFrame()) { Slide(); }
        }
        else if (slide && currentlySliding)
        {
            if (moveDirection.y > -0.1f) { if (!slideAction.IsPressed()) { SlideStop(); } }
        }
    }
    void Update_ClimbableDetect()
    {
        bool jumpPressed = jumpAction.IsInProgress();
        if (!tryGrabLedge && jumpPressed) { tryGrabLedge = true; }
        bool detectLedge = tryGrabLedge && (jumpPressed || !isGrounded);
        playerParkour.checkForClimbable = detectLedge;
    }
    void Update_AnimatorParams()
    {
        Animator anim = plAnimCont.anim;
        anim.SetFloat("moveSpeed", moveSpeedSigned);
        //anim.SetFloat("moveAngle", moveAngle);
        anim.SetFloat("fallSpeed", fallSpeed);
        anim.SetBool("holdingLedge", playerParkour.holdingLedge);

        // isGrounded block
        if (isGroundedAnimBlock) 
        {
            anim.SetBool("isGrounded", true);
            if (isGrounded) { isGroundedAnimBlock = false; }
        }
        else { anim.SetBool("isGrounded", isGrounded); }
    }
    void Update_Grounded()
    {
        if (playerParkour.holdingLedge) { isGrounded = false; return; }
        bool value = false;
        if (charCont.isGrounded) { value = true; }
        //
        RaycastHit hit;
        bool raycastHit = false;
        float raycastLength = 0.2f;
        Debug.DrawRay(groundRaycastPoint.position + (transform.forward * 0.1f), Vector3.down * 0.2f, Color.sandyBrown);
        if (Physics.Raycast(groundRaycastPoint.position + (transform.forward * 0.1f), Vector3.down, out hit, raycastLength)) { raycastHit = true; }
        else
        {
            Debug.DrawRay(groundRaycastPoint.position - (transform.forward * 0.1f), Vector3.down * 0.2f, Color.sandyBrown);
            if (Physics.Raycast(groundRaycastPoint.position - (transform.forward * 0.1f), Vector3.down, out hit, raycastLength)) { raycastHit = true; }
            else
            {
                Debug.DrawRay(groundRaycastPoint.position + (transform.right * 0.1f), Vector3.down * 0.2f, Color.sandyBrown);
                if (Physics.Raycast(groundRaycastPoint.position + (transform.right * 0.1f), Vector3.down, out hit, raycastLength)) { raycastHit = true; }
                else
                {
                    Debug.DrawRay(groundRaycastPoint.position - (transform.right * 0.1f), Vector3.down * 0.2f, Color.sandyBrown);
                    if (Physics.Raycast(groundRaycastPoint.position - (transform.right * 0.1f), Vector3.down, out hit, raycastLength)) { raycastHit = true; }
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
                groundSlopeNormal = hitNormalDir;
            }
            if (hit.collider.material != null) { groundFriction = hit.collider.material.dynamicFriction; }
            else { groundFriction = 1f; }
            value = true;

            // Slide down if downwards slope
            if (moveDirection.y < 0)
            {
                if (!slide && moveSpeed > walkSpeed)
                {
                    float downwardsAngle = Mathf.Abs(Mathf.Atan2(moveDirection.y, new Vector3(moveDirection.x, 0, moveDirection.z).magnitude) * Mathf.Rad2Deg);
                    if (downwardsAngle > 29f)
                    { 
                        Slide();
                        plAnimCont.transform.localEulerAngles = new Vector3(downwardsAngle, plAnimCont.transform.localEulerAngles.y, plAnimCont.transform.localEulerAngles.z);
                    }
                }
            }
            // Otherwise move slower
            else if (moveDirection.y > 0)
            {
                frameMoveSpeedFactor = 0.5f;
            }
        }
        // Is in air (no timer padding; frame important)
        else //if (!value)
        {
            // Reset move direction plane
            if (groundSlopeNormal != Vector3.up)
            {
                moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
                groundSlopeNormal = Vector3.up;
            }
        }

        isGroundedFrame = value;
        if (value != isGrounded)
        {
            if (value) { groundedPadTimer = groundedPadTime; Landed(); }
            else 
            {
                // Timer padding
                if (groundedPadTime > 0f)  // if timer padding enabled
                {
                    if (groundedPadTimer > 0f)
                    {
                        value = true;
                        groundedPadTimer -= Time.deltaTime;
                    }
                }
                if (!value) { followCameraRotation = false; }
            }
        }
        isGrounded = value;

        // If is in air (incl timer padding)
        if (!isGrounded)
        {
            // Predict where player will land
            if (!groundPredicted && fallSpeed >= 1f)
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
    }

    //// Main
    // Vertical movement actions
    void Jump()
    {
        //Debug.Log("Jump");
        followCameraRotation = false;

        if (isGrounded && playerParkour.TryVaultFromTrigger())
            return;

        Tuple<bool, bool> parkourableDetectedOnJump = playerParkour.CheckClimbables();  // Item1 -> in ground trigger, Item2 -> in jump trigger
        if (!parkourableDetectedOnJump.Item1)
        {
            groundPredicted = false;
            //if (groundSlopeNormal == Vector3.up || groundSlopeNormal == null) { playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y); }
            //else { playerVelocity += Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y) * groundSlopeNormal; Debug.Log("Slope jump!"); }
            fallSpeed = -Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y);

            plAnimCont.anim.SetTrigger("jump");
            //Debug.Log("Normal jump");
        }
        else
        {
            Debug.Log("Ledge jump, isGrounded: " + isGrounded);
            //Debug.Break();
        }
    }
    IEnumerator RollInputInterval()
    {
        float timer = 0f;
        roll = false;
        while (timer < predictionTime)
        {
            if (slideAction.IsPressed()) { roll = true; break; }
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }
    void Landed()
    {
        bool hardLanding = fallSpeed >= hardLandingVelThreshold && !landingPass;
        if (fallSpeed > 0f)
        {
            // Hard landing
            //Debug.Log("Hard landing? " + fallSpeed);
            Debug.Log("Landing type: " + (hardLanding ? (roll ? "2 (roll)" : "3 (hard)") : "1 (normal)"));
            if (hardLanding)
            {
                plAnimCont.anim.SetInteger("landingType", roll ? 2 : 3);
                if (roll) { StartCoroutine(RollSpeedLoss_Smooth()); } //moveSpeed *= 0.5f; }
                else { MovementAnimationSolo(true);  //AnimationSolo(true, true);
                    moveSpeed = smooth_hardlanding_startSpeed; //(moveSpeed * 0.25f > smooth_hardlanding_startSpeed) ? smooth_hardlanding_startSpeed : (moveSpeed * 0.25f);
                    StartCoroutine(HardLandingAcceleration_Smooth()); }
            }
            else
            {
                //if (moveSpeed < runStartSpeed) { moveSpeed = 0f; }
                if (true) //else
                {
                    float factor = 1 - (0.1f * (fallSpeed / hardLandingVelThreshold));
                    moveSpeed *= factor; //playerVelocity.x *= factor; playerVelocity.z *= factor;
                }
                plAnimCont.anim.SetInteger("landingType", 1);
            }
        }
        followCameraRotation = !hardLanding; //if (!hardLanding) { followCameraRotation = true; }
        roll = false;
        groundPredicted = false;
        landingPass = false;
    }
    // Sliding
    void Slide()
    {
        if (moveSpeed < walkSpeed) { return; }
        MovementAnimationSolo(true);
        plAnimCont.anim.SetBool("sliding", true);
        slide = true;
    }
    public void SlideStart()
    {
        currentlySliding = true;
        SetSmallCollider(true);
    }
    void SlideStop()
    {
        plAnimCont.anim.SetBool("sliding", false);
        slide = false;
    }
    public void SlideEnd()
    {
        currentlySliding = false;
        MovementAnimationSolo(false);
        SetSmallCollider(false);
    }
    // Ledges
    public void GrabbedLedge(Ledge ledge)
    {
        charCont.enabled = false;
        moveSpeed = 0; //fallSpeed = 0;
        moveDirection = Vector3.zero;
        // Camera
        followCameraRotation = false;
        transform.LookAt(new Vector3(transform.position.x + ledge.transform.forward.x, transform.position.y, transform.position.z + ledge.transform.forward.z));
    }
    void DropOffLedge()
    {
        fallSpeed = 0;
        playerParkour.holdingLedge = false;
        plAnimCont.DropOffLedge();
        StartCoroutine(DropOffBracedHang_Smooth());
    }
    public void ClimbedLedge(Ledge ledge = null, bool snap = true, float speedAfterClimb = 0f)
    {
        Debug.Log("ClimbedLedge()");

        fallSpeed = 0;
        // Enable
        tryGrabLedge = false;
        playerParkour.holdingLedge = false;
        charCont.enabled = true;
        isGroundedAnimBlock = true; isGrounded = false;
        plAnimCont.anim.SetBool("isGrounded", true);

        landingPass = true;

        playerParkour.ClimbedLedge();

        // Set position to ground
        if (snap)
        {
            //if (ledge == null) { SnapToGround(1f); }
            //else { transform.position = ledge.transform.position + (ledge.transform.forward * 0.5f) + (Vector3.up * 0.05f); }

            transform.position = new Vector3(transform.position.x, ledge.transform.position.y + 0.05f, transform.position.z);
            //transform.position = ledge.transform.position + (ledge.transform.forward * 0.5f) + (Vector3.up * 0.05f);
            //StartCoroutine(SnapToGroundSmooth(ledge.transform));

            SimulateFall();
            Debug.Log("ClimbedLedge() snapped!");
        }
        if (moveAction.ReadValue<Vector2>().y > 0) { moveSpeed = walkSpeed; }
        //if (moveAction.ReadValue<Vector2>().y <= 0) { moveSpeed = 0; }
    }


    IEnumerator SnapToGroundSmooth(Transform ledge)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = ledge.transform.position + (ledge.transform.forward * 0.2f) + (Vector3.up * 0.05f);
        float duration = 0.1f;
        float timer = 0f;
        while (timer < duration) {
            transform.position = Vector3.Slerp(startPosition, targetPosition, timer / duration);
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.position = targetPosition;
    }

    // Vaults
    public void StartVault(Ledge ledge, int vaultType)
    {
        Debug.Log("StartVault() type: " + vaultType);

        isVaulting = true;

        followCameraRotation = false;
        transform.LookAt(new Vector3(
            transform.position.x + ledge.transform.forward.x,
            transform.position.y,
            transform.position.z + ledge.transform.forward.z));

        plAnimCont.StartVault(vaultType);
    }

    public void VaultStart()
    {
        Debug.Log("VaultStart()");
        AnimationSolo(true, false);
    }

    public void VaultEnd()
    {
        Debug.Log("VaultEnd()");
        isVaulting = false;
        isGroundedAnimBlock = true;
        landingPass = true;
        AnimationSolo(false);
        followCameraRotation = true;

        //experimental
        if (moveAction.ReadValue<Vector2>().y > 0)
        {
            moveSpeed = walkSpeed;
        }
    }

    //// Common
    Vector3 GetCurrentVelocity()
    {
        Vector3 totalVelocity = moveDirection * moveSpeed;
        totalVelocity += fallSpeed * -Vector3.up;
        return totalVelocity;
    }
    public void AnimationSolo(bool enabled, bool keepMoving = false)
    {
        GetComponent<CharacterController>().detectCollisions = !enabled; //playerCont.GetComponent<CharacterController>().enabled = true;
        canControlMove = !enabled;
        if (!enabled) { accelerationFactor = 1f; }
        applyGravity = !enabled;
        moveCharacter = !enabled || keepMoving;
        followCameraRotation = !enabled;
    }
    public void MovementAnimationSolo(bool enabled)
    {
        //GetComponent<CharacterController>().detectCollisions = !enabled;
        canControlMove = !enabled;
        if (!enabled) { accelerationFactor = 1f; }
        //applyGravity = !enabled;
        moveCharacter = true;
        followCameraRotation = !enabled;
        plAnimCont.transform.localEulerAngles = new Vector3(plAnimCont.transform.localEulerAngles.x, 0, plAnimCont.transform.localEulerAngles.z);
    }

    //// Utility
    void SnapToGround(float rayLength = 0.4f, float verticalStartPointDelta = 1f)
    {
        //Debug.Break();
        rayLength += verticalStartPointDelta;
        RaycastHit hit;
        bool raycastHit = false;
        Vector3 startPoint = groundRaycastPoint.position + (transform.forward * 0.1f) + (transform.up * verticalStartPointDelta);
        Debug.DrawRay(startPoint, Vector3.down * rayLength, Color.rosyBrown);
        if (Physics.Raycast(startPoint, Vector3.down, out hit, rayLength)) { raycastHit = true; }
        else
        {
            startPoint = groundRaycastPoint.position - (transform.forward * 0.1f) + (transform.up * verticalStartPointDelta);
            Debug.DrawRay(startPoint, Vector3.down * rayLength, Color.rosyBrown);
            if (Physics.Raycast(startPoint, Vector3.down, out hit, rayLength)) { raycastHit = true; }
            else
            {
                startPoint = groundRaycastPoint.position + (transform.right * 0.1f) + (transform.up * verticalStartPointDelta);
                Debug.DrawRay(startPoint, Vector3.down * rayLength, Color.rosyBrown);
                if (Physics.Raycast(startPoint, Vector3.down, out hit, rayLength)) { raycastHit = true; }
                else
                {
                    startPoint = groundRaycastPoint.position - (transform.right * 0.1f) + (transform.up * verticalStartPointDelta);
                    Debug.DrawRay(startPoint, Vector3.down * rayLength, Color.rosyBrown);
                    if (Physics.Raycast(startPoint, Vector3.down, out hit, rayLength)) { raycastHit = true; }
                }
            }
        }
        if (raycastHit)
        {
            Debug.Log("Hit!: " + hit.point + "; snapping to " + (hit.point));
            Vector3 hitNormalDir = hit.normal.normalized;
            if (groundSlopeNormal != hitNormalDir)
            {
                // Update move direction
                moveDirection = Vector3.ProjectOnPlane(moveDirection, hitNormalDir);
                groundSlopeNormal = hitNormalDir;
            }
            if (hit.collider.material != null) { groundFriction = hit.collider.material.dynamicFriction; }
            else { groundFriction = 1f; }

            // Snap
            transform.position = hit.point;
            isGrounded = true;
        }
        else { Debug.Log("Miss!"); }
    }
    void SimulateFall()
    {
        float heightFell = 0f; float heightLimit = 10f;
        float delta = 0.02f;
        Update_Grounded();
        while (!isGrounded && heightFell < heightLimit)
        {
            charCont.Move(Vector3.down * delta);
            Update_Grounded();
            heightFell += delta;
        }
        if (heightFell >= heightLimit) { Debug.LogWarning("Failed to simulate fall - height limit reached."); }
    }
    public void SetSmallCollider(bool value)
    {
        if (value)
        {
            charCont.center = new Vector3(charCont.center.x, 0.35f, charCont.center.z);
            charCont.height = 0.5f;
        }
        else
        {
            charCont.center = new Vector3(charCont.center.x, 1, charCont.center.z);
            charCont.height = 1.8f;
        }
    }

    //// External utility
    /*public void RootMotionMovement(bool enabled, bool keepMoving = false)
    {
        GetComponent<CharacterController>().detectCollisions = !enabled; //playerCont.GetComponent<CharacterController>().enabled = true;
        canControlMove = !enabled;
        applyGravity = !enabled;
        moveCharacter = !enabled || keepMoving;
    }*/


    //// Smooth transition functions
    IEnumerator RollSpeedLoss_Smooth()
    {
        float moveSpeedOnCall = moveSpeed;
        float targetSpeed = Mathf.Clamp(moveSpeed - smooth_roll_targetSpeedDiff, 0, runMaxSpeed);

        float startDelay = smooth_roll_startDelay;
        if (startDelay > 0) { yield return new WaitForSeconds(startDelay); }

        float duration = smooth_roll_duration;
        float timer = 0f;
        while (timer < duration)
        {
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
            moveSpeed = Mathf.Lerp(moveSpeedOnCall, targetSpeed, timer / duration);
        }
    }
    IEnumerator DropOffBracedHang_Smooth()
    {
        yield return new WaitForSeconds(smooth_bracedHand_dropDelay);
        tryGrabLedge = false;
        playerParkour.holdingLedge = false;
        moveSpeed = 0f; fallSpeed = 0f;
        charCont.enabled = true;
    }
    IEnumerator HardLandingAcceleration_Smooth()
    {
        float startDelay = smooth_hardlanding_startDelay;
        if (startDelay > 0) { yield return new WaitForSeconds(startDelay); }

        accelerationFactor = smooth_hardlanding_accelFactor;
        canControlMove = true;
    }
    IEnumerator SlideDecellerationStart_Smooth()
    {
        float startDelay = 1f;
        if (startDelay > 0) { yield return new WaitForSeconds(startDelay); }

        currentlySliding = true;
    }
}

enum LedgeHoldType { BracedHang, Hang, Freehang };



/*[CustomEditor(typeof(PlayerController))]
public class PlayerController_Inspector : Editor
{
    PlayerController playerCont;

    bool movementSpeeds;
    bool smoothTransitions;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        playerCont = (PlayerController)target;

        Control();
        Speeds();

        RollSmoothSlowdown();
    }

    void Control()
    {
        EditorGUILayout.LabelField("-- Control");
        EditorGUILayout.Toggle("Move character", playerCont.moveCharacter);
        EditorGUILayout.Toggle("Apply gravity", playerCont.applyGravity);
        EditorGUILayout.Toggle("Control character movement", playerCont.canControlMove);
    }
    void Speeds()
    {
        EditorGUILayout.LabelField("-- Movement");
        EditorGUILayout.FloatField("Current speed", playerCont.moveSpeed);
        EditorGUILayout.FloatField("Current fall speed", playerCont.fallSpeed);
        if (movementSpeeds = EditorGUILayout.Foldout(movementSpeeds, "Parameters"))
        {
            //playerCont.walkSpeed = EditorGUILayout.FloatField("Walk speed", playerCont.walkSpeed);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Run start speed | Time to accelerate");
            playerCont.runStartSpeed = EditorGUILayout.FloatField(playerCont.runStartSpeed);
            playerCont.runStartAccelTime = EditorGUILayout.FloatField(playerCont.runStartAccelTime);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Run max speed | Time to accelerate");
            playerCont.runMaxSpeed = EditorGUILayout.FloatField(playerCont.runMaxSpeed);
            playerCont.runAccelTime = EditorGUILayout.FloatField(playerCont.runAccelTime);
            EditorGUILayout.EndHorizontal();
        }

    }
    void RollSmoothSlowdown()
    {
        if (smoothTransitions = EditorGUILayout.Foldout(smoothTransitions, "-- Smooth Transition Settings"))
        {

            LeftCenteredLabel("Roll");
            playerCont.smooth_roll_targetSpeedDiff = EditorGUILayout.FloatField("Slow down value", playerCont.smooth_roll_targetSpeedDiff);
            playerCont.smooth_roll_duration = EditorGUILayout.FloatField("Slow down duration", playerCont.smooth_roll_duration);
            playerCont.smooth_roll_startDelay = EditorGUILayout.FloatField("Slow down start delay", playerCont.smooth_roll_startDelay);
        }
    }


    // Utility
    void CenteredLabel(string label) => EditorGUILayout.LabelField(" ", label);
    void LeftCenteredLabel(string label) => EditorGUILayout.LabelField(String.Concat(Enumerable.Repeat(" ", 20)) + label);
}*/