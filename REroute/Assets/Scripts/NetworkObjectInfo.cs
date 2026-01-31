using FishNet.Object;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkObjectInfo : MonoBehaviour
{
    public NetworkObject no;

    private void Reset() => no = GetComponent<NetworkObject>();
}

#if UNITY_EDITOR
[CustomEditor(typeof(NetworkObjectInfo))]
public class NetworkObjectInfo_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        NetworkObjectInfo noi = (NetworkObjectInfo)target;

        EditorGUILayout.TextField("Owner client ID: ", noi.no.Owner.ClientId.ToString());
    }
}
#endif