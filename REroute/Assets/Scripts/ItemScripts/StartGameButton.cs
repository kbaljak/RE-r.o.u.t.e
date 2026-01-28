using FishNet.Managing;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class StartGameButton : MonoBehaviour
{
    public static StartGameButton Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI startGamePrompt;
    private InputAction buttonPressAction;
    private NetworkManager _networkManager;

    private bool canBePressed = false;

    private void Start()
    {
        buttonPressAction = InputSystem.actions.FindAction("UseItem");

        _networkManager = DDOL.GetNetworkManager(); //NetworkManager.Instances[0].gameObject;  //GameObject.Find("NetworkManager"); 
        if (_networkManager == null) { Debug.LogError("Could not find Network Manager in scene!"); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            startGamePrompt.gameObject.SetActive(true);
            canBePressed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            startGamePrompt.gameObject.SetActive(false);
            canBePressed = false;
        }
    }

    private void Update()
    {
        if (buttonPressAction.WasPressedThisFrame() && canBePressed)
        {
            Debug.Log("Player pressed the button! Loading new scene!");
            DDOL.GetSceneLoader().LoadLevel("Map_2"); //TeleportPlayersToLevelArea();
        }
    }

    public void DisablePrompt()
    {  
        Debug.LogWarning("Disable Prompt");
        canBePressed = false;
        startGamePrompt.gameObject.SetActive(false);
    }
}
