using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    void Awake()
    {
        GameObject[] soundSources = GameObject.FindGameObjectsWithTag("buttonSound");

        if (soundSources.Length > 1) Destroy(this.gameObject);
        
        DontDestroyOnLoad(this.gameObject);
    }
}
