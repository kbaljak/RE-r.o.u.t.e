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
    [SerializeField] public int authorizationPort = 42568;
    [SerializeField] public float broadcastInterval = 2f;   // time between broadcasts
    [SerializeField] public int maxPlayers = 4;
    [SerializeField] public string gameIdentifier = "REroute";

    private string hostName;
    private IPAddress hostAddress;
    private UdpClient udpClient;
    private UdpClient authUdpClient;
    private IPEndPoint broadcastEndPoint;
    private bool isBroadcasting = false;
    private bool isListeningForAuth = false;
    private Coroutine authListenerCoroutine;
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

    public void StartAuthorizationListener()
    {
        if (isListeningForAuth) 
        { 
            Debug.Log("Already listening for authorization requests!"); 
            return; 
        }

        try
        {
            authUdpClient = new UdpClient(authorizationPort);
            isListeningForAuth = true;
            authListenerCoroutine = StartCoroutine(AuthorizationListenerCoroutine());
            
            Debug.Log($"Started listening for authorization on port {authorizationPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start authorization listener: {e.Message}");
        }
    }

    public void StopAuthorizationListener()
    {
        if (!isListeningForAuth) { return; }
        
        isListeningForAuth = false;
        
        if (authListenerCoroutine != null) 
        { 
            StopCoroutine(authListenerCoroutine); 
            authListenerCoroutine = null; 
        }
        
        if (authUdpClient != null) 
        { 
            authUdpClient.Close(); 
            authUdpClient = null; 
        }
        
        Debug.Log("Stopped authorization listener!");
    }

    private IEnumerator AuthorizationListenerCoroutine()
    {
        while (isListeningForAuth)
        {
            if (authUdpClient != null && authUdpClient.Available > 0)
            {
                try
                {
                    IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedData = authUdpClient.Receive(ref clientEndPoint);
                    string receivedMessage = Encoding.UTF8.GetString(receivedData);
                    
                    HandleAuthorizationRequest(receivedMessage, clientEndPoint);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error receiving authorization request: {e.Message}");
                }
            }
            
            yield return null;
        }
    }

    private IEnumerator BroadcastCorutine()
    {
        while (isBroadcasting)
        {
            BroadcastGameInfo();
            yield return new WaitForSeconds(broadcastInterval);
        }
    }

    private void HandleAuthorizationRequest(string message, IPEndPoint clientEndPoint)
    {
        string[] parts = message.Split('|');
        
        if (parts.Length == 3)
        {
            if (message.StartsWith($"{gameIdentifier}|Code"))
            {
                string receivedCode = parts[2];
                string correctCode = NetworkLobbyManager.Instance.GetLobbyCode();
                bool isAuthorized = receivedCode == correctCode;

                Debug.Log($"Authorization request from {clientEndPoint.Address}: Code={receivedCode}, Valid={isAuthorized}");

                SendAuthorizationResponse(clientEndPoint, isAuthorized);
            }
        }
        else
        {
            SendAuthorizationResponse(clientEndPoint, false);
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

            //Debug.Log($"Broadcasting: {broadcastMessage} => {broadcastData}");

            udpClient.Send(broadcastData, broadcastData.Length, broadcastEndPoint);
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not broadcast: {e.Message}");
        }
    }

    private void SendAuthorizationResponse(IPEndPoint clientEndPoint, bool isAuthorized)
    {
        try
        {
            string response = $"{gameIdentifier}|Correct|{isAuthorized}";
            byte[] responseData = Encoding.UTF8.GetBytes(response);
            
            authUdpClient.Send(responseData, responseData.Length, clientEndPoint);
            
            Debug.Log($"Sent authorization response to {clientEndPoint.Address}: {isAuthorized}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send authorization response: {e.Message}");
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
        StopAuthorizationListener();
    }
    private void OnApplicationQuit()
    {
        StopBroadcastingGameInfo();
        StopAuthorizationListener();
    }
}
