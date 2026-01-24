using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyBoardManager : MonoBehaviour
{
    [SerializeField] public GameObject lobbyBoardCanvas;
    [SerializeField] public GameObject playerEntryList;
    [SerializeField] public GameObject playerEntryPrefab;
    [SerializeField] public RectTransform codeText;

    private InputAction openLobbyBoard;

    private void Start()
    {
        string genereatedLobbyCode = NetworkLobbyManager.Instance.GetLobbyCode();

        if (codeText != null) {codeText.GetComponent<TextMeshProUGUI>().text = genereatedLobbyCode; }
        else { Debug.LogError("Could not find CodeText UI element! Did you assign it in the editor?"); }

        if (lobbyBoardCanvas == null) { Debug.LogError("Could not find Lobby Board Cavas!"); }

        openLobbyBoard = InputSystem.actions.FindAction("OpenLobbyBoard");
        if (openLobbyBoard == null) { Debug.LogError("Could not find OpenEscapeMenu!");}
    }

    private void Update()
    {
        if (openLobbyBoard.WasPressedThisFrame())
        {
            //Debug.Log("Tab pressed! Open Lobby Board");
            lobbyBoardCanvas.SetActive(true);
        }

        if (openLobbyBoard.WasReleasedThisFrame())
        {
            lobbyBoardCanvas.SetActive(false);
        }
    }
}
