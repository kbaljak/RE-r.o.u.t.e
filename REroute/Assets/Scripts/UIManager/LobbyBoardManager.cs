using System.Collections.Generic;
using FishNet.Managing;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyBoardManager : MonoBehaviour
{
    public static LobbyBoardManager Instance { get; private set; }

    [SerializeField] public GameObject lobbyBoardCanvas;
    [SerializeField] public GameObject playerEntryList;
    [SerializeField] public GameObject playerEntryPrefab;
    [SerializeField] public RectTransform codeText;
    [SerializeField] public RectTransform codeString;
    private NetworkManager _networkManager;
    private List<GameObject> activePlayerEntries = new List<GameObject>();
    private InputAction openLobbyBoard;
    private string generatedLobbyCode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (NetworkLobbyManager.Instance == null) { generatedLobbyCode = "TEST12"; }
        else { generatedLobbyCode = NetworkLobbyManager.Instance.GetLobbyCode(); }
        

        if (codeText != null) {codeText.GetComponent<TextMeshProUGUI>().text = generatedLobbyCode; }
        else { Debug.LogError("Could not find CodeText UI element! Did you assign it in the editor?"); }

        if (lobbyBoardCanvas == null) { Debug.LogError("Could not find Lobby Board Cavas!"); }

        if (codeString == null) { Debug.LogError("Could not find Lobby Code String!"); }

        openLobbyBoard = InputSystem.actions.FindAction("OpenLobbyBoard");
        if (openLobbyBoard == null) { Debug.LogError("Could not find OpenEscapeMenu!");}

        _networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        if (_networkManager == null) { Debug.LogError("Could not find Network Manager in scene!"); }
    }

    private void Update()
    {
        if (openLobbyBoard.WasPressedThisFrame())
        {
            PopulateLobbyBoard();
            lobbyBoardCanvas.SetActive(true);
        }

        if (openLobbyBoard.WasReleasedThisFrame())
        {
            lobbyBoardCanvas.SetActive(false);
            ClearLobbyBoard();
        }
    }

    private void PopulateLobbyBoard()
    {
        if (_networkManager == null || playerEntryList == null || playerEntryPrefab == null) 
        { 
            Debug.LogError("Missing references for populating lobby board!");
            return; 
        }

        ClearLobbyBoard();

        if (_networkManager.IsServerStarted)
        {
            foreach (var conn in _networkManager.ServerManager.Clients.Values)
            {
                if (conn.FirstObject != null)
                {
                    PlayerController player = conn.FirstObject.GetComponent<PlayerController>();
                    if (player != null) { CreatePlayerEntry(player.GetPlayerName()); }
                }
            }
        }
        else if (_networkManager.IsClientOnlyStarted)
        {
            codeString.gameObject.SetActive(false);
            foreach (var conn in _networkManager.ClientManager.Clients.Values)
            {
                if (conn.FirstObject != null)
                {
                    PlayerController player = conn.FirstObject.GetComponent<PlayerController>();
                    if (player != null) { CreatePlayerEntry(player.GetPlayerName()); }
                }
            }
        }
        Debug.Log($"Populated lobby board with {activePlayerEntries.Count} players");
    }

    private void CreatePlayerEntry(string playerName)
    {
        GameObject entry = Instantiate(playerEntryPrefab, playerEntryList.transform);
        
        TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null) { nameText.text = playerName; }
        else { Debug.LogWarning("Player entry prefab doesn't have TextMeshProUGUI component!"); }
        
        activePlayerEntries.Add(entry);
    }
    private void ClearLobbyBoard()
    {
        foreach (GameObject entry in activePlayerEntries) { Destroy(entry); }
        activePlayerEntries.Clear();
    }
}
