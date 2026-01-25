using FishNet.Demo.AdditiveScenes;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using Unity.Multiplayer.Playmode;
using UnityEngine;

/// <summary>
/// Handles network connection startup based on whether the player is hosting or joining
/// </summary>
public class NetworkConnectionStarter : MonoBehaviour
{
    public static NetworkConnectionStarter Instance {get; private set;}

    private NetworkManager _networkManager;
    private Tugboat _tugboat;
    private LANBroadcaster _lanBroadcaster;

    private string hostName;
    private string playerName;

    [Header("Connection Mode")]
    [Tooltip("For editor testing: Auto-start as host if main editor, client if virtual player")]
    [SerializeField] private bool autoConnectInEditor = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _networkManager = GetComponent<NetworkManager>();
        if (_networkManager == null) { Debug.LogError("Could not find NetworkManager component!"); }

        _tugboat = GetComponent<Tugboat>();
        if (_tugboat == null) { Debug.LogError("Could not find Tugboat component!"); }

        _lanBroadcaster = GetComponent<LANBroadcaster>();
        if (_lanBroadcaster == null) { Debug.LogError("Could not find LANBroadcaster component!"); }
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (autoConnectInEditor) { EditorAutoConnect(); }
#endif
    }

    /// <summary>
    /// Starts connection as HOST (Server + Client)
    /// Called by NetworkLobbyManager when "Host Game" is clicked
    /// </summary>
    public void StartAsHost(string hostName)
    {
        if (_networkManager == null || _tugboat == null)
        {
            Debug.LogError("Cannot start as host - NetworkManager or Tugboat not found!");
            return;
        }

        if (_networkManager.ServerManager.Started)
        {
            Debug.LogWarning("Server already started!");
            return;
        }

        Debug.Log("Starting as HOST (Server + Client)");
        
        _tugboat.StartConnection(true);     // Start server
        _tugboat.StartConnection(false);    // Start client
        
        this.hostName = hostName;

        if (_lanBroadcaster != null) { Invoke(nameof(StartBroadcast), 1.0f); }
        if (_lanBroadcaster != null) { _lanBroadcaster.StartAuthorizationListener(); }
    }

    private void StartBroadcast()
    {
        if (_lanBroadcaster != null && _networkManager.IsServerStarted) { _lanBroadcaster.StartBroadcastingGameInfo(hostName); }
    }

    /// <summary>
    /// Starts connection as CLIENT only
    /// Called by NetworkLobbyManager when "Join Game" is clicked
    /// </summary>
    public void StartAsClient(string playerName)
    {
        if (_networkManager == null || _tugboat == null)
        {
            Debug.LogError("Cannot start as client - NetworkManager or Tugboat not found!");
            return;
        }

        if (_networkManager.ClientManager.Started)
        {
            Debug.LogWarning("Client already started!");
            return;
        }

        //Debug.Log($"Starting as CLIENT - Connecting to {serverAddress}:{serverPort}");
        
        this.playerName = playerName;
        _tugboat.StartConnection(false);        // Start client
    }

    /// <summary>
    /// Auto-connect for editor testing with Virtual Players
    /// </summary>
    private void EditorAutoConnect()
    {
#if UNITY_EDITOR
        if (CurrentPlayer.IsMainEditor)
        {
            Debug.Log("Editor Auto-Connect: Starting as HOST (Main Editor)");
            StartAsHost("Host");
        }
        else
        {
            Debug.Log("Editor Auto-Connect: Starting as CLIENT (Virtual Player)");
            StartAsClient("Player");
        }
#endif
    }

    public void StopConnection()
    {
        if (_networkManager == null) return;

        Debug.Log("Stopping all connections");

        if (_networkManager.IsServerStarted) { _networkManager.ServerManager.StopConnection(true); }

        if (_networkManager.IsClientStarted) { _networkManager.ClientManager.StopConnection(); }
    }

    /// <summary>
    /// Updates server address at runtime
    /// </summary>
    public void SetServerAddress(string serverAddress)
    {
        if (_tugboat != null)
        {
            _tugboat.SetClientAddress(serverAddress);
            Debug.Log($"Server address updated to: {serverAddress}");
        }
    }

    public string GetPlayerName() { return playerName; }
    public string GetHostName() { return hostName; }
    private void OnDestroy() { StopConnection(); }
}
