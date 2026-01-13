using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeInputDialogController : MonoBehaviour
{
    public static CodeInputDialogController Instance { get; private set; }

    [SerializeField] GameObject codeInputDialog;
    [SerializeField] TMP_InputField codeInputField;
    [SerializeField] GameObject joinButton;
    [SerializeField] GameObject backButton;

    private DiscoveredServer serverInfo;
    
    void Start()
    {
        if (codeInputDialog == null) { Debug.LogError("Code Input Dialog can't be empty, did you forget to assign it in the inspector?"); }

        if (codeInputField == null) { Debug.LogError("Code input field can't be empty, did you forget to assign it in the inspector?"); }

        if (joinButton != null) { joinButton.GetComponent<Button>().onClick.AddListener(OnJoinButtonClicked); }
        else { Debug.LogError("Could not find Join button, did you assign it in the inspector?"); }

        if (backButton != null) { backButton.GetComponent<Button>().onClick.AddListener(OnBackButtonClicked); }
        else { Debug.LogError("Could not find Back button, did you assign it in the inspector?"); }
    }

    private void OnJoinButtonClicked()
    {
        string code = string.IsNullOrEmpty(codeInputField.text) ? "" : codeInputField.text;
        Debug.Log("Inputed code is: " + code);

        if (code != "")
        {
            //TODO:
            //send code to server to see if player inputed the correct code

            serverInfo = ServerEntryUI.Instance.getDiscoveredServerInfo();
            if (serverInfo != null) { NetworkLobbyManager.Instance.JoinGame(serverInfo.hostAddress); }
            else {Debug.LogError("Server info is empty, can't join server!");}    
        }
    }

    private void OnBackButtonClicked()
    {
        codeInputField.text = "";
        ShowCodeInputDialog(false);
    }

    private void OnDestroy()
    {
        joinButton.GetComponent<Button>().onClick.RemoveListener(OnJoinButtonClicked);
        backButton.GetComponent<Button>().onClick.RemoveListener(OnBackButtonClicked);
    }

    public void ShowCodeInputDialog(bool isShown)
    {
        codeInputDialog.SetActive(isShown);
    }
}
