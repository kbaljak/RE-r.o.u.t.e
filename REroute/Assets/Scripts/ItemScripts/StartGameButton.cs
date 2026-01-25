using FishNet.Managing;
using FishNet.Managing.Scened;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI startGamePrompt;
    [SerializeField] public string level_scene = "Map_2";
    private InputAction buttonPressAction;
    private NetworkManager _networkManager;

    private bool canBePressed = false;

    private void Start()
    {
        buttonPressAction = InputSystem.actions.FindAction("UseItem");

        _networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
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
            _networkManager.GetComponent<LoadScenes>().LoadLevelScene(level_scene);
        }
    }
}
