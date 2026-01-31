using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

//[RequireComponent(typeof(VirtualChild))]
public class PlayerCameraController : MonoBehaviour
{
    public Transform target;
    VirtualChild virtualChildComp;
    Vector3 virtualChildBaseLocalPosition;
    public CinemachineCamera tpvCamera;

    // Input
    InputAction lookAction;

    public bool thirdPersonView = true;

    public float sensitivity = 1f;
    public float maxVertAngle = 45f;
    Vector3 rotation = Vector3.zero;

    public float lookUpPosDelta = 0f;
    public float lookDownPosDelta = 0.1f;


    private void Awake()
    {
        virtualChildComp = GetComponent<VirtualChild>();
        if (virtualChildComp) { virtualChildBaseLocalPosition = virtualChildComp.virtualLocalPosition; }

        //if (thirdPersonView) { }
        //else { }
    }

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
        //var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
        //var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);
        //target.rotation = xQuat * yQuat;

        // Update local z position based on vertical angle
        float factor = (rotation.y / maxVertAngle);
        float posDelta = (factor > 0 ? (lookUpPosDelta) : (-lookDownPosDelta)) * factor;
        //if (virtualChildComp) { GetComponent<VirtualChild>().virtualLocalPosition.z = posDelta; }
        //else { transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, posDelta); }
    }


    public void PositionEffect(Vector3 delta, float duration) 
    {
        StartCoroutine(PositionEffect_Coroutine(delta, duration));

        //tpvCamera.GetComponent<CinemachineThirdPersonFollow>().Damping = new Vector3(0, 0, 0);
    }
    IEnumerator PositionEffect_Coroutine(Vector3 fullDelta, float duration)
    {
        Debug.Log("Position effect coroutine");


        Vector3 delta = fullDelta / duration;
        Vector3 target = virtualChildBaseLocalPosition + fullDelta;
        // Move
        float timer = duration;
        while (timer > 0f)
        {
            float timeDelta = Time.deltaTime;
            timer -= timeDelta;
            if (timer < 0f) { timeDelta += timer; }
            if (virtualChildComp) { virtualChildComp.virtualLocalPosition = Vector3.Slerp(target, virtualChildBaseLocalPosition, (timer / duration)); }  //virtualChildComp.virtualLocalPosition += delta * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        // Return
        //Debug.Log("Return");
        timer = duration;
        while (timer > 0f)
        {
            float timeDelta = Time.deltaTime;
            timer -= timeDelta;
            if (timer < 0f) { timeDelta += timer; }
            if (virtualChildComp) { virtualChildComp.virtualLocalPosition = Vector3.Slerp(virtualChildBaseLocalPosition, target, (timer / duration)); }  //virtualChildComp.virtualLocalPosition -= delta * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }
}
