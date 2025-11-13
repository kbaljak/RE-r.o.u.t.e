using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;

public class VirtualChild : MonoBehaviour
{
    public GameObject virtualParent;

    public bool applyPosition = true;
    public Vector3 virtualLocalPosition = Vector3.zero;
    //public bool applyRotation = true;

    //Vector3 baseLocalPosition;
    //Quaternion baseLocalRotation;

    public void SetVirtualParent(GameObject player)
    {
        virtualParent = player;
    }
    private void Awake()
    {
        //baseLocalPosition = transform.localPosition;
        //baseLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (applyPosition && virtualParent != null)
        {
            transform.position = virtualParent.transform.position + (virtualParent.transform.rotation * virtualLocalPosition);
        }
        //if (applyRotation) { }
    }
}
