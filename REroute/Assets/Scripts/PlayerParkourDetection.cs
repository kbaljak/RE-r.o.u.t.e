using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;


public class PlayerParkourDetection : MonoBehaviour
{
    PlayerController plCont;

    public PlayerClimbTrigger climbTrigger;
    public PlayerClimbTrigger jumpClimbTrigger;

    public float highest = 2.4f;
    public float mediumHeight = 2f;
    public float lowHeight = 1.4f;
    public float vaultHeight = 1f;

    //public bool checkForLedge = false;
    public bool checkForClimbable = false;
    HashSet<Collider> detected = new HashSet<Collider>();
    

    private void Awake()
    {
        plCont = transform.parent.GetComponent<PlayerController>();
        // Set up triggers
        BoxCollider climbTriggerCol = climbTrigger.gameObject.GetComponent<BoxCollider>();
        BoxCollider jumpClimbTriggerCol = jumpClimbTrigger.gameObject.GetComponent<BoxCollider>();
        climbTriggerCol.size = new Vector3(climbTriggerCol.size.x, highest, climbTriggerCol.size.z);
        jumpClimbTrigger.transform.localPosition = new Vector3(0, highest, 0);
        jumpClimbTriggerCol.size = new Vector3(jumpClimbTriggerCol.size.x, plCont.jumpHeight, jumpClimbTriggerCol.size.z);
        jumpClimbTriggerCol.center = new Vector3(0, -plCont.jumpHeight / 2, 0);
    }

    private void Update()
    {
        //if (checkForLedge) { CheckForLedges(); }
        if (checkForClimbable && !plCont.holdingLedge) { CheckClimbables(); }
    }

    public bool CheckClimbables()
    {
        //Debug.Log("CheckClimbables() [" + climbTrigger.detected.Count + "]");
        Collider wall = null;
        foreach (Collider collider in climbTrigger.detected)
        {
            ParkourInteractType typeDetected = CheckForInteraction(collider);
            if (typeDetected > ParkourInteractType.Wall) { DoParkourInteract(collider, typeDetected); return true; }
            wall = collider;
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
            if (typeDetected > ParkourInteractType.Wall) { DoParkourInteract(collider, typeDetected); return true; }
        }
        return false;
    }
    ParkourInteractType CheckForInteraction(Collider col)
    {
        switch (col.tag)
        {
            case "Ledge":
                float ledgeHeight = col.transform.position.y - plCont.transform.position.y;
                Debug.Log("Ledge " + col.name + " at height " + ledgeHeight + " (" + col.transform.position.y + " - " + plCont.transform.position.y + ")");
                LedgeLevel ledgeLvl;
                if (ledgeHeight >= highest) { ledgeLvl = LedgeLevel.High; }  //Debug.Log("-> " + col.name + " ledge detected at High level."); }
                else if (ledgeHeight >= mediumHeight) { ledgeLvl = LedgeLevel.Med; }  //Debug.Log("-> " + col.name + " ledge detected at Med level.");
                else if (ledgeHeight >= lowHeight) { ledgeLvl = LedgeLevel.Low; }  //Debug.Log("-> " + col.name + " ledge detected at Low level.");
                else { ledgeLvl = LedgeLevel.Vault; }  //Debug.Log("-> " + col.name + " ledge detected at Vault level.");

                plCont.JumpObstacle(col.transform, ledgeHeight, ledgeLvl, col.transform.forward);
                checkForClimbable = false;

                return ParkourInteractType.Ledge;
            default:  // wall
                Debug.Log("Wall");
                return ParkourInteractType.Wall;
        }
        return ParkourInteractType.None;
    }
    void DoParkourInteract(Collider col, ParkourInteractType interactType)
    {
        
    }


    internal void ClimbableEnter(Collider other) { detected.Add(other); }
    internal void ClimbableExit(Collider other) { detected.Remove(other); }

    public bool AnyDetected() { return detected.Count > 0; }
}


// Prioritised by 1 = least, inf = most
enum ParkourInteractType { None = -1, Wall, Ledge, VerticalClimb, Swing }