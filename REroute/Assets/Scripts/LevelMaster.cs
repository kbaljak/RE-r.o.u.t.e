using FishNet.Managing;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMaster : MonoBehaviour
{
    public Transform[] spawnPoints;

    void Start()
    {
        NetworkManager networkManager = DDOL.GetNetworkManager();
        if (!networkManager.IsServerStarted) { return; }

        Debug.Log("Loaded level '" + SceneManager.GetActiveScene().name + "'");

        //DDOL.GetSceneLoader().TeleportPlayersToSpawnPoints(this);
        DDOL.GetSceneLoader().LevelStart(this);
    }
}
