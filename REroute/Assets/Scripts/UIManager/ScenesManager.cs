using UnityEngine;
using UnityEngine.SceneManagement;
public class ScenesManager : MonoBehaviour
{
    GameObject soundPlayer;
    AudioSource audioSource;
    void Start()
    {
        soundPlayer = GameObject.FindGameObjectWithTag("buttonSound");
        audioSource = soundPlayer.GetComponent<AudioSource>();
    }
    public void HostGameScene()
    {
        //SceneManager.LoadScene("HostGameScene");
        SceneManager.LoadScene("Ian_gym");
    }

    public void JoinGameScene()
    {
        SceneManager.LoadScene("JoinGameScene");
    }

    public void SettingsScene()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    public void MainMenueScene()
    {
        SceneManager.LoadScene("MainMenueScene");
    }

    public void JoinGame()
    {
        //Debug.Log("This will join the game!");
        // start Clinet
        SceneManager.LoadScene("Ian_gym");
    }

    public void QuitGame()
    {
        Debug.Log("This will quit the game!");
        Application.Quit();
    }

    public void PlaySoundOnButtonClick()
    {
        audioSource.Play();
    }
}
