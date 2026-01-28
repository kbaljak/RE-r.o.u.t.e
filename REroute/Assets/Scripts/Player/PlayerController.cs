using System.Collections;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerFollowRotation { NONE, CAMERA, MOVEMENT }

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    private string playerHash;

    [Header("References")]
    public CharacterController charCont;
    public PlayerAnimationController plAnimCont;
    public PlayerParkourDetection playerParkour;
    public PlayerCameraController playerCamera;
    [SerializeField] private PlayerItemInteraction playerItemInteraction;
    private PlayerUIController plScoreCont;
    public Transform head;
    public Transform climbTriggersT;
    private VirtualChild virtualChild;
    private GameObject tpCamera;
    [SerializeField] private GameObject rollMarker;

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
    bool slopeSliding = false;
    

    Vector3 groundSlopeNormal = Vector3.up;
    Collider groundCollider = null;
    float groundFriction = 1f;
    // Player velocities
    public float moveSpeed { get; private set; } = 0f;
    bool MoveSpeedIsZero() => moveSpeed < 0.001f && moveSpeed > -0.001f;
    /// <summary>false = linear; true = quadratic</summary>
    [SerializeField] private bool linearOrQuadraticRunAccel = false;
    public Vector3 moveDirection { get; private set; } = Vector3.forward;
    bool backwardMovement = false;
    // Slope
    float slopeAngle = 0f;
    float slopeAccFactor = 1f;
    bool groundIsSlope = false;
    Vector3 slopeDirection;
    public float fallSpeed { get; private set; } = 0f;
    //public bool followCameraRotation = true;
    public PlayerFollowRotation followRotation = PlayerFollowRotation.CAMERA;

    // Game
    Vector3 currentRespawnPoint;

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
    float frameAccelerationFactor = 1f;
    [Tooltip("Jump height in meters.")]
    public float jumpHeight = 0.7f;
    [Tooltip("Is air control enabled?")]
    public bool airControl = false;

    /// Climbing
    //public bool holdingLedge = false;
    //bool tryGrabLedge = true;
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
    public float smooth_hardlanding_accelKeepTime = 1.2f;
    public float smooth_hardlanding_accelIncreaseTime = 1f;
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

    private readonly SyncVar<string> playerNameTag = new SyncVar<string>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
        {
            //gameObject.GetComponent<PlayerController>().enabled = false;
            //gameObject.GetComponent<CharacterController>().enabled = false;
            enabled = false; playerParkour.enabled = false;
            return;
        }

        string myName;
        if (IsHostStarted) { myName = NetworkConnectionStarter.Instance.GetHostName(); }
        else { myName = NetworkConnectionStarter.Instance.GetPlayerName(); }

        SetPlayerNameServerRPC(myName);

        RegisterPlayer(myName);

        SetupCharacter();

        //RaceTimeManager.Instance.RegisterPlayer(Owner, myName);

        UI.InitializePlayerController(this);
    }

    [ServerRpc]
    private void SetPlayerNameServerRPC(string name)
    {
        playerNameTag.Value = name;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayer(string playerName, NetworkConnection conn = null)
    {
        playerHash = ComputePlayerHash(conn);
        Debug.LogWarning("Registering conn: " + conn);
        RaceTimeManager.Instance.RegisterPlayer(playerHash, conn, playerName);
    }
    public static string ComputePlayerHash(object value)
    {
        if (value == null)
            return null;


        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value.ToString());
            byte[] hash = sha256.ComputeHash(bytes);


            var sb = new StringBuilder();
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));


            // return only first 10 characters
            return sb.ToString().Substring(0, 15);
        }
    }
    public string GetPlayerHash() => playerHash;

    [ObserversRpc]
    public void TeleportPlayerToLevelSpawnPoints(Vector3 newPosition, Quaternion newRotation)
    {
        // Only execute on the owner's client
        if (!IsOwner) return;
        
        Debug.Log($"TeleportPlayer called on {gameObject.name} - Owner: {IsOwner}");
        
        // Disable character controller temporarily
        if (charCont != null)
        {
            charCont.enabled = false;
        }
        
        // Set new position and rotation
        transform.position = newPosition;
        transform.rotation = newRotation;
        
        // Reset movement state to prevent weird physics issues
        moveSpeed = 0f;
        fallSpeed = 0f;
        moveDirection = transform.forward;
        
        Debug.Log($"Player teleported to {newPosition}");
        
        // Re-enable character controller
        if (charCont != null)
        {
            charCont.enabled = true;
        }
    }
    public string GetPlayerName()
    {
        return playerNameTag.Value;
    }

    // [ObserversRpc]
    // public void ClientReinitializeAfterSceneLoad()
    // {
    //     if (!IsOwner) return;
        
    //     Debug.Log("ClientReinitializeAfterSceneLoad called on owner");
    //     StartCoroutine(DelayedSceneSetup());
    // }

    // private IEnumerator DelayedSceneSetup()
    // {
    //     // Wait for scene to fully initialize
    //     yield return new WaitForEndOfFrame();
    //     yield return new WaitForEndOfFrame();

    //     Debug.Log("Starting delayed scene setup...");
        
    //     // Re-find cameras in new scene
    //     SetupCharacter();
        
    //     // Reset movement state
    //     ResetMovementState();
        
    //     Debug.Log("PlayerController: Scene setup complete");
    // }
    private void ResetMovementState()
    {
        // Reset movement variables
        moveSpeed = 0f;
        fallSpeed = 0f;
        moveDirection = transform.forward;
        
        // Reset states
        isGrounded = false;
        slide = false;
        currentlySliding = false;
        roll = false;
        
        // Reset camera following
        followRotation = PlayerFollowRotation.CAMERA;
        
        // Make sure character controller is enabled
        if (charCont != null)
        {
            charCont.enabled = true;
        }

        Debug.Log("Movement state reset complete");
    }

    public void EnablePlayerControl(bool value)
    {
        if (!IsOwner) { return; }

        canControlMove = value;
        followRotation = value ? PlayerFollowRotation.CAMERA : PlayerFollowRotation.NONE;
    }
    private void ResetVars()
    {
        isGrounded = false;

        moveSpeed = 0f;
        moveDirection = Vector3.zero;
        fallSpeed = 0f;

        slide = false;
        currentlySliding = false;
        isVaulting = false;

        moveSpeedSigned = 0f;
        moveAngle = 0f;
        lookAngleSigned = 0f;
        lookAngleSignedAnim = 0f;
    }

    private void Start()
    {
        charCont = GetComponent<CharacterController>();
            
        // Input setup (only once)
        freeLookAction = InputSystem.actions.FindAction("Free look");
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        slideAction = InputSystem.actions.FindAction("Slide");
            
        climbTriggersBaseLocalPosZ = climbTriggersT.localPosition.z;
        //SetupCharacter();
    
        currentRespawnPoint = transform.position;
    }

    private void SetupCharacter()
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

        plScoreCont = GetComponent<PlayerUIController>();
        if (plScoreCont == null) { Debug.LogError("Coudl not find Player UI Controller!"); }

        
        if (playerItemInteraction == null)
        {
            if (transform.Find("ItemInteraction")) { playerItemInteraction = transform.Find("ItemInteraction").GetComponent<PlayerItemInteraction>(); }
            if (playerItemInteraction == null) { Debug.LogError("PlayerItemInteraction component not found on PlayerController!"); }
        }
    }

    private void LateUpdate()
    {
        if (followRotation != PlayerFollowRotation.CAMERA && freeLook && MoveSpeedIsZero())
        {
            float targetYAngle = playerCamera.transform.eulerAngles.y - transform.eulerAngles.y;
            if (targetYAngle > 180f) { targetYAngle = targetYAngle - 360f; }
            if (targetYAngle < -180f) { targetYAngle = 360f + targetYAngle; }
            targetYAngle = Mathf.Clamp(targetYAngle, -90f, 90f);
            head.transform.localEulerAngles = new Vector3(0, targetYAngle, 0);
        }
        else { head.transform.localEulerAngles = Vector3.zero; }
    }

    public void Respawn()
    {
        if (!IsOwner) { return; }

        // Disable player
        //enabled = false;
        gameObject.SetActive(false);
        canControlMove = false;

        // Reset vars
        ResetVars();

        // Move to respawn point
        transform.position = currentRespawnPoint;

        // Reset animator
        plAnimCont.anim.Rebind();
        plAnimCont.anim.Update(0f);

        // Enable player
        gameObject.SetActive(true);
        canControlMove = true;
    }
    public void SetRespawnPoint(Vector3 pos) { currentRespawnPoint = pos; }

    public void AddScoreAfterRoll()
    {
        plScoreCont.OnCombatRollPerformedScore();
    }

    //// Update
    private void Update()
    {
        if (!IsOwner) return;

        if (playerCamera != null)
        {
            Update_FaceCamera();
        }

        Update_ClimbableDetect();
        // Check if grounded no matter if we can move
        Update_Grounded();

        Update_Sliding();
        Update_Movement();

        Update_ForwardPosDelta();
        Update_AnimatorParams();
    }
    Vector2 GetMoveInput() { return moveAction.ReadValue<Vector2>(); }
    Vector3 GetCameraForward() { if (playerCamera == null) { return Vector3.zero; } else { return Vector3.ProjectOnPlane(new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z), groundSlopeNormal); }}
    Vector3 GetTransformForward() { return Vector3.ProjectOnPlane(new Vector3(transform.forward.x, 0, transform.forward.z), groundSlopeNormal); }
    Vector3 GetFrameMovementDirection(Vector3 cameraForward, Vector2 input, Vector3 transformForward)
    {
        Vector3 frameMoveDir;
        if (followRotation != PlayerFollowRotation.CAMERA || freeLook)  // Body is facing in a different direction than camera
        {
            Vector3 transformRight = Vector3.ProjectOnPlane(new Vector3(transform.right.x, 0, transform.right.z), groundSlopeNormal);
            frameMoveDir = (transformRight * input.x) + (transformForward * input.y).normalized;
        }
        else  // Body forward is camera forward
        {
            Vector3 cameraRight = Vector3.ProjectOnPlane(new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z), groundSlopeNormal);
            // Get forward and right direction for movement
            frameMoveDir = (cameraRight * input.x) + (cameraForward * input.y).normalized;
        }
        return frameMoveDir;
    }
    // Main update
    void Update_FaceCamera()
    {
        freeLook = freeLookAction.IsInProgress();

        
        // Options: transform towards camera; only head towards camera
        // Fully rotate transform and body towards camera
        if (followRotation == PlayerFollowRotation.CAMERA && !freeLook)
        {
            Quaternion targetRotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, playerCamera.transform.eulerAngles.y, transform.eulerAngles.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * 1.2f * Time.deltaTime);

            plAnimCont.transform.localRotation = Quaternion.identity;
        }
        // Rotate body towards movement (free look)
        else if (followRotation == PlayerFollowRotation.MOVEMENT || (followRotation == PlayerFollowRotation.CAMERA && freeLook))
        {
            float targetYAngle;
            if (followRotation == PlayerFollowRotation.CAMERA) { targetYAngle = playerCamera.transform.eulerAngles.y - transform.eulerAngles.y; }
            else { targetYAngle = (Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg); }

            if (!MoveSpeedIsZero())
            {
                if (followRotation == PlayerFollowRotation.CAMERA)
                {
                    if (targetYAngle > 180f) { targetYAngle = targetYAngle - 360f; }
                    if (targetYAngle < -180f) { targetYAngle = 360f + targetYAngle; }
                    targetYAngle = Mathf.Clamp(targetYAngle, -90f, 90f);
                    // If moving rotate whole body
                    plAnimCont.transform.localEulerAngles = new Vector3(0, targetYAngle, 0); //Vector3.RotateTowards(plAnimCont.transform.localEulerAngles, targetLocalEulAng, 360f * 1.2f * Time.deltaTime, 0);
                }
                else
                {
                    plAnimCont.transform.eulerAngles = new Vector3(0, targetYAngle, 0);
                }
            }
            else
            {
                // Otherwise rotate only head
                plAnimCont.transform.localEulerAngles = Vector3.zero;
            }
        }
        // Don't rotate at all
        else {}

        // Look animator params
        if (followRotation != PlayerFollowRotation.NONE && !MoveSpeedIsZero())
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
        else
        {
            plAnimCont.anim.SetFloat("lookAngleSign", 0);
            plAnimCont.anim.SetFloat("moveAngle", 0);
        }
    }
    void Update_Movement()
    {
        // Get relevant orientation vectors on the plane we are standing on
        Vector3 cameraForward = GetCameraForward();

        if (moveCharacter && !playerParkour.holdingLedge && !playerParkour.isVaulting)
        {
            // Fall speed correction
            if (isGroundedFrame && fallSpeed > 0) { fallSpeed = 0.1f; }

            if (currentlySliding) { Update_Movement_Slope(cameraForward); }
            else { Update_Movement_Normal(cameraForward); }

            // Gravity
            if (applyGravity) { fallSpeed -= Physics.gravity.y * Time.deltaTime; }

            // Combine velocities and move
            Vector3 frameVelocity = Vector3.zero;
            frameVelocity += moveDirection * moveSpeed;
            if (applyGravity) { frameVelocity += fallSpeed * -Vector3.up; }
            if (!playerParkour.holdingLedge && !playerParkour.isVaulting) { charCont.Move(frameVelocity * Time.deltaTime); } //* frameMoveSpeedFactor);

            // Reset frame vars
            frameAccelerationFactor = 1f;
        }

        // Update stats
        moveSpeedSigned = moveSpeed;
        if (moveSpeed > 0) { moveSpeedSigned *= (backwardMovement ? -1 : 1); }  //angleLookToVel > 90f
        moveAngle = Vector3.SignedAngle(cameraForward, moveDirection, groundSlopeNormal);
        if (backwardMovement) { moveAngle = (180f - Mathf.Abs(moveAngle)) * Mathf.Sign(moveAngle); }
    }

    void Update_Movement_Normal(Vector3 cameraForward)
    {
        if (playerCamera == null) { return; }

        if (canControlMove)
        {
            // Input to movement direction
            Vector2 input = GetMoveInput(); Vector3 transformForward = GetTransformForward();
            Vector3 frameMoveDir = GetFrameMovementDirection(cameraForward, input, transformForward);

            // Get angles
            float angleMoveToVel = Vector3.Angle(frameMoveDir, moveDirection);
            lookAngleSigned = Vector3.SignedAngle(cameraForward, transformForward, groundSlopeNormal);

            if (isGrounded && fallSpeed >= 0)  // = if we are grounded and our current vertical direction is downward or zero/none (otherwise we are going up)
            {
                // no input -> slow down to stand still
                if (input == Vector2.zero)
                {
                    if (moveSpeed > 0)
                    {
                        float factor = ((runMaxSpeed / fullDecelTime) * Time.deltaTime * (1.0f / 0.25f)) * groundFriction;
                        moveSpeed = Mathf.Clamp(moveSpeed - factor, 0f, Mathf.Infinity);
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
                            if (frameAccelerationFactor == 1f) { moveSpeed = walkSpeed; }
                            else { moveSpeed = Mathf.Clamp(moveSpeed + (walkSpeed * Time.deltaTime * frameAccelerationFactor), 0, walkSpeed); }
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
                            { moveSpeed = Mathf.Clamp(moveSpeed + (runStartSpeed * (Time.deltaTime / runStartAccelTime) * frameAccelerationFactor), 0, curMaxSpeed); }
                            else
                            {
                                float t = (moveSpeed - runStartSpeed) / (runMaxSpeed - runStartSpeed);
                                // Acceleration function
                                float c = linearOrQuadraticRunAccel ? (2.0f * (1.0f - t)) : (1.0f);
                                moveSpeed = Mathf.Clamp(moveSpeed + ((Time.deltaTime / runAccelTime) * c * (runMaxSpeed - runStartSpeed) * frameAccelerationFactor), 0, curMaxSpeed);
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
            else  // If in air
            {
                if (input != Vector2.zero)
                {
                    float moveStrength = 10.0f * Time.deltaTime;

                    // Change speed slightly
                    float angle = Vector3.SignedAngle(moveDirection, frameMoveDir, Vector3.up);
                    float angleFactor = Mathf.Cos(angle * Mathf.Deg2Rad);
                    //moveSpeed = Mathf.Clamp(moveSpeed + (angleFactor * moveStrength), 0, runStartSpeed);

                    // Update direction
                    float resultantDirAngle = Mathf.Atan2((moveSpeed * moveDirection.z) + (moveStrength * frameMoveDir.z), (moveSpeed * moveDirection.x) + (moveStrength * frameMoveDir.x));
                    //                      Mathf.Atan2((moveDirection.z) + (frameMoveDir.z), (moveDirection.x) + (frameMoveDir.x));
                    moveDirection = new Vector3(Mathf.Cos(resultantDirAngle), moveDirection.y, Mathf.Sin(resultantDirAngle));
                }
            }
        }
    }
    void Update_Movement_Slope(Vector3 cameraForward)
    {
        // Climbing slope (Sisyphus)
        if (moveDirection.y > -0.01f)
        {
            moveSpeed = Mathf.Clamp(moveSpeed - ((slideDecelAmount * groundFriction * Time.deltaTime) * (2f - slopeAccFactor)), 0, Mathf.Infinity);
            if (!slideAction.IsPressed() && moveSpeed < runStartSpeed) { SlideStop(); }
        }
        // Going down slope
        else
        {
            float slideAccelFactor = 1f;
            if (slopeSliding && canControlMove)
            {
                float moveToSlopeAngle = Vector3.SignedAngle(slopeDirection, moveDirection, groundSlopeNormal);
                Vector2 input = GetMoveInput();
                if (input.x != 0f)
                {
                    float turnStrength = 45f * Time.deltaTime * (1 - ((moveSpeed / runMaxSpeed) * 0.4f));
                    float turnDelta = turnStrength * input.x;
                    moveToSlopeAngle = Mathf.Clamp(moveToSlopeAngle + turnDelta, -Slope.horizontalAngleLimit, Slope.horizontalAngleLimit);
                }
                else
                {
                    float recenterStrength = 20f * Time.deltaTime;
                    moveToSlopeAngle = Mathf.Sign(moveToSlopeAngle) * Mathf.Clamp(Mathf.Abs(moveToSlopeAngle) - recenterStrength, 0f, Slope.horizontalAngleLimit);
                    //moveDirection = Vector3.RotateTowards(moveDirection, slopeDirection, recenterStrength, 0f);
                }
                moveDirection = Quaternion.AngleAxis(moveToSlopeAngle, groundSlopeNormal) * slopeDirection;
                slideAccelFactor = 0.6f + ((Mathf.Clamp(moveToSlopeAngle, 0f, 45f) / 45f) * 0.4f);
            }

            moveSpeed = Mathf.Clamp(moveSpeed + ((slideAccelAmount * groundFriction * Time.deltaTime) * slideAccelFactor), 0, runMaxSpeed);
        }
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
        // Not sliding
        if (!slide && !currentlySliding && isGrounded && canControlMove && !backwardMovement)
        {
            if (slideAction.WasPressedThisFrame()) { Slide(false); }
        }
        // Sliding
        else if (slide && currentlySliding)
        {
            if (moveDirection.y > -0.01f)
            {
                if (!slideAction.IsPressed()) { SlideStop(); }
                if (slopeSliding) { Slide(false); }
            }
            // Sliding on slope
            else
            {
                if (!slopeSliding)
                {
                    if (groundIsSlope) { Slide(true); }
                    // Stop so weird behaviour doesn't happen
                    else { SlideStop(); }
                }
            }
        }
    }
    void Update_ClimbableDetect()
    {
        playerParkour.checkForClimbable = !isGrounded && !playerParkour.holdingLedge && !playerParkour.isVaulting;
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
        // Do each frame
        rollMarker.SetActive(false);

        if (playerParkour.holdingLedge) { isGrounded = false; return; }
        bool value = false;
        //if (charCont.isGrounded) { value = true; }
        //
        RaycastHit hit;
        bool raycastHit = false;
        float raycastLength = 0.25f;
        float raycastRadius = 0.2f;
        Debug.DrawRay(groundRaycastPoint.position + (transform.forward * raycastRadius), Vector3.down * 0.2f, Color.sandyBrown);
        if (Physics.Raycast(groundRaycastPoint.position + (transform.forward * raycastRadius), Vector3.down, out hit, raycastLength)) { raycastHit = true; }
        else
        {
            Debug.DrawRay(groundRaycastPoint.position - (transform.forward * raycastRadius), Vector3.down * 0.2f, Color.sandyBrown);
            if (Physics.Raycast(groundRaycastPoint.position - (transform.forward * raycastRadius), Vector3.down, out hit, raycastLength)) { raycastHit = true; }
            else
            {
                Debug.DrawRay(groundRaycastPoint.position + (transform.right * raycastRadius), Vector3.down * 0.2f, Color.sandyBrown);
                if (Physics.Raycast(groundRaycastPoint.position + (transform.right * raycastRadius), Vector3.down, out hit, raycastLength)) { raycastHit = true; }
                else
                {
                    Debug.DrawRay(groundRaycastPoint.position - (transform.right * raycastRadius), Vector3.down * 0.2f, Color.sandyBrown);
                    if (Physics.Raycast(groundRaycastPoint.position - (transform.right * raycastRadius), Vector3.down, out hit, raycastLength)) { raycastHit = true; }
                }
            }
        }
        if (raycastHit)
        {
            Vector3 hitNormalDir = hit.normal.normalized;
            if (groundSlopeNormal != hitNormalDir)
            {
                Vector3 newMoveDir = Vector3.ProjectOnPlane(moveDirection, Vector3.up).normalized; 
                newMoveDir = Vector3.ProjectOnPlane(newMoveDir, hitNormalDir).normalized;

                // If slope is less than 45 apply it
                float limit = Mathf.Sin(45f * Mathf.Deg2Rad);
                if (Mathf.Abs(newMoveDir.y) < limit)
                {
                    moveDirection = newMoveDir;
                    groundSlopeNormal = hitNormalDir;
                    slopeAngle = Mathf.Abs(Mathf.Atan2(newMoveDir.y, new Vector3(newMoveDir.x, 0, newMoveDir.z).magnitude)) * Mathf.Rad2Deg;
                    slopeAccFactor = 0.5f + (Mathf.Clamp(45f - slopeAngle, 0f, 45f) / 90f);
                    if (hit.collider.GetComponent<Slope>() is Slope slope)
                    {
                        groundIsSlope = true;
                        slopeDirection = hit.collider.GetComponent<Slope>().direction;
                    }
                }
            }
            
            if (hit.collider.material != null) { groundFriction = hit.collider.material.dynamicFriction; }
            else { groundFriction = 1f; }
            value = true;

            //Debug.Log("Move direction y: " + moveDirection.y);
            if (Mathf.Abs(moveDirection.y) <= 0.01f) { moveDirection = new Vector3(moveDirection.x, 0, moveDirection.z); }
            else
            {
                Vector3 localMoveDir = Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f) * moveDirection;
                // Slide down if downwards slope
                if (moveDirection.y < -0.01f)
                {
                    if (!slide && !backwardMovement && groundIsSlope)  //slopeAngle > 9f
                    {
                        if (moveSpeed > walkSpeed && Slope.SlideCheck(slopeDirection, moveDirection))
                        {
                            Slide(true);
                            plAnimCont.transform.localEulerAngles = new Vector3(slopeAngle, plAnimCont.transform.localEulerAngles.y, plAnimCont.transform.localEulerAngles.z);
                        }
                    }
                    
                }
                // Otherwise move slower (i.e. run up that hill :3)
                else if (moveDirection.y > 0.01f)
                {
                    //frameMoveSpeedFactor = 0.5f + (Mathf.Clamp(45f - slopeAngle, 0f, 45f) / 90f); //0.8f;
                    frameAccelerationFactor *= slopeAccFactor;
                }
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
                if (!value) { followRotation = PlayerFollowRotation.MOVEMENT; }
            }
        }
        isGrounded = value;

        // If is in air (incl timer padding)
        if (!isGrounded)
        {
            float threshold = hardLandingVelThreshold - (0.3f * -Physics.gravity.y);
            
            // Predict where player will land
            if (fallSpeed >= threshold)  //&& !groundPredicted
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

                    // Show with marker
                    bool showMarker = true;
                    // If currently not hard landing threshold check if it will be by time we hit the ground
                    if (fallSpeed < hardLandingVelThreshold)
                    {
                        //s = ut + 0.5 * (a * t^2)
                        float distanceNeeded = predDistance * 0.3f + (0.5f * (-Physics.gravity.y * 0.09f));
                        
                        showMarker = hit.distance >= distanceNeeded;
                    }
                    if (showMarker)  //fallSpeed >= hardLandingVelThreshold - 0.01f)
                    {
                        rollMarker.SetActive(true);
                        rollMarker.transform.position = hit.point + (Vector3.up * 0.1f) + (transform.forward * 0.05f);
                    }
                }
            }
        }
    }

    //// Main
    // Vertical movement actions
    void Jump()
    {
        //Debug.Log("Jump");
        //followRotation = PlayerFollowRotation.MOVEMENT;

        bool parkourableDetectedOnJump = playerParkour.CheckVaultables();
        if (!parkourableDetectedOnJump) { parkourableDetectedOnJump = playerParkour.CheckJumpReachClimbables(); }

        // No vault or jump climbable -> jump normally
        if (!parkourableDetectedOnJump)
        {
            followRotation = PlayerFollowRotation.MOVEMENT;
            groundPredicted = false;

            // Jumping in the normal of the slope - nah
            //if (groundSlopeNormal == Vector3.up || groundSlopeNormal == null) { playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y); }
            //else { playerVelocity += Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y) * groundSlopeNormal; Debug.Log("Slope jump!"); }

            fallSpeed = -Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y);

            plAnimCont.anim.SetTrigger("jump");
            //Debug.Log("Normal jump");
        }
        // Vault / Jump climb (for now nothing special to do)
        else { playerParkour.checkForClimbable = false; }
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
            //Debug.Log("Landing type: " + (hardLanding ? (roll ? "2 (roll)" : "3 (hard)") : "1 (normal)"));
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
                followRotation = PlayerFollowRotation.CAMERA;
            }
        }
        //if (hardLanding) { followRotation = PlayerFollowRotation.MOVEMENT; }
        //else {  followRotation = PlayerFollowRotation.CAMERA; }
        roll = false;
        groundPredicted = false;
        landingPass = false;
    }
    // Sliding
    void Slide(bool slopeSlide)
    {
        if (moveSpeed < walkSpeed) { return; }
        slopeSliding = slopeSlide;
        if (slopeSlide) { canControlMove = true; followRotation = PlayerFollowRotation.MOVEMENT; }
        else { MovementAnimationSolo(true); }
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
        slopeSliding = false;
        MovementAnimationSolo(false);
        SetSmallCollider(false);
    }
    // Ledges
    public void GrabbedLedge(Ledge ledge, bool resetSpeed = true)
    {
        if (resetSpeed) 
        {
            charCont.enabled = false;
            moveSpeed = 0; //fallSpeed = 0;
            moveDirection = Vector3.zero;
        }
        if (ledge.IsLedgeOiledUp())
        {
            //Debug.LogError($"Player just grabbed a oiled ledge, apply penalty for climbing or something!");
            playerItemInteraction.RequestRemoveOilFromLedgeRpc(ledge.GetComponent<NetworkObject>().ObjectId);
        }
        // Camera
        followRotation = PlayerFollowRotation.NONE;
        Vector3 ledgeForward = ledge.transform.forward * (ledge.transform.lossyScale.z < -0.01f ? -1 : 1);
        Vector3 lookAtVec = new Vector3(transform.position.x + ledgeForward.x, transform.position.y, transform.position.z + ledgeForward.z);
        transform.LookAt(lookAtVec);
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
        Debug.Log("ClimbedLedge() => ledge: " + ledge + " snap: " + snap);

        fallSpeed = 0;
        // Enable
        //tryGrabLedge = false;
        playerParkour.holdingLedge = false;
        
        // player climbed ledge, player can apply oil to ledge
        //int ledgeNetID = ledge.GetComponent<NetworkObject>().ObjectId;
        //Debug.Log("Ledge net id: " + ledgeNetID);
        //playerItemInteraction.StartOilApplicationTimeWindow(ledgeNetID);

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
    public void VaultedLedge()
    {
        Debug.Log("VaultedLedge()");
        playerParkour.isVaulting = false;
        isGroundedAnimBlock = true;
        landingPass = true;
        //charCont.enabled = true;
        AnimationSolo(false);
        followRotation = PlayerFollowRotation.CAMERA;

        //SimulateFall();

        //experimental
        /*if (moveAction.ReadValue<Vector2>().y > 0)
        {
            moveSpeed = walkSpeed;
        }*/
        plScoreCont.OnVaultPerformedScore();
    }

    //Slip
    public void GotUpFromSlip()
    {
        Debug.Log("GotUpFromSlip()");
        isGroundedAnimBlock = true;
        landingPass = true;
        //charCont.enabled = true;
        AnimationSolo(false);
        followRotation = PlayerFollowRotation.CAMERA;
    }

    public void ForceStopSliding()
    {
        plAnimCont.anim.SetBool("sliding", false);
        slide = false;
        currentlySliding = false;
        slopeSliding = false;
        SetSmallCollider(false);
    }

    //// Common
    Vector3 GetCurrentVelocity()
    {
        Vector3 totalVelocity = moveDirection * moveSpeed;
        totalVelocity += fallSpeed * -Vector3.up;
        return totalVelocity;
    }



    //// Animation
    public void AnimationSolo(bool enabled, bool keepMoving = false)
    {
        GetComponent<CharacterController>().detectCollisions = !enabled; //playerCont.GetComponent<CharacterController>().enabled = true;
        canControlMove = !enabled;
        //if (!enabled) { accelerationFactor = 1f; }
        applyGravity = !enabled;
        moveCharacter = !enabled || keepMoving;
        //followCameraRotation = !enabled;
        followRotation = enabled ? PlayerFollowRotation.NONE : PlayerFollowRotation.CAMERA;
    }
    public void MovementAnimationSolo(bool enabled)
    {
        //GetComponent<CharacterController>().detectCollisions = !enabled;
        canControlMove = !enabled;
        //if (!enabled) { accelerationFactor = 1f; }
        //applyGravity = !enabled;
        moveCharacter = true;
        //followCameraRotation = !enabled;
        followRotation = enabled ? PlayerFollowRotation.NONE : PlayerFollowRotation.CAMERA;
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
        //tryGrabLedge = false;
        playerParkour.holdingLedge = false;
        moveSpeed = 0f; fallSpeed = 0f;
        charCont.enabled = true;
    }
    IEnumerator HardLandingAcceleration_Smooth()
    {
        // Delay from start of animation
        float startDelay = smooth_hardlanding_startDelay;
        float timer = 0f;
        if (startDelay > 0f)
        {
            while (timer < startDelay)
            {
                yield return new WaitForEndOfFrame();
                //frameAccelerationFactor = 0f;
                moveSpeed = Mathf.Lerp(smooth_hardlanding_startSpeed, 0f, timer / startDelay);
                timer += Time.deltaTime;
                if (MoveSpeedIsZero()) { timer += Time.deltaTime; }
            }
        }
        //if (startDelay > 0) { yield return new WaitForSeconds(startDelay); }

        // Reduce acceleration a lot, [optional] increase it slowly
        frameAccelerationFactor = smooth_hardlanding_accelFactor;
        canControlMove = true;
        moveSpeed = 0f; //smooth_hardlanding_startSpeed;
        timer = 0f;
        if (smooth_hardlanding_accelKeepTime > 0f)
        {
            while (timer < smooth_hardlanding_accelKeepTime)
            {
                yield return new WaitForEndOfFrame();
                frameAccelerationFactor = smooth_hardlanding_accelFactor;
                timer += Time.deltaTime;
                if (MoveSpeedIsZero()) { timer += Time.deltaTime; }
            }
        }

        // Increase it slowly
        timer = 0f;
        while (timer < smooth_hardlanding_accelIncreaseTime)
        {
            yield return new WaitForEndOfFrame();
            frameAccelerationFactor = smooth_hardlanding_accelFactor + ((timer / smooth_hardlanding_accelIncreaseTime) * (1f - smooth_hardlanding_accelFactor));
            timer += Time.deltaTime;
            if (MoveSpeedIsZero()) { timer += Time.deltaTime; }
        }
    }
    IEnumerator SlideDecellerationStart_Smooth()
    {
        float startDelay = 1f;
        if (startDelay > 0) { yield return new WaitForSeconds(startDelay); }

        currentlySliding = true;
    }


    [ObserversRpc]
    public void StartRaceCountdown_RPC()
    {
        DDOL.GetNetworkManager().GetComponent<RaceTimeManager>().StartRaceWithCountdown(this);
    }
    //[ObserversRpc]
    //public void EnablePlayerControl_RPC(bool value) { Debug.Log("[Client] EnablePlayerControl_RPC(" + value + ")"); EnablePlayerControl(value); }
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