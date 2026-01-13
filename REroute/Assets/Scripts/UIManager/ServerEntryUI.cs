using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerEntryUI : MonoBehaviour
{
    public static ServerEntryUI Instance {get; private set;}
    [SerializeField] Transform serverName;
    [SerializeField] Transform playerCount;
    [SerializeField] GameObject joinButton;    

    private GameObject codeInput;
    private TextMeshProUGUI serverNameText;
    private TextMeshProUGUI serverPlayerCountText;
    private DiscoveredServer serverInfo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void Start()
    {
        if (joinButton != null) { joinButton.GetComponent<Button>().onClick.AddListener(OnJoinButtonClicked); }

        if (serverName != null) {serverNameText = serverName.GetComponent<TextMeshProUGUI>();}
        else { Debug.LogWarning($"Could not find Text Mesh Pro component on object: {serverName}"); }

        if (playerCount != null) {serverPlayerCountText = playerCount.GetComponent<TextMeshProUGUI>(); }
        else { Debug.LogWarning($"Could not find Text Mesh Pro component on object: {playerCount}"); }
    }
    private void OnJoinButtonClicked()
    {
        if (serverInfo == null) {Debug.LogError("No server info avaliable!"); }
        
        codeInput = GameObject.Find("Canvas/CodeInput");
        if (codeInput == null) { Debug.LogError("Could not find GameObject CodeInput!"); }

        CodeInputDialogController codeInputDialogController = codeInput.GetComponent<CodeInputDialogController>();
        if (codeInputDialogController != null) { codeInputDialogController.ShowCodeInputDialog(true); }
        else { Debug.LogError("Could not find component CodeInputDialogController"); }
    }
    private void OnDestroy()
    {
        if (joinButton != null)
        {
            joinButton.GetComponent<Button>().onClick.RemoveListener(OnJoinButtonClicked);
        }
    }
    public void PopulateFileds(DiscoveredServer server)
    {
        serverInfo = server;
        UpdateInfo(server);
    }

    public void UpdateInfo(DiscoveredServer server)
    {
        serverInfo = server;
        if (serverName != null)
        {
            if (serverNameText != null) { serverNameText.text = server.hostName; }
        }
        if (playerCount != null)
        {
            if (serverPlayerCountText != null) { serverPlayerCountText.text = $"{server.connectedPlayerCount}/{server.maxPlayerCount}"; }
        }
    }

    public DiscoveredServer getDiscoveredServerInfo()
    {
        return serverInfo;
    }
}
