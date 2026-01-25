using System;
using System.Collections.Generic;
using UnityEngine;


public class PlayerParkourDetection : MonoBehaviour
{
    PlayerController plCont;

    public PlayerClimbTrigger grabTrigger;
    //public PlayerClimbTrigger wallRunTrigger;
    public PlayerClimbTrigger standJumpClimbTrigger;
    public PlayerClimbTrigger vaultTrigger;
    // Params
    public float hangingHeight = 2.2f;
    public float bracedHangHeight = 2f;
    public float lowHeight = 1.4f;
    public float minHeight;
    public float vaultHeight = 2f;

    public bool holdingLedge = false;
    public bool isVaulting = false;
    public bool checkForClimbable = false;
    //HashSet<Collider> detected = new HashSet<Collider>();
    internal Ledge targetLedge = null;
    internal float? targetGrabXDelta = null;

    [Space(20)]
    [Header("Debug")]
    public bool visualizeReach = false;
    public GameObject visualizeReachGO;
    
    

    private void Awake()
    {

        plCont = transform.parent.GetComponent<PlayerController>();
        //if (plCont != null && !plCont.IsOwner) { enabled = false; return; }
        if (!plCont.IsOwner) {return;}
        // Set up triggers
        // Grab trigger
        BoxCollider grabTriggerCol = grabTrigger.gameObject.GetComponent<BoxCollider>();
        grabTriggerCol.size = new Vector3(grabTriggerCol.size.x, hangingHeight + 0.02f, grabTriggerCol.size.z);
        grabTriggerCol.transform.localPosition = new Vector3(0, grabTriggerCol.size.y / 2f, 0);
        // Stand jump trigger
        BoxCollider standJumpTriggerCol = standJumpClimbTrigger.gameObject.GetComponent<BoxCollider>();
        standJumpTriggerCol.size = new Vector3(standJumpTriggerCol.size.x, plCont.jumpHeight + 0.02f, standJumpTriggerCol.size.z);
        standJumpClimbTrigger.transform.localPosition = new Vector3(0, hangingHeight + (standJumpTriggerCol.size.y / 2f), 0);
        // Vault trigger
        BoxCollider vaultTriggerCol = vaultTrigger.gameObject.GetComponent<BoxCollider>();
        vaultTriggerCol.size = new Vector3(vaultTriggerCol.size.x, vaultHeight + 0.02f, vaultTriggerCol.size.z);
        vaultTrigger.transform.localPosition = new Vector3(0, vaultTriggerCol.size.y / 2f, vaultTrigger.transform.localPosition.z);
    }

    private void Start()
    {
        // Debug
        if (visualizeReach)
        {
            if (visualizeReachGO == null) { Debug.LogError("ERROR: Visualize reach activated, but GO is null."); }
            else
            {
                foreach (float height in new List<float>{ hangingHeight, bracedHangHeight, lowHeight })
                {
                    GameObject go = Instantiate(visualizeReachGO, transform);
                    go.transform.localPosition = new Vector3(0, height - transform.localPosition.y, go.transform.localScale.z / 2f);
                }
            }
        }
    }

    private void Update()
    {
        if (checkForClimbable && !holdingLedge) { CheckAirClimbable(); }
    }

    // Parkour interaction checks
    public bool CheckAirClimbable()
    {
        //Debug.Log("CheckClimbables() [" + climbTrigger.detected.Count + "]");
        Collider wall = null;
        foreach (Collider collider in grabTrigger.detected)
        {
            ParkourInteractType typeDetected = CheckForInteraction(collider, false);
            if (typeDetected > ParkourInteractType.Wall) { return true; } //Tuple.Create(true, false); }
            else if (typeDetected == ParkourInteractType.Wall) { wall = collider; }
        }
        //if (wall != null) { return Tuple.Create(false, CheckJumpReachClimbables()); }
        //if (plCont.isGrounded) { return Tuple.Create(false, CheckJumpReachClimbables()); }
        //return Tuple.Create(false, false);
        return false;
    }
    public bool CheckJumpReachClimbables()
    {
        //Debug.Log("CheckJumpReachClimbables() [" + jumpClimbTrigger.detected.Count + "]");
        foreach (Collider collider in standJumpClimbTrigger.detected)
        {
            ParkourInteractType typeDetected = CheckForInteraction(collider, true);
            if (typeDetected > ParkourInteractType.Wall) { return true; }
        }
        return false;
    }
    public bool CheckVaultables()
    {
        //Debug.Log("CheckVaultables() [" + vaultTrigger.detected.Count + "]");
        foreach (Collider collider in vaultTrigger.detected)
        {
            ParkourInteractType typeDetected = CheckForInteraction(collider, true);
            if (typeDetected == ParkourInteractType.Vault) { return true; }
        }
        return false;
    }

