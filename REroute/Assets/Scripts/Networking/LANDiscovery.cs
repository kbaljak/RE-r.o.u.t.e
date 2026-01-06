using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class LANDiscovery : MonoBehaviour
{
    [Header("Discovery Settings")]
    [SerializeField] public int listenPort = 42567;
    [SerializeField] public float serverTimeout = 5f;   //Remove server after serverTimeout seconds if no broadcast received
    [SerializeField] public string gameIdentifier = "REroute";

    private Dictionary<string, DiscoveredServer> discoveredServers = new Dictionary<string, DiscoveredServer>();

    private UdpClient udpClient;
    private Thread _listenThread;
    private bool isListening = false;

    private Queue<DiscoveredServer> serverUpdateQueue = new Queue<DiscoveredServer>();
    private readonly object queueLock = new object();

    public event Action<DiscoveredServer> OnServerDiscovered;
    public event Action<string> OnServerLost;

    private void Start()
    {
        StartListeningForBroadcast();
    }

    private void Update()
    {
        ProcessServerUpdates();
        CheckForTimedOutServers();
    }

    public void StartListeningForBroadcast()
    {
        if (isListening) { Debug.Log("Already listening for broadcasts!"); return; }

        try
        {
            udpClient = new UdpClient(listenPort);
            isListening = true;

            _listenThread = new Thread(ListenForBroadcasts);
            _listenThread.IsBackground = true;
            _listenThread.Start();

            Debug.Log($"Started listening for game broadcasts on port {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start listener: {e.Message}");
        }
    }

    public void StopListeningForBroadcasts()
    {
        if (!isListening) { return; }

        if (udpClient != null) { udpClient.Close(); udpClient = null; }

        if (_listenThread != null && _listenThread.IsAlive) { _listenThread.Join(1000); _listenThread = null; }

        discoveredServers.Clear();

        Debug.Log("Stopped listening for broadcasts");
    }

    private void ListenForBroadcasts()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isListening)
        {
            try
            {
                byte[] broadcastData = udpClient.Receive(ref remoteEP);
                string broadcastMessage = Encoding.UTF8.GetString(broadcastData);

                if (broadcastMessage.StartsWith(gameIdentifier))
                {
                    string[] messageParts = broadcastMessage.Split('|');
                    if (messageParts.Length == 5)
                    {
                        string hostName = messageParts[1] + "'s Game";
                        string hostAddress = messageParts[2];
                        int playerCount = int.Parse(messageParts[3]);
                        int maxPlayerCount = int.Parse(messageParts[4]);

                        DiscoveredServer discoveredServer = new DiscoveredServer
                        {
                            hostName = hostName,
                            hostAddress = hostAddress,
                            connectedPlayerCount = playerCount,
                            maxPlayerCount = maxPlayerCount,
                            lastBroadcast = DateTime.Now
                        };

                        Debug.Log(discoveredServer.ToString());

                        lock(queueLock) { serverUpdateQueue.Enqueue(discoveredServer); }
                    }
                    else
                    {
                        throw new Exception($"Expected length of 5 but got message of length: {messageParts.Length}");
                    }
                }
            }
            catch (SocketException)
            {
                Debug.LogError("Socket excpetion!");
                break;
            }
            catch (Exception e)
            {
                if (isListening) { Debug.LogError($"Error receiving broadcast: {e.Message}"); break; }
            }
        }
    }

    private void ProcessServerUpdates()
    {
        lock (queueLock)
        {
            while (serverUpdateQueue.Count > 0)
            {
                DiscoveredServer server = serverUpdateQueue.Dequeue();

                string dictKey = server.hostAddress;

                bool isNewServerDiscovered = !discoveredServers.ContainsKey(dictKey);
                discoveredServers[dictKey] = server;

                if (isNewServerDiscovered) { Debug.Log($"Discovered new server => [{server.hostName}'s Game] @ {server.hostAddress}"); /*OnServerDiscovered?.Invoke(server);*/ }
                OnServerDiscovered?.Invoke(server);
            }
        }
    }

    private void CheckForTimedOutServers()
    {
        List<string> serversToRemove = new List<string>();
        DateTime currentDT = DateTime.Now;

        foreach(var server in discoveredServers)
        {
            double secondsSinceLastBroadcast = (currentDT - server.Value.lastBroadcast).TotalSeconds;
            if (secondsSinceLastBroadcast > serverTimeout) { serversToRemove.Add(server.Key); }
        }

        foreach(string serverAddr in serversToRemove)
        {
            Debug.Log($"Server {serverAddr} timed out! Removing from list!");
            discoveredServers.Remove(serverAddr);
            OnServerLost?.Invoke(serverAddr);
        }
    }
    public List<DiscoveredServer> GetDiscoveredServers()
    {
        return new List<DiscoveredServer>(discoveredServers.Values);
    }

    private void OnDestroy()
    {
        StopListeningForBroadcasts();
    }

    private void OnApplicationQuit()
    {
        StopListeningForBroadcasts();
    }
}
