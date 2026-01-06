using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkLobbyManager : MonoBehaviour
{
    public static NetworkLobbyManager Instance { get; private set; }
    [SerializeField] string LobbySceneName = "Ian_gym";

    private bool shouldStartAsHost = false;
    private bool shouldStartAsClient = false;
    private string lobbyCode;
    private string hostName;
    private string playerName;
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

    public void HostGame(string hostName)
    {
        shouldStartAsHost = true;
        shouldStartAsClient = false;

        this.hostName = hostName;

        SceneManager.sceneLoaded += OnLobbySceneLoaded;
        SceneManager.LoadScene(LobbySceneName);
    }

    public void JoinGame(string playerName)
    {
        shouldStartAsClient = true;
        shouldStartAsHost = false;

        this.playerName = playerName;

        SceneManager.sceneLoaded += OnLobbySceneLoaded;
        SceneManager.LoadScene(LobbySceneName);
    }

    private void OnLobbySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != LobbySceneName) { return; }

        SceneManager.sceneLoaded -= OnLobbySceneLoaded;

        NetworkConnectionStarter networkConnectionStarter = GameObject.Find("NetworkManager").GetComponent<NetworkConnectionStarter>();
        if (networkConnectionStarter == null) { Debug.LogError("Could not find NetworkManager with component NetworkConnectionStarter"); return; }

        if (shouldStartAsHost) 
        {
            networkConnectionStarter.StartAsHost(hostName);

            lobbyCode = GenerateLobbyCode();
            Debug.Log($"Host: Starting lobby with code: {lobbyCode}");

            //TODO:
            // start broadcasting on LAN server name, server address, number of current players
        }
        else if (shouldStartAsClient) 
        {
            networkConnectionStarter.StartAsClient(playerName); 
        }

        shouldStartAsClient = false;
        shouldStartAsHost = false;
    }

    private string GenerateLobbyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] code = new char[6];
        System.Random random = new System.Random();
        
        for (int i = 0; i < 6; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(code);
    }
}
