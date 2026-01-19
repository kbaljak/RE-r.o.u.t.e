using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class PrefabCopy_Editor : MonoBehaviour
{
    public GameObject prefab = null;
    public int count = 1;
    public Vector3 positionDelta = Vector3.zero;
    public Vector3 eulerAngles = Vector3.zero;


    public void Apply()
    {
        Vector3 position = Vector3.zero;
        for (int i = 0; i < count; ++i)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            if (i > 0) { go.name += " (" + i.ToString() + ")"; }
            go.transform.localPosition = position;
            go.transform.eulerAngles = eulerAngles;
            position += positionDelta;
        }
    }

}

[CustomEditor(typeof(PrefabCopy_Editor))]
public class PrefabCopy_Editor_Inspector : Editor
{
    PrefabCopy_Editor script;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        script = (PrefabCopy_Editor)target;

        if (GUILayout.Button("Apply"))
        {
            script.Apply();
        }
    }
}

#endif