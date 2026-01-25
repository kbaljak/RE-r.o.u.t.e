using System;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using UnityEngine;

public class LoadScenes : MonoBehaviour
{
    private NetworkManager _networkManager;
    public void LoadLevelScene(string sceneName)
    {
        _networkManager = GetComponent<NetworkManager>();
        if (_networkManager == null) { Debug.LogError("Could not get Network Manager!"); return; }

        foreach(NetworkConnection conn in _networkManager.ServerManager.Clients.Values)
        {
            if (conn.FirstObject != null) { _networkManager.ServerManager.Despawn(conn.FirstObject); }
        }

        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.ReplaceScenes = ReplaceOption.All;

        _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;

        foreach (NetworkConnection conn in _networkManager.ServerManager.Clients.Values) { _networkManager.ServerManager.Spawn(conn.FirstObject, conn); }
    }
}
