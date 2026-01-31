using FishNet.Object;
using UnityEngine;
public class ItemAnimation : NetworkBehaviour
{
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 1f;
    public float rotationSpeed = 30f;

    private Vector3 startPos;
    void Start()
    {
        startPos = transform.position;
    }
    void Update()
    {
        float amplitude = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, amplitude, startPos.z);

        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}
