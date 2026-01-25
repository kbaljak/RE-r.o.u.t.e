using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI startGamePrompt;
    private InputAction buttonPressAction;

    private bool canBePressed = false;

    private void Start()
    {
        buttonPressAction = InputSystem.actions.FindAction("UseItem");
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
        }
    }
}
