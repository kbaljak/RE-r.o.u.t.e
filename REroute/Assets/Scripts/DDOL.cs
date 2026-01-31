using FishNet.Managing;
using System;
using UnityEngine;

public class DDOL : MonoBehaviour
{
    #region Public.
    /// <summary>
    /// Created instance of DDOL.
    /// </summary>
    private static DDOL Instance;
    #endregion


    private void Awake()
    {
        Debug.Log("Instance: " + Instance);
        if (Instance != null) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Returns the current DDOL or creates one if not yet created.
    /// </summary>
    public static DDOL GetDDOL() => Instance;


    //// Additions
    public static Transform Find(string name) => Instance.transform.Find(name);
    public static NetworkManager GetNetworkManager() { Debug.Log("Instance for NetworkManager: " + Instance); return Instance.transform.Find("NetworkManager").GetComponent<NetworkManager>(); }
    public static LoadScenes GetSceneLoader() => Instance.transform.Find("NetworkObjects").GetComponent<LoadScenes>();
}