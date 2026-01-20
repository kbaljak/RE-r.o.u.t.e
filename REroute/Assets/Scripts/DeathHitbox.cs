using UnityEngine;

public class DeathHitbox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() is PlayerController playerCont)
        {
            playerCont.Respawn();
        }
    }
}
