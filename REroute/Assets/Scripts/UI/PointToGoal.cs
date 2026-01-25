using UnityEngine;

public class PointToGoal : MonoBehaviour
{
    [SerializeField] Transform beacon;
    [SerializeField] Transform playerCamera;
    [SerializeField] bool ignoreVertical = true;

    private RectTransform rectTransform;
    private Quaternion originalRot;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if(rectTransform == null)
        {
            Debug.LogError("PointToGoal script must be attached to a UI element with RectTransform.");
        }
    }

    private void Start()
    {
        originalRot = rectTransform.rotation;
    }

    void Update()
    {
        if (beacon == null || playerCamera == null) return;

        Vector3 directionToBeacon = beacon.position - playerCamera.position;

        if (ignoreVertical) directionToBeacon.y = 0;

        Vector3 fw = playerCamera.forward;
        if (ignoreVertical) fw.y = 0;

        float angle = Vector3.SignedAngle(fw, directionToBeacon, Vector3.up);

        rectTransform.localRotation = Quaternion.Euler(originalRot.eulerAngles.x, originalRot.eulerAngles.y, originalRot.eulerAngles.z - angle);
    }
}
