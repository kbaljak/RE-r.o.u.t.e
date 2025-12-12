using FishNet.Transporting.Tugboat;
using Unity.Multiplayer.Playmode;
using UnityEngine;

public class ConnectionStarter : MonoBehaviour
{
    private Tugboat _tugboat;
    void Start()
    {
        if (TryGetComponent(out Tugboat _t)) _tugboat = _t;

        else
        {
            Debug.Log("Couldn't get Tugboat!", this);
            return;
        }

        if (CurrentPlayer.IsMainEditor)
        {
            _tugboat.StartConnection(true);
            _tugboat.StartConnection(false);
        }
        else
        {
            _tugboat.StartConnection(false);
        }
    }
}
