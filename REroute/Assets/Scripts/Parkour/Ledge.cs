using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class Ledge : MonoBehaviour, Parkourable
{
    // Scriptable data
    [SerializeField] private Ledge_Data ledgeData;
    private void Reset(){ ledgeData = ScriptableObjectManager.Load<Ledge_Data>(); }
    //

    public bool vaultable = false;

    private float width;
    private List<float> playerGrabbedDeltaXs = new List<float>();


    void Awake() {
        /*if (GetComponent<Ledge_Editor>()) 
        {
            width = GetComponent<Ledge_Editor>().width;
            Destroy(GetComponent<Ledge_Editor>()); 
        }
        else
        {
            width = GetComponent<BoxCollider>().size.x; 
        }*/

        width = GetComponent<BoxCollider>().size.x;  //transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().size.x; //width = transform.lossyScale.x; 
        if (GetComponent<Ledge_Editor>()) { Destroy(GetComponent<Ledge>()); }
    }

    
    public float? PlayerGrabbed(PlayerController playerCont)
    {
        // Calculate grab X delta
        Vector3 localPlayerPos = transform.InverseTransformPoint(playerCont.transform.position);
        float deltaX = localPlayerPos.x * width;
        //Debug.Log("deltaX: " + deltaX);

        // Check if can grab
        float? insideWidthAdj = InsideWidthAdjustment(deltaX);
        if (!insideWidthAdj.HasValue) { return null; }
        if (insideWidthAdj.Value != 0)  // Means we are at an edge
        {
            if (IsBlockedByPlayer(deltaX + insideWidthAdj.Value)) { return null; }
            return deltaX + insideWidthAdj.Value;
        }

        float? playerAdj = BlockedByPlayerAdjustment(deltaX);
        if (!playerAdj.HasValue) { return null; }

        return deltaX + playerAdj.Value;
    }

    /// <summary>
    /// Has to be called when a parent or indirect parent's local scale has changed!
    /// </summary>
    public void GlobalScaleChanged()
    {
        width = transform.lossyScale.x;
    }


    // Utility
    Tuple<int, int> IndecesOfPlayersOnEachSide(float deltaX)
    {
        if (playerGrabbedDeltaXs.Count == 0) { return Tuple.Create(-1, -1); }
        int left = playerGrabbedDeltaXs.Count - 1;
        int right = -1;
        for (int i = 0; i < playerGrabbedDeltaXs.Count; ++i)
        {
            if (playerGrabbedDeltaXs[i] > deltaX) 
            {
                left = i - 1; right = i;
                break;
            }
        }
        return Tuple.Create(left, right);
    }
    bool IsInsideWidth(float deltaX) { return ((Mathf.Abs(deltaX) + (ledgeData.playerLedgeGrabWidth / 2)) - (width / 2) <= 0); }
    float? InsideWidthAdjustment(float deltaX)
    {
        // Check if horizontally missed the ledge alltogether
        float diff = (Mathf.Abs(deltaX) + (ledgeData.playerLedgeGrabWidth / 2)) - (width / 2);
        if (diff > ledgeData.missGrabPadding) { return null; }
        if (diff > 0) { return diff * -Mathf.Sign(deltaX); }
        return 0;
    }
    bool IsBlockedByPlayer(float deltaX)
    {
        if (playerGrabbedDeltaXs.Count == 0) { return false; }
        // Check if blocked by player
        if (playerGrabbedDeltaXs.Count > 0)
        {
            Tuple<int, int> deltasOnSides = IndecesOfPlayersOnEachSide(deltaX);
            int left = deltasOnSides.Item1; int right = deltasOnSides.Item2;
            if (left > -1)
            {
                float limit = playerGrabbedDeltaXs[left] + (ledgeData.playerLedgeGrabWidth);
                if (deltaX - limit > 0) { return true; }
            }
            if (right > -1)
            {
                float limit = playerGrabbedDeltaXs[right] - (ledgeData.playerLedgeGrabWidth);
                if (limit - deltaX > 0) { return true; }
            }
        }
        return false;
    }
    float? BlockedByPlayerAdjustment(float deltaX)
    {
        if (playerGrabbedDeltaXs.Count == 0) { return 0; }
        float adjustment = 0;
        // Check if blocked by player
        if (playerGrabbedDeltaXs.Count > 0)
        {
            Tuple<int, int> deltasOnSides = IndecesOfPlayersOnEachSide(deltaX);
            int left = deltasOnSides.Item1; int right = deltasOnSides.Item2;
            if (left > -1)
            {
                float limit = playerGrabbedDeltaXs[left] + (ledgeData.playerLedgeGrabWidth);
                float diff = deltaX - limit;
                if (diff > ledgeData.missGrabPadding) { return null; }
                if (diff > 0) { adjustment = diff; deltaX += diff; }
            }
            if (right > -1)
            {
                float limit = playerGrabbedDeltaXs[right] - (ledgeData.playerLedgeGrabWidth);
                float diff = limit - deltaX;
                if (diff > ledgeData.missGrabPadding) { return null; }
                if (diff > 0)
                {
                    // If adjustment != 0 means we already moved the point towards the right
                    // -> if we have to move it again, now towards the left, means we are stuck between two widths -> can't grab
                    if (adjustment != 0) { return null; }
                    adjustment = -diff; deltaX -= diff;
                }
            }
            // Check if is inside with the adjustment
            if (!IsInsideWidth(deltaX + adjustment)) { return null; }
        }
        return adjustment;
    }
}
