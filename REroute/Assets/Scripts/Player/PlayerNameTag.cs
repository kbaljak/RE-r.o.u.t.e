using TMPro;
using UnityEngine;

public class PlayerNameTag : MonoBehaviour
{
    [SerializeField] private PlayerController playerCont;
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private RectTransform playerNameTagCanvas;

    private void Start()
    {
        if (playerCont.IsOwner) { return; }
        
        Invoke(nameof(SetName), 0.1f);
    }

    private void SetName()
    {
        string name = playerCont.GetPlayerName();
        
        if (string.IsNullOrEmpty(name))
        {
            Invoke(nameof(SetName), 0.1f);
            return;
        }
        
        playerName.text = name;
        Debug.Log($"Set nametag to: {name}");
    }
}