using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    // Input
    InputAction lookAction;

    public float sensitivity = 1f;
    public float maxVertAngle = 45f;
    Vector3 rotation = Vector3.zero;

    public float lookUpPosDelta = 0f;
    public float lookDownPosDelta = 0.1f;


    void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
    }

    void Update()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        if (look != Vector2.zero)
        {
            rotation.x += look.x * sensitivity;
            rotation.y += look.y * sensitivity;
            rotation.y = Mathf.Clamp(rotation.y, -maxVertAngle, maxVertAngle);
            var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
            var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);
            transform.rotation = xQuat * yQuat;
        }

        // Update local z position based on vertical angle
        float factor = (rotation.y / maxVertAngle);
        float posDelta = (factor > 0 ? (lookUpPosDelta) : (-lookDownPosDelta)) * factor;
        if (GetComponent<VirtualChild>()) { GetComponent<VirtualChild>().virtualLocalPosition.z = posDelta; }
        else { transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, posDelta); }
    }
}
