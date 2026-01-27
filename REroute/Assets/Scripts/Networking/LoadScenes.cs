using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

public class LoadScenes : MonoBehaviour
{
    private NetworkManager _networkManager;
    [SerializeField] public List<Transform> levelSpawnPoints = new List<Transform>();
    [SerializeField] public GameObject startGameButton;
    private int playerIndex;

    private void Start()
    {
        _networkManager = GetComponent<NetworkManager>();
        if (_networkManager == null) { Debug.LogError("Could not find NetworkManager!"); }
    }
    public void TeleportPlayersToLevelArea()
    {
        foreach (NetworkConnection conn in _networkManager.ServerManager.Clients.Values)
        {
            if (conn.FirstObject != null)
            {
                NetworkObject playerNetObj = conn.FirstObject;
                
                Debug.Log($"Processing player {playerIndex + 1}: {playerNetObj.name}");
                
                // Get spawn point (cycle if more players than spawn points)
                Transform spawnPoint = levelSpawnPoints[playerIndex % levelSpawnPoints.Count];
                
                TeleportPlayerToSpawnPoint(playerNetObj, spawnPoint);
                
                Debug.Log($"Player {playerIndex + 1} teleported to {spawnPoint.name} at position {spawnPoint.position}");
                playerIndex++;
            }
            else
            {
                Debug.LogWarning($"Connection has null FirstObject!");
            }
        }
    }

    private void TeleportPlayerToSpawnPoint(NetworkObject playerNetObj, Transform spawnPoint)
    {
        // Get the PlayerController component
        PlayerController playerController = playerNetObj.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Call the RPC on the player to teleport them
            playerController.TeleportPlayerToLevelSpawnPoints(spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError($"PlayerController not found on {playerNetObj.name}!");
        }
        
        // Disable the start game prompt after first teleport
        if (playerIndex == 0)
        {
            StartGameButton strtGameBtn = startGameButton.GetComponent<StartGameButton>();
            if (strtGameBtn != null) { strtGameBtn.DisablePrompt(); }
        }
    }
}
