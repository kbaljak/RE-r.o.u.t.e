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
    [SerializeField] public List<Transform> levelSpawnPoints = new List<Transform>();
    [SerializeField] public GameObject startGameButton;
    private int playerIndex;

    private void Start()
    {
        _networkManager = DDOL.GetDDOL().transform.Find("NetworkManager").GetComponent<NetworkManager>();
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
        PlayerController playerController = playerNetObj.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TeleportPlayerToLevelSpawnPoints(spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError($"PlayerController not found on {playerNetObj.name}!");
        }
        
        if (playerIndex == 0)
        {
            StartGameButton strtGameBtn = startGameButton.GetComponent<StartGameButton>();
            if (strtGameBtn != null) { strtGameBtn.DisablePrompt(); }
        }

        RaceTimeManager.Instance.StartRaceWithCountdown();
    }

    public void LoadLevel(string sceneName)
    {
        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.ReplaceScenes = ReplaceOption.All;

        SceneManager.LoadGlobalScenes(sld);
    }
}
