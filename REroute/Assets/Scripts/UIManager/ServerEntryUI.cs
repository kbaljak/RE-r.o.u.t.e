using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerEntryUI : MonoBehaviour
{
    [SerializeField] Transform serverName;
    [SerializeField] Transform playerCount;
    [SerializeField] GameObject joinButton;

    private TextMeshProUGUI serverNameText;
    private TextMeshProUGUI serverPlayerCountText;

    private DiscoveredServer serverInfo;

    private void Start()
    {
        if (joinButton != null) { joinButton.GetComponent<Button>().onClick.AddListener(OnJoinButtonClicked); }

        if (serverName != null) {serverNameText = serverName.GetComponent<TextMeshProUGUI>();}
        else { Debug.LogWarning($"Could not find Text Mesh Pro component on object: {serverName}"); }

        if (playerCount != null) {serverPlayerCountText = playerCount.GetComponent<TextMeshProUGUI>(); }
        else { Debug.LogWarning($"Could not find Text Mesh Pro component on object: {playerCount}"); }
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

    private void OnJoinButtonClicked()
    {
        if (serverInfo == null) {Debug.LogError("No server info avaliable!"); }

        Debug.Log($"Joining server: {serverInfo.hostAddress}");

        //TODO:
        // create code input popup for authentication

        NetworkLobbyManager.Instance.JoinGame(serverInfo.hostAddress);
    }
    private void OnDestroy()
    {
        if (joinButton != null)
        {
            joinButton.GetComponent<Button>().onClick.RemoveListener(OnJoinButtonClicked);
        }
    }
}
