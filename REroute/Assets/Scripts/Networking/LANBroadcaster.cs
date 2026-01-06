using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FishNet.Managing;
using UnityEngine;

public class LANBroadcaster : MonoBehaviour
{
    [Header("Broadcast Settings")]
    [SerializeField] public int broadcastPort = 42567;
    [SerializeField] public float broadcastInterval = 1f;   // time between broadcasts
    [SerializeField] public int maxPlayers = 4;
    [SerializeField] public string gameIdentifier = "REroute";

    private string hostName;
    private IPAddress hostAddress;
    private UdpClient udpClient;
    private IPEndPoint broadcastEndPoint;
    private bool isBroadcasting = false;
    private Coroutine broadcastCorutine;

    private NetworkManager _networkManager;

    private void Awake()
    {
        _networkManager = GetComponent<NetworkManager>();
        if (_networkManager == null) { Debug.LogError("Could not find NetworkManager component!");}
    }

    public bool IsBroadcasting => isBroadcasting;
    public void StartBroadcastingGameInfo(string hostName)
    {
        if (isBroadcasting) { Debug.Log("Already broadcasting!"); return; }

        this.hostName = hostName;
        hostAddress = GetLocalIPAddress();
        Debug.Log($"My address is : {hostAddress}");

        try
        {
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;
            broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

            isBroadcasting = true;
            broadcastCorutine = StartCoroutine(BroadcastCorutine());

            Debug.Log($"Started broadcasting from {hostAddress}:{broadcastPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start broadcasting game info: {e.Message}");
        }
    }

    public void StopBroadcastingGameInfo()
    {
        if (!isBroadcasting) { return; }
        isBroadcasting = false;

        if (broadcastCorutine != null) { StopCoroutine(broadcastCorutine); broadcastCorutine = null; }

        if (udpClient != null) { udpClient.Close(); udpClient = null; }

        Debug.Log("Stopped broadcasting game info!");
    }

    private IEnumerator BroadcastCorutine()
    {
        while (isBroadcasting)
        {
            BroadcastGameInfo();
            yield return new WaitForSeconds(broadcastInterval);
        }
    }

    private void BroadcastGameInfo()
    {
        if (udpClient == null && _networkManager == null) { return; }

        try
        {
            int playerCount = _networkManager.ServerManager.Clients.Count;

            string broadcastMessage = $"{gameIdentifier}|{hostName}|{hostAddress}|{playerCount}|{maxPlayers}";   // REroute|Bob|192.168.1.105|2|4
            byte[] broadcastData = Encoding.UTF8.GetBytes(broadcastMessage);

            Debug.Log($"Broadcasting: {broadcastMessage} => {broadcastData}");

            udpClient.Send(broadcastData, broadcastData.Length, broadcastEndPoint);
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not broadcast: {e.Message}");
        }
    }

    private IPAddress GetLocalIPAddress()
    {
        try
        {
            string hName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hName);

            foreach(IPAddress addr in addresses)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr)) { return addr; }
            }
            return IPAddress.Loopback;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting local IP address via local DNS: {e.Message}");
            return IPAddress.Loopback;
        }
    }
    private void OnDestroy()
    {
        StopBroadcastingGameInfo();
    }
    private void OnApplicationQuit()
    {
        StopBroadcastingGameInfo();
    }
}
