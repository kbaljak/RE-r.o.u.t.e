using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    public Transform respawnPoint;

    private void Reset()
    {
        respawnPoint = transform.GetChild(0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() is PlayerController playerCont)
        {
            playerCont.SetRespawnPoint(respawnPoint.position);
        }
    }
}
