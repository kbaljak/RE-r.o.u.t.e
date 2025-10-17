using System;
using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    CharacterController charCont;
    public PlayerAnimationController plAnimCont;
    public PlayerClimbTrigger plClmbTrig;
    public GameObject playerCamera;
    public Transform cameraPoint;

    // Input
    InputAction moveAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction attackAction;
    InputAction crouchAction;

    /// Movement
    // Is the character moved each frame
    public bool moveCharacter = true;
    public bool applyGravity = true;
    // Is the player currently controllable
    public bool canControlMove = true;
    public Transform groundRaycastPoint;
    public bool isGrounded = false;  public bool characterControllerIsGrounded;
    // Predict landing for roll input
    public bool groundPredicted = false;
    public float predictionTime = 0.1f;
    public bool roll = false;

    public Vector3 groundSlopeNormal = Vector3.up;
    float groundFriction = 1f;
    // Player velocities
    public float moveSpeed = 0f;
    /// <summary>false = linear; true = quadratic</summary>
    public bool linearOrQuadraticRunAccel = false;
    public Vector3 moveDirection = Vector3.forward;
    bool? backwardMovement = null;
    public float fallSpeed = 0f;

    internal bool followCameraRotation = true;
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

    /// Smooth transitions
    // Roll
    public float smooth_roll_targetSpeedDiff;
    public float smooth_roll_duration;
    public float smooth_roll_startDelay;

    // Debug
    public float moveSpeedSigned = 0f;
    public float moveAngle = 0f;
    public float lookAngleSigned = 0f;
    public float lookAngleSignedAnim = 0f;


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

    //// Update
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
        }
        else
        {
            plAnimCont.anim.SetFloat("lookAngleSign", 0);
        }
    }
    void Update_FootMovement()
    {
        // Check if grounded no matter if we can move
        Update_Grounded();
        if (!moveCharacter) { return; }

        //// Movement
        // Get relevant orientation vectors on the plane we are standing on
        Vector3 cameraForward = Vector3.ProjectOnPlane(new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z), groundSlopeNormal);
        Vector3 cameraRight = Vector3.ProjectOnPlane(new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z), groundSlopeNormal);
        Vector3 transformForward = Vector3.ProjectOnPlane(new Vector3(transform.forward.x, 0, transform.forward.z), groundSlopeNormal);
        if (canControlMove)
        {
            // Get forward and right direction for movement
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector3 frameMoveDir = (cameraRight * input.x) + (cameraForward * input.y).normalized;
            // Get angles
            float angleMoveToVel = Vector3.Angle(frameMoveDir, moveDirection);
            lookAngleSigned = Vector3.SignedAngle(cameraForward, transformForward, groundSlopeNormal);
            if (isGrounded && fallSpeed >= 0)  // = if we are grounded and our current vertical direction is downward or zero/none (otherwise we are going up)
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
                        float angleToVelFactor = Mathf.Clamp(angleMoveToVel / 90f, 0f, 1f);
                        float curMaxSpeed = input.y > 0 ? runMaxSpeed : runStartSpeed;

                        //Debug.Log(moveSpeed + " + " + accelFactor + " -> " + Mathf.Clamp(moveSpeed + accelFactor * Time.deltaTime, 0, curMaxSpeed) + " / " + curMaxSpeed);

                        // Just redirect speed
                        if (angleMoveToVel < 4f)
                        {
                            moveDirection = frameMoveDir;
                            if (moveSpeed < runStartSpeed) { moveSpeed = Mathf.Clamp(moveSpeed + (runStartSpeed * (Time.deltaTime / runStartAccelTime)), 0, curMaxSpeed); }
                            else
                            {
                                float t = (moveSpeed - runStartSpeed) / (runMaxSpeed - runStartSpeed);
                                // Acceleration function
                                float c = linearOrQuadraticRunAccel ? (2.0f * (1.0f - t)) : (1.0f);
                                moveSpeed = Mathf.Clamp(moveSpeed + ((Time.deltaTime / runAccelTime) * c * (runMaxSpeed - runStartSpeed)), 0, curMaxSpeed);
                                //moveSpeed = runStartSpeed + (c * (runMaxSpeed - runStartSpeed));
                            }
                        }
                        // Slow down speed depending on angle and smoothly change direction
                        else if (angleMoveToVel < 90f)
                        {
                            // Slow down
                            moveSpeed = Mathf.Clamp(moveSpeed - (angleToVelFactor * Time.deltaTime), runStartSpeed, runMaxSpeed); //Mathf.Lerp(moveSpeed, runStartSpeed, angleToVelFactor * Time.deltaTime);
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
        // Gravity
        fallSpeed -= Physics.gravity.y * Time.deltaTime;

        // Update stats
        moveSpeedSigned = moveSpeed;
        if (moveSpeed > 0) { moveSpeedSigned *= (backwardMovement != null && backwardMovement.Value ? -1 : 1); }  //angleLookToVel > 90f
        moveAngle = Vector3.SignedAngle(cameraForward, moveDirection, groundSlopeNormal);
        if (backwardMovement != null && backwardMovement.Value) { moveAngle = (180f - Mathf.Abs(moveAngle)) * Mathf.Sign(moveAngle); }

        // Combine velocities and move
        Vector3 frameVelocity = Vector3.zero;
        if (moveCharacter) { frameVelocity += moveDirection * moveSpeed; }
        frameVelocity += fallSpeed * -Vector3.up;
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
        if (crouchAction.IsPressed()) { DropOffLedge(); }
    }
    void Update_AnimatorParams()
    {
        Animator anim = plAnimCont.anim;
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("moveSpeed", moveSpeedSigned);
        anim.SetFloat("moveAngle", moveAngle);
        anim.SetFloat("fallSpeed", fallSpeed);
    }
    void Update_Grounded()
    {
        characterControllerIsGrounded = charCont.isGrounded;
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
        else if (!value)
        {
            // Reset move direction plane
            if (groundSlopeNormal != Vector3.up)
            {
                moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
                groundSlopeNormal = Vector3.up;
            }
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

        if (value)
        {
            if (!isGrounded) { Landed(); }
        }
        isGrounded = value;
    }

    //// Main
    // Vertical movement actions
    void Jump()
    {
        groundPredicted = false;
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
        Debug.Log("Landed()");
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
                if (roll) { StartCoroutine(RollSpeedLoss_Smooth()); } //moveSpeed *= 0.5f; }
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



    //// Common
    Vector3 GetCurrentVelocity()
    {
        Vector3 totalVelocity = moveDirection * moveSpeed;
        totalVelocity += fallSpeed * -Vector3.up;
        return totalVelocity;
    }


    //// External utility
    public void RootMotionMovement(bool enabled, bool keepMoving = false)
    {
        GetComponent<CharacterController>().detectCollisions = !enabled; //playerCont.GetComponent<CharacterController>().enabled = true;
        canControlMove = !enabled;
        applyGravity = !enabled;
        moveCharacter = !enabled || keepMoving;
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
}



[CustomEditor(typeof(PlayerController))]
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
            playerCont.walkSpeed = EditorGUILayout.FloatField("Walk speed", playerCont.walkSpeed);
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
}