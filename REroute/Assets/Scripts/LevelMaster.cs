using FishNet.Managing;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMaster : MonoBehaviour
{
    public Transform[] spawnPoints;
    public AudioSource musicSource;

    void Start()
    {
        NetworkManager networkManager = DDOL.GetNetworkManager();
        if (!networkManager.IsServerStarted) { return; }

        Debug.Log("Loaded level '" + SceneManager.GetActiveScene().name + "'");

        //DDOL.GetSceneLoader().TeleportPlayersToSpawnPoints(this);
        DDOL.GetSceneLoader().LevelStart(this);

        musicSource.volume = 0;
        musicSource.Play();
        musicSource.time = 8f;
        StartCoroutine(CountdownMusic());
    }


    IEnumerator CountdownMusic()
    {
        float duration = 3f;
        
        float timer = 0;
        while (timer < duration)
        {
            musicSource.volume = timer / duration;
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        musicSource.volume = 1f;

        PointToGoal ptg = UI.Find("DirectionGuide/DirectionArrow").GetComponent<PointToGoal>();
        ptg.beacon = GameObject.Find("FinishLine").transform;
        ptg.gameObject.SetActive(true);
    }
}
