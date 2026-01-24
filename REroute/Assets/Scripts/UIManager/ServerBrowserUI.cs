using System.Collections.Generic;
using UnityEngine;

public class ServerBrowserUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform serverListPanel;
    [SerializeField] private GameObject serverEntryPrefab;

    private LANDiscovery lanDiscovery;
    private Dictionary<string, GameObject> serverEntries = new Dictionary<string, GameObject>();

    private void Start()
    {
        lanDiscovery = GetComponent<LANDiscovery>();
        if (lanDiscovery == null) { Debug.LogError("Could not find component LANDiscovery!"); }

        lanDiscovery.OnServerDiscovered += OnServerDiscovered;
        lanDiscovery.OnServerLost += OnServerLost;

        lanDiscovery.StartListeningForBroadcast();
    }

    private void OnServerDiscovered(DiscoveredServer server)
    {
        if (serverEntries.ContainsKey(server.hostAddress))
        {
            UpdateServerEntry(server);
        }
        else
        {
            CreateServerEntry(server);
        }
    }

    private void OnServerLost(string serverAddress)
    {
        if (serverEntries.ContainsKey(serverAddress))
        {
            Destroy(serverEntries[serverAddress]);
            serverEntries.Remove(serverAddress);
        }
    }

    private void CreateServerEntry(DiscoveredServer server)
    {
        //Debug.Log("CreateServerEntry()");
        Debug.Log($"Server info => {server}");
        if (serverEntryPrefab == null || serverListPanel == null) { Debug.LogError("ServerEntry prefab or ServerList panel not assigned!"); return; }

        GameObject serverEntryObj = Instantiate(serverEntryPrefab, serverListPanel);
        serverEntries[server.hostAddress] = serverEntryObj;

        ServerEntryUI serverentryUI = serverEntryObj.GetComponent<ServerEntryUI>();
        if (serverentryUI != null) { serverentryUI.PopulateFileds(server); }
        else { Debug.LogError("Could not find ServerEntryUI component on ServerEntry prefab!"); }
    }

    private void UpdateServerEntry(DiscoveredServer server)
    {
        //Debug.Log("UpdateServerEntry()");
        Debug.Log($"Server info => {server}");
        if (serverEntries.TryGetValue(server.hostAddress, out GameObject serverEntryObj))
        {
            ServerEntryUI serverEntryUI = serverEntryObj.GetComponent<ServerEntryUI>();
            if (serverEntryUI != null) { serverEntryUI.UpdateInfo(server); }
            serverEntryObj.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (lanDiscovery != null)
        {
            lanDiscovery.OnServerDiscovered -= OnServerDiscovered;
            lanDiscovery.OnServerLost -= OnServerLost;
        }
    }
}
