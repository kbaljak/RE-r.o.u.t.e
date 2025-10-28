using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


public class PlayerParkourDetection : MonoBehaviour
{
    PlayerController plCont;

    public PlayerClimbTrigger grabTrigger;
    //public PlayerClimbTrigger wallRunTrigger;
    public PlayerClimbTrigger standJumpClimbTrigger;

    // Params
    public float hangingHeight = 2.2f;
    public float bracedHangHeight = 2f;
    public float lowHeight = 1.4f;
    //public float vaultHeight = 1f;

    public bool holdingLedge = false;
    public bool checkForClimbable = false;
    HashSet<Collider> detected = new HashSet<Collider>();

    [Space(20)]
    [Header("Debug")]
    public bool visualizeReach = false;
    public GameObject visualizeReachGO;
    
    

    private void Awake()
    {
        plCont = transform.parent.GetComponent<PlayerController>();

        // Set up triggers
        // Grab trigger
        BoxCollider grabTriggerCol = grabTrigger.gameObject.GetComponent<BoxCollider>();
        grabTriggerCol.size = new Vector3(grabTriggerCol.size.x, hangingHeight - transform.localPosition.y, grabTriggerCol.size.z);
        grabTriggerCol.transform.localPosition = new Vector3(0, (hangingHeight - transform.localPosition.y) / 2f, 0);
        // Stand jump trigger
        BoxCollider standJumpTriggerCol = standJumpClimbTrigger.gameObject.GetComponent<BoxCollider>();
        standJumpTriggerCol.size = new Vector3(standJumpTriggerCol.size.x, plCont.jumpHeight, standJumpTriggerCol.size.z);
        standJumpClimbTrigger.transform.localPosition = new Vector3(0, (hangingHeight + (plCont.jumpHeight / 2f)) - transform.localPosition.y, 0);
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
        //if (checkForLedge) { CheckForLedges(); }
        if (checkForClimbable && !holdingLedge) { CheckClimbables(); }
    }

    public Tuple<bool, bool> CheckClimbables()
    {
        //Debug.Log("CheckClimbables() [" + climbTrigger.detected.Count + "]");
        Collider wall = null;
        foreach (Collider collider in grabTrigger.detected)
        {
            ParkourInteractType typeDetected = CheckForInteraction(collider);
            if (typeDetected > ParkourInteractType.Wall) { return Tuple.Create(true, false); }
            else if (typeDetected == ParkourInteractType.Wall) { wall = collider; }
        }
        //if (wall != null) { return Tuple.Create(false, CheckJumpReachClimbables()); }
        if (plCont.isGrounded) { return Tuple.Create(false, CheckJumpReachClimbables()); }
        return Tuple.Create(false, false);
    }
    public bool CheckJumpReachClimbables()
    {
        //Debug.Log("CheckJumpReachClimbables() [" + jumpClimbTrigger.detected.Count + "]");
        foreach (Collider collider in standJumpClimbTrigger.detected)
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
                if (!InteractLedge(col.GetComponent<Ledge>())) { return ParkourInteractType.None; }
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
    bool InteractLedge(Ledge ledge)
    {
        if (holdingLedge) { return false; }

        float ledgeHeight = ledge.transform.position.y - plCont.transform.position.y;
        //Debug.Log("Ledge " + ledge.name + " at height " + ledgeHeight + " (" + ledge.transform.position.y + " - " + plCont.transform.position.y + ")");
        LedgeLevel ledgeLvl = GetLedgeLevel(ledgeHeight);

        Debug.Log("InteractLedge: " + ledgeHeight + ", level = " + ledgeLvl);
        if (ledgeLvl == LedgeLevel.OutOfReach) { return false; }
        if (ledgeLvl == LedgeLevel.BracedHang || ledgeLvl == LedgeLevel.Hang)
        {
            // Ledge
            checkForClimbable = false;
            holdingLedge = true;
            plCont.GrabbedLedge(ledge);
            plCont.plAnimCont.GrabOntoLedge(ledge, ledgeLvl);
        }
        return true;
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