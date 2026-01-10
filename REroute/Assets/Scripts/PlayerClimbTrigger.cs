using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerClimbTrigger : MonoBehaviour
{
    public PlayerParkourDetection playerParkourDetection;

    public HashSet<Collider> detected = new HashSet<Collider>();


    private void OnTriggerEnter(Collider other) { detected.Add(other); } //playerParkourDetection.ClimbableEnter(other); }
    private void OnTriggerExit(Collider other) { detected.Remove(other); } //playerParkourDetection.ClimbableExit(other); }
}

[CustomEditor(typeof(PlayerClimbTrigger))]
public class PlayerController_Inspector : Editor
{
    PlayerClimbTrigger playerClimbTrigger;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        playerClimbTrigger = (PlayerClimbTrigger)target;


        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Detected [" + playerClimbTrigger.detected.Count + "]:");

            foreach (Collider c in playerClimbTrigger.detected)
            {
                EditorGUILayout.ObjectField(c.gameObject, typeof(GameObject), true);
            }

            if (GUILayout.Button("Dump detected"))
            {
                string s = name + "<PlayerClimbTrigger>.detected: [";
                bool first = true;
                foreach (Collider c in playerClimbTrigger.detected)
                {
                    if (first) { first = false; }
                    else { s += ", "; }
                    s += c.name;
                }
                Debug.Log(s + "]");
            }
        }
    }
}
