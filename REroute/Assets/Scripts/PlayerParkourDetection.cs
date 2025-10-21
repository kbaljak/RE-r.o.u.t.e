using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;


public class PlayerParkourDetection : MonoBehaviour
{
    PlayerController plCont;

    public PlayerClimbTrigger climbTrigger;
    public PlayerClimbTrigger jumpClimbTrigger;

    // Params
    public float hangingHeight = 2.2f;
    public float bracedHangHeight = 2f;
    public float lowHeight = 1.4f;
    //public float vaultHeight = 1f;

    public bool holdingLedge = false;
    public bool checkForClimbable = false;
    HashSet<Collider> detected = new HashSet<Collider>();
    

    private void Awake()
    {
        plCont = transform.parent.GetComponent<PlayerController>();

        // Set up triggers
        BoxCollider climbTriggerCol = climbTrigger.gameObject.GetComponent<BoxCollider>();
        climbTriggerCol.size = new Vector3(climbTriggerCol.size.x, hangingHeight, climbTriggerCol.size.z);
        climbTriggerCol.transform.localPosition = new Vector3(0, hangingHeight / 2f, 0);

        BoxCollider jumpClimbTriggerCol = jumpClimbTrigger.gameObject.GetComponent<BoxCollider>();
        jumpClimbTriggerCol.size = new Vector3(jumpClimbTriggerCol.size.x, plCont.jumpHeight, jumpClimbTriggerCol.size.z);
        jumpClimbTrigger.transform.localPosition = new Vector3(0, hangingHeight + (plCont.jumpHeight / 2f), 0);
    }

    private void Update()
    {
        //if (checkForLedge) { CheckForLedges(); }
        if (checkForClimbable && !holdingLedge) { CheckClimbables(); }
    }

    public bool CheckClimbables()
    {
        //Debug.Log("CheckClimbables() [" + climbTrigger.detected.Count + "]");
        Collider wall = null;
        foreach (Collider collider in climbTrigger.detected)
        {
            ParkourInteractType typeDetected = CheckForInteraction(collider);
            if (typeDetected > ParkourInteractType.Wall) { return true; }
            else if (typeDetected == ParkourInteractType.Wall) { wall = collider; }
        }
        if (wall != null) { return CheckJumpReachClimbables(); }  //DoParkourInteract(wall, ParkourInteractType.Wall); return true; }
        return false;
    }
    public bool CheckJumpReachClimbables()
    {
        //Debug.Log("CheckJumpReachClimbables() [" + jumpClimbTrigger.detected.Count + "]");
        foreach (Collider collider in jumpClimbTrigger.detected)
        {
            ParkourInteractType typeDetected = CheckForInteraction(collider);
            if (typeDetected > ParkourInteractType.Wall) { return true; }
        }
        return false;
    }
    ParkourInteractType CheckForInteraction(Collider col)
    {
        switch (col.tag)
        {
            case "Ledge":
                if (!col.GetComponent<Ledge>())
                { Debug.LogError("ERROR: No 'Ledge' object found on a GO in 'Climbable' layer and marked as 'Ledge'."); }
                InteractLedge(col.GetComponent<Ledge>());
                return ParkourInteractType.Ledge;
            default:  // wall
                Debug.Log("Wall");
                return ParkourInteractType.Wall;
        }
        return ParkourInteractType.None;
    }
    void DoParkourInteract(Parkourable target, ParkourInteractType interactType)
    {
        switch (interactType)
        {
            case ParkourInteractType.Ledge:
                InteractLedge((Ledge)target);
                break;
            default:  // Wall
                break;
        }
    }
    // Specific interactions
    void InteractLedge(Ledge ledge)
    {
        if (holdingLedge) { return; }

        float ledgeHeight = ledge.transform.position.y - plCont.transform.position.y;
        //Debug.Log("Ledge " + ledge.name + " at height " + ledgeHeight + " (" + ledge.transform.position.y + " - " + plCont.transform.position.y + ")");
        LedgeLevel ledgeLvl = GetLedgeLevel(ledgeHeight);

        Debug.Log("InteractLedge: " + ledgeHeight + ", level = " + ledgeLvl);
        if (ledgeLvl == LedgeLevel.BracedHang || ledgeLvl == LedgeLevel.Hang)
        {
            // Ledge
            checkForClimbable = false;
            holdingLedge = true;
            plCont.GrabbedLedge(ledge);
            plCont.plAnimCont.GrabOntoLedge(ledge, ledgeLvl);
        }
    }


    internal void ClimbableEnter(Collider other) { detected.Add(other); }
    internal void ClimbableExit(Collider other) { detected.Remove(other); }

    //// Utility
    public bool AnyDetected() { return detected.Count > 0; }
    LedgeLevel GetLedgeLevel(float height)
    {
        LedgeLevel ledgeLvl;
        if (height >= hangingHeight) { ledgeLvl = LedgeLevel.OutOfReach; }  //Debug.Log("-> " + col.name + " ledge detected at OutOfReach level."); }
        else if (height >= bracedHangHeight) { ledgeLvl = LedgeLevel.Hang; }  //Debug.Log("-> " + col.name + " ledge detected at Hang level.");
        else if (height >= lowHeight) { ledgeLvl = LedgeLevel.BracedHang; }  //Debug.Log("-> " + col.name + " ledge detected at BracedHang level.");
        else { ledgeLvl = LedgeLevel.Low; }  //Debug.Log("-> " + col.name + " ledge detected at Low level.");
        return ledgeLvl;
    }
}


public enum LedgeLevel { Low, BracedHang, Hang, OutOfReach }
// Prioritised by 1 = least, inf = most
enum ParkourInteractType { None = -1, Wall, Ledge, VerticalClimb, Swing }