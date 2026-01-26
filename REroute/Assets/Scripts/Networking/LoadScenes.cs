using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Rendering;

public class LoadScenes : MonoBehaviour
{
    private NetworkManager _networkManager;
    public void LoadLevelScene(string sceneName)
    {
        _networkManager = GetComponent<NetworkManager>();
        if (_networkManager == null) { Debug.LogError("Could not get Network Manager!"); return; }

        if (!_networkManager.IsServerStarted) { Debug.LogError("Only Host can load scenes!"); return; }

        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.ReplaceScenes = ReplaceOption.All;

        _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;

        Debug.Log("Scene load complete. Repositioning players...");

        GameObject spawnPointsParent = GameObject.Find("SpawnPoints");
        if (spawnPointsParent == null) { Debug.LogError("SpawnPoints GameObject not found in scene!"); return; }

        List<Transform> spawnPoints = new List<Transform>();
        for (int i = 0; i < spawnPointsParent.transform.childCount; i++)
        {
            spawnPoints.Add(spawnPointsParent.transform.GetChild(i));
        }
        if (spawnPoints.Count == 0) {  Debug.LogError("No spawn points found!"); return; }

        int playerIndex = 0;
        foreach (NetworkConnection conn in _networkManager.ServerManager.Clients.Values)
        {
            if (conn.FirstObject != null)
            {
                NetworkObject playerNetObj = conn.FirstObject;
                PlayerController playerController = playerNetObj.GetComponent<PlayerController>();

                if (playerController != null)
                {
                    Transform spawnPoint = spawnPoints[playerIndex % spawnPoints.Count];
                    
                    TeleportPlayerToSpawn(playerController, spawnPoint);
                    
                    Debug.Log($"Player {playerIndex + 1} teleported to {spawnPoint.name}");
                    playerIndex++;
                }
            }
        }
    }
    private void TeleportPlayerToSpawn(PlayerController player, Transform spawnPoint)
    {
        CharacterController charController = player.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
        }

        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        if (charController != null)
        {
            charController.enabled = true;
        }

        player.OnSceneTransition();
    }
}
