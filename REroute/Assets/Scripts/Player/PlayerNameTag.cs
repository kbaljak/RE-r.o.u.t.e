using TMPro;
using UnityEngine;
using UnityEngine.Timeline;

public class PlayerNameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private RectTransform playerNameTagCanvas;
    private PlayerController plCont;

    private void Start()
    {
        plCont = GetComponent<PlayerController>();
        
        if (plCont.IsOwner) { return; }
        
        Invoke(nameof(SetName), 0.1f);
    }

    private void SetName()
    {
        string name = plCont.GetPlayerName();
        
        if (string.IsNullOrEmpty(name))
        {
            Invoke(nameof(SetName), 0.1f);
            return;
        }
        
        playerName.text = name;
        Debug.Log($"Set nametag to: {name}");
    }
}