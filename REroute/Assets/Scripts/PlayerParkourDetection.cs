using System;
using System.Collections.Generic;
using UnityEngine;


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
    internal Ledge targetLedge = null;
    internal float? targetGrabXDelta = null;

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
        if (!plCont.IsOwner) return;
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
                return InteractLedge(col.GetComponent<Ledge>());
            default:  // wall
                Debug.Log("Wall");
                return ParkourInteractType.Wall;
        }
        //return ParkourInteractType.None;
    }
    // Specific interactions
    ParkourInteractType InteractLedge(Ledge ledge)
    {
        if (holdingLedge) { return ParkourInteractType.None; }

        float ledgeHeight = ledge.transform.position.y - plCont.transform.position.y;
        //Debug.Log("Ledge " + ledge.name + " at height " + ledgeHeight + " (" + ledge.transform.position.y + " - " + plCont.transform.position.y + ")");
        LedgeLevel ledgeLvl = GetLedgeLevel(ledgeHeight);

        Debug.Log("InteractLedge: " + ledgeHeight + ", level = " + ledgeLvl);

        // Ledge is out of reach
        if (ledgeLvl == LedgeLevel.OutOfReach) { return ParkourInteractType.None; }

        checkForClimbable = false;
        if (ledgeLvl == LedgeLevel.Low)             // Vault possibility
        {
            if (plCont.isGrounded && true)  // Always vault if grounded for now
            {

                return ParkourInteractType.Vault;
            }
        }

        // Climb ledge
        if (!GrabLedge(ledge, ledgeLvl)) { return ParkourInteractType.None; }       
        return ParkourInteractType.Ledge;
    }

    bool GrabLedge(Ledge ledge, LedgeLevel ledgeLvl)
    {
        targetLedge = ledge;
        float? deltaX = ledge.PlayerGrabbed(plCont);
        if (!deltaX.HasValue) { return false; }
        targetGrabXDelta = deltaX;
        //SetGrabLedgeVars_RPC(ledge, deltaX.Value);

        holdingLedge = true;
        plCont.GrabbedLedge(ledge);
        plCont.plAnimCont.GrabOntoLedge(ledge, ledgeLvl);
        return true;
    }

    public void ClimbedLedge()
    {
        //SetGrabLedgeVars_RPC(null, null);
        targetLedge = null;
        targetGrabXDelta = null;
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
enum ParkourInteractType { None = -1, Wall, Ledge, Vault, Swing }