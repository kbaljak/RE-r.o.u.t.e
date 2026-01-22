using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(Collider))]
[ExecuteInEditMode]
public class Slope : MonoBehaviour
{
    public static float horizontalAngleLimit = 20f;

    public Vector3 direction = Vector3.zero;

#if UNITY_EDITOR
    private Collider collider;
    public static float drawLength = 4f;

    private void Reset() 
    {
        collider = GetComponent<Collider>();
        RaycastDirection(); 
        if (Slope.drawLength == 0) { Slope.drawLength = 2f; }
    }

    public bool RaycastDirection()
    {
        Ray ray = new Ray(transform.position + (Vector3.up * 2f), -Vector3.up * 4f);
        RaycastHit hit;
        if (collider.Raycast(ray, out hit, 4f))
        {
            //direction = new Vector3(hit.normal.x, -hit.normal.y, hit.normal.z);
            direction = Vector3.ProjectOnPlane(new Vector3(hit.normal.x, 0f, hit.normal.z), hit.normal).normalized;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        using (new Handles.DrawingScope(Color.purple))
        {
            Handles.DrawLine(transform.position, transform.position + (direction * drawLength));
        }
    }
#endif

    public static bool SlideCheck(Vector3 slopeDirection, Vector3 moveDirection)
    {
        Vector2 moveDirHor = new Vector2(moveDirection.x, moveDirection.z);
        float angle = Vector2.Angle(moveDirHor, new Vector2(slopeDirection.x, slopeDirection.z));
        if (angle < horizontalAngleLimit) { return true; }
        return false;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Slope))]
public class SlopeInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Slope slope = (Slope)target;

        if (GUILayout.Button("Update direction"))
        {
            bool success = slope.RaycastDirection();
            if (!success) { Debug.LogError(slope.name + "<Slope>: Failed to get direction by raycast."); }
        }

        Slope.drawLength = EditorGUILayout.FloatField("Draw length", Slope.drawLength);
    }
}
#endif