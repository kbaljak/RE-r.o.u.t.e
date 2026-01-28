using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;
using UnityEngine;

public class LoadScenes : NetworkBehaviour
{
    private NetworkManager _networkManager;
    [SerializeField] public GameObject startGameButton;

    private void Start()
    {
        _networkManager = DDOL.GetDDOL().transform.Find("NetworkManager").GetComponent<NetworkManager>();
        if (_networkManager == null) { Debug.LogError("Could not find NetworkManager!"); }
    }

    public void LevelStart(LevelMaster levelMaster)
    {
        if (!IsServerStarted) { return; }
        TeleportPlayersToSpawnPoints(levelMaster.spawnPoints);
    }
    [Server]
    void TeleportPlayersToSpawnPoints(Transform[] levelSpawnPoints)
    {
        NetworkManager networkManager = DDOL.GetNetworkManager();

        int playerIndex = 0;
        foreach (NetworkConnection conn in networkManager.ServerManager.Clients.Values)
        {
            if (conn.FirstObject != null)
            {
                NetworkObject playerNetObj = conn.FirstObject;
                
                Debug.Log($"Processing player {playerIndex + 1}: {playerNetObj.name}");
                
                Transform spawnPoint = levelSpawnPoints[playerIndex % levelSpawnPoints.Length];
                
                TeleportPlayerToSpawnPoint(playerNetObj, spawnPoint);
                
                Debug.Log($"Player {playerIndex + 1} teleported to {spawnPoint.name} at position {spawnPoint.position}");
                playerIndex++;
            }
            else
            {
                Debug.LogWarning($"Connection has null FirstObject!");
            }
        }

        Debug.Log("[Server] Starting race countdown sequence...");

        foreach (PlayerController plCont in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            plCont.StartRaceCountdown_RPC();
        }
    }
    private static void TeleportPlayerToSpawnPoint(NetworkObject playerNetObj, Transform spawnPoint)
    {
        PlayerController playerController = playerNetObj.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TeleportPlayerToLevelSpawnPoints(spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError($"PlayerController not found on {playerNetObj.name}!");
        }
    }

    public void LoadLevel(string sceneName)
    {
        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        sld.MovedNetworkObjects = new NetworkObject[players.Length];
        for (int i = 0; i < players.Length; ++i) { sld.MovedNetworkObjects[i] = players[i]; }

        SceneManager.LoadGlobalScenes(sld);
    }
}