    ParkourInteractType CheckForInteraction(Collider col, bool onGround)
    {
        switch (col.tag)
        {
            case "Ledge":
                if (!col.GetComponent<Ledge>())
                { Debug.LogError("ERROR: No 'Ledge' object found on a GO in 'Climbable' layer and marked as 'Ledge'."); }
                return InteractLedge(col.GetComponent<Ledge>(), onGround);
            default:  // Wall
                Debug.Log("CheckForInteraction() => Collided with: " + col.gameObject.name);
                return ParkourInteractType.Wall;
        }
        //return ParkourInteractType.None;
    }
    // Specific interactions
    ParkourInteractType InteractLedge(Ledge ledge, bool onGround)
    {
        if (holdingLedge) { return ParkourInteractType.None; }

        float ledgeHeight = ledge.transform.position.y - plCont.transform.position.y;
        //Debug.Log("Ledge " + ledge.name + " at height " + ledgeHeight + " (" + ledge.transform.position.y + " - " + plCont.transform.position.y + ")");

        // Vault
        if (onGround && ledge.vaultable && ledgeHeight <= vaultHeight && VaultLedge(ledge))
        {
            checkForClimbable = false; return ParkourInteractType.Vault;
        }
        
        LedgeLevel ledgeLvl = GetLedgeLevel(ledgeHeight);
        Debug.Log("InteractLedge: " + ledgeHeight + ", level = " + ledgeLvl);

        // Grab (or jump grab)
        switch (ledgeLvl)
        {
            /*
            case LedgeLevel.Low:
                if (onGround && ledge.vaultable && VaultLedge(ledge))
                { checkForClimbable = false; return ParkourInteractType.Vault;  }
                else { return ParkourInteractType.None; }
            */
            case LedgeLevel.BracedHang:
            case LedgeLevel.Hang:
            case LedgeLevel.JumpBracedHang:
            case LedgeLevel.JumpHang:
                if (GrabLedge(ledge, ledgeLvl))
                    { checkForClimbable = false; return ParkourInteractType.Ledge; }
                else { return ParkourInteractType.None; }
            default:
                return ParkourInteractType.None;
        }

        /*
        // Ledge is out of reach
        if (ledgeLvl == LedgeLevel.OutOfReach) { return ParkourInteractType.None; }
        //    (Stop checking for climbables if a valid ledge is found)
        // Jump climbables
        if (ledgeLvl == LedgeLevel.JumpHang && GrabLedge(ledge, ledgeLvl))
        {
            checkForClimbable = false;
            return ParkourInteractType.Ledge;
        }
        //Vault logic
        if (ledgeLvl == LedgeLevel.Low && 
            (onGround && ledge.vaultable && VaultLedge(ledge)))
        {
            checkForClimbable = false;
            return ParkourInteractType.Vault;
        }
        // Climb ledge
        if (GrabLedge(ledge, ledgeLvl)) { return ParkourInteractType.Ledge; }
        return ParkourInteractType.None;
        */
    }

    bool GrabLedge(Ledge ledge, LedgeLevel ledgeLvl)
    {
        targetLedge = ledge;
        float? deltaX = ledge.PlayerGrabbed(plCont);
        if (!deltaX.HasValue) { return false; }
        targetGrabXDelta = deltaX;

        holdingLedge = true;
        plCont.GrabbedLedge(ledge);
        plCont.plAnimCont.GrabOntoLedge(ledge, ledgeLvl);
        return true;
    }
    bool VaultLedge(Ledge ledge)
    {
        targetLedge = ledge;
        float? deltaX = ledge.PlayerGrabbed(plCont);
        if (!deltaX.HasValue) { return false; }
        targetGrabXDelta = deltaX;

        isVaulting = true;
        plCont.GrabbedLedge(ledge, false);
        plCont.plAnimCont.VaultOverLedge(1);
        return true;
    }
    public void ClimbedLedge()
    {
        //SetGrabLedgeVars_RPC(null, null);
        targetLedge = null;
        targetGrabXDelta = null;
    }

    //// Utility
    LedgeLevel GetLedgeLevel(float height)
    {
        LedgeLevel ledgeLvl;
        if (height > hangingHeight + plCont.jumpHeight) { ledgeLvl = LedgeLevel.OutOfReach; }
        else if (height > bracedHangHeight + plCont.jumpHeight) { ledgeLvl = LedgeLevel.JumpHang; }
        else if (height > hangingHeight) { ledgeLvl = LedgeLevel.JumpBracedHang; }  //Debug.Log("-> " + col.name + " ledge detected at OutOfReach level."); }
        else if (height > bracedHangHeight) { ledgeLvl = LedgeLevel.Hang; }  //Debug.Log("-> " + col.name + " ledge detected at Hang level.");
        else if (height >= lowHeight) { ledgeLvl = LedgeLevel.BracedHang; }  //Debug.Log("-> " + col.name + " ledge detected at BracedHang level.");
        else { ledgeLvl = LedgeLevel.Low; }  //Debug.Log("-> " + col.name + " ledge detected at Low level.");
        return ledgeLvl;
    }
}


public enum LedgeLevel { Bump, Low, BracedHang, Hang, JumpBracedHang, JumpHang, OutOfReach }
// Prioritised by 1 = least, inf = most
enum ParkourInteractType { None = -1, Wall, Ledge, Vault, Swing }