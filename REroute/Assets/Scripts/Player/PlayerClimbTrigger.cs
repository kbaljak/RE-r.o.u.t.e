using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbTrigger : MonoBehaviour
{
    public PlayerParkourDetection playerParkourDetection;

    public HashSet<Collider> detected = new HashSet<Collider>();


    private void OnTriggerEnter(Collider other) { detected.Add(other); } //playerParkourDetection.ClimbableEnter(other); }
    private void OnTriggerExit(Collider other) { detected.Remove(other); } //playerParkourDetection.ClimbableExit(other); }
}
