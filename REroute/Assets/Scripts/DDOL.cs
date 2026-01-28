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
        if (Instance != null) { Destroy(gameObject); }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Returns the current DDOL or creates one if not yet created.
    /// </summary>
    public static DDOL GetDDOL()
    {
        // Not yet made.
        if (Instance == null)
        {
            GameObject obj = new();
            obj.name = "DontDestroyOnLoad";
            DDOL ddol = obj.AddComponent<DDOL>();
            DontDestroyOnLoad(ddol);
            Instance = ddol;
            return ddol;
        }
        // Already  made.
        else
        {
            return Instance;
        }
    }


    //// Additions
    public static NetworkManager GetNetworkManager() => Instance.transform.Find("NetworkManager").GetComponent<NetworkManager>();
    public static LoadScenes GetSceneLoader() => Instance.transform.Find("NetworkObjects").GetComponent<LoadScenes>();
}