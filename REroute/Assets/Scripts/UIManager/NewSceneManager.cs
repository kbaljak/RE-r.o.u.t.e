using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewSceneManager : MonoBehaviour
{
    private GameObject soundPlayer;
    AudioSource audioSource;

    private void Start()
    {
        soundPlayer = GameObject.FindGameObjectWithTag("buttonSound");
        if (soundPlayer != null)
        {
            audioSource = soundPlayer.GetComponent<AudioSource>();
            if (audioSource == null) { Debug.LogError("Could not find AudioSource component!"); }
        }
        else { Debug.LogError("Coudl not find GameObject with tag \"buttonSound\""); }
    }
    // ########### SCENE MANAGEMENT ########### \\
    public void SwitchToMainMenuScene()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
    public void SwitchToHostGameScene()
    {
        SceneManager.LoadScene("HostGameScene");
    }

    public void SwitchToJoinGameScene()
    {
        SceneManager.LoadScene("JoinGameScene");
    }

    public void SwitchToServerDiscoveryScene()
    {
        TMP_InputField playerNameInputFiled = GameObject.Find("Canvas/PlayerNameInputFiled").GetComponent<TMP_InputField>();
        if (playerNameInputFiled == null) { Debug.LogError("Could not find TMPro Input Filed!"); }

        string playerName = string.IsNullOrEmpty(playerNameInputFiled.text) ? "Player" : playerNameInputFiled.text;

        NetworkLobbyManager.Instance.SetPlayerName(playerName);

        SceneManager.LoadScene("ServerDiscoveryScene");
    }

    public void SwitchToSettingsScene()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    // ########### BUTTON FUNCTIONS ########### \\
    public void HostGame()
    {
        TMP_InputField hostNameInputFiled = GameObject.Find("Canvas/HostNameInputFiled").GetComponent<TMP_InputField>();
        if (hostNameInputFiled == null) { Debug.LogError("Could not find TMPro Input Filed!"); }

        string hostName = string.IsNullOrEmpty(hostNameInputFiled.text) ? "Host" : hostNameInputFiled.text;

        NetworkLobbyManager.Instance.SetHostName(hostName);
        NetworkLobbyManager.Instance.HostGame();

    }

    public void QuitGame()
    {
        //Debug.Log("Quitting game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    // ########### AUXLILIARY FUNCTIONS ########### \\
    public void PlaySoundOnButtonClick()
    {
        audioSource.Play();
    }
}
