using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;


public class PlayerClimbTrigger : MonoBehaviour
{
    PlayerController plCont;

    public Transform topRayStartPos;
    public Transform forwardRayStartPos;
    public float highest = 2.4f;
    public float mediumHeight = 2f;
    public float lowHeight = 1.4f;
    public float vaultHeight = 1f;

    public bool checkForLedge = false;
    HashSet<Collider> detected = new HashSet<Collider>();
    

    private void Awake()
    {
        plCont = transform.parent.GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (checkForLedge) { CheckForLedges(); }
    }


    public bool CheckForLedges()
    {
        foreach (Collider collider in detected) 
        {
            if (CheckForLedge(collider)) { return true; }
        }
        return false;
    }
    bool CheckForLedge(Collider col)
    {
        RaycastHit hit;
        float fullLength = highest + 0.1f;
        Vector3 rayStartPoint = topRayStartPos.position;
        Ray ray = new Ray(rayStartPoint, Vector3.down);

        if (col.Raycast(ray, out hit, fullLength))
        {
            // Get height
            float height = (highest + 0.1f) - hit.distance;
            // Get ledge level
            LedgeLevel ledgeLvl;
            if (height >= highest) { ledgeLvl = LedgeLevel.High; }  //Debug.Log("-> " + col.name + " ledge detected at High level."); }
            else if (height >= mediumHeight) { ledgeLvl = LedgeLevel.Med; }  //Debug.Log("-> " + col.name + " ledge detected at Med level.");
            else if (height >= lowHeight) { ledgeLvl = LedgeLevel.Low; }  //Debug.Log("-> " + col.name + " ledge detected at Low level.");
            else { ledgeLvl = LedgeLevel.Vault; }  //Debug.Log("-> " + col.name + " ledge detected at Vault level.");

            // Debug
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.blue, 10);
            //Debug.Log(hit.distance + " -> " + height);

            // Get face direction
            rayStartPoint = forwardRayStartPos.position;
            //Vector3 tempDir = hit.point - plCont.playerCamera.transform.position;
            Ray rayForNormal = new Ray(rayStartPoint, forwardRayStartPos.forward);  //plCont.playerCamera.transform.position, tempDir);
            Debug.Log(plCont.playerCamera.transform.position);
            if (col.Raycast(rayForNormal, out hit, 2))
            {
                Debug.DrawRay(rayForNormal.origin, rayForNormal.direction * hit.distance, Color.blueViolet, 10);
                Debug.DrawLine(hit.point, hit.point + hit.normal * 2, Color.violetRed, 10);
                //checkForLedge = false;
                plCont.JumpObstacle(height, ledgeLvl, -hit.normal);
                return true;
            }
            else { Debug.DrawRay(rayForNormal.origin, rayForNormal.direction * 2, Color.steelBlue, 10); }

        }
        else { Debug.DrawRay(ray.origin, ray.direction * fullLength, Color.gray, 10); }
        return false;
    }


    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log(name + " climbable collided! - " + other.name);
        detected.Add(other);
    }
    private void OnTriggerExit(Collider other) { detected.Remove(other); }

    public bool AnyDetected() { return detected.Count > 0; }
}
