using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private Transform playerCameraTrans;

    private void Start()
    {
        playerCameraTrans = GameObject.FindGameObjectWithTag("camPoint").transform;
        if (playerCameraTrans == null) { Debug.LogError("Could not find camera!"); }
    }
    private void LateUpdate()
    {
        transform.LookAt(transform.position + playerCameraTrans.rotation * Vector3.forward, playerCameraTrans.rotation * Vector3.up);
    }
}
