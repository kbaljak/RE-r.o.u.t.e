using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
    private bool isJoinButtonEnabled = false;
    public int authPort = 42568;
    private string gameIdentifier = "REroute";
    
    void Start()
    {
        if (codeInputDialog == null) { Debug.LogError("Code Input Dialog can't be empty, did you forget to assign it in the inspector?"); }

        if (codeInputField == null) { Debug.LogError("Code input field can't be empty, did you forget to assign it in the inspector?"); }

        if (joinButton != null) { joinButton.GetComponent<Button>().onClick.AddListener(OnJoinButtonClicked); }
        else { Debug.LogError("Could not find Join button, did you assign it in the inspector?"); }

        if (backButton != null) { backButton.GetComponent<Button>().onClick.AddListener(OnBackButtonClicked); }
        else { Debug.LogError("Could not find Back button, did you assign it in the inspector?"); }
    }

    void Update()
    {
        if (codeInputField.text.Length == 6 && !isJoinButtonEnabled)
        {
            isJoinButtonEnabled = !isJoinButtonEnabled;
            joinButton.SetActive(isJoinButtonEnabled);
        }
        else if (codeInputField.text.Length < 6 && isJoinButtonEnabled)
        {
            isJoinButtonEnabled = !isJoinButtonEnabled;
            joinButton.SetActive(isJoinButtonEnabled);
        }
    }

    private void OnJoinButtonClicked()
    {
        string joinCode = codeInputField.text.ToUpper();
        Debug.Log("Inputed code is: " + joinCode);
        codeInputField.text = "";

        serverInfo = ServerEntryUI.Instance.getDiscoveredServerInfo();

        //TODO:
        //send code to server to see if player inputed the correct code
        bool isCodeCorrect = AuthorizeJoinWithServer(serverInfo.hostAddress, joinCode);

        if (isCodeCorrect)
        {    
            if (serverInfo != null) { NetworkLobbyManager.Instance.JoinGame(serverInfo.hostAddress); }
            else {Debug.LogError("Server info is empty, can't join server!");}
        }
        else
        {
            //TODO:
            //Maybe disaply red text "WRONG CODE"
            Debug.LogWarning("Incorrect code!");
        }
    }

    private bool AuthorizeJoinWithServer(string serverAddress, string code)
    {
        IPEndPoint dst = new IPEndPoint(IPAddress.Parse(serverAddress), authPort);
        UdpClient udpClient = new UdpClient();

        string message = $"{gameIdentifier}|Code|{code}";       //example ReRoute|Code|CBVR5A
        byte[] msg_data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(msg_data, msg_data.Length, dst);

        try
        {
            byte[] response_data = udpClient.Receive(ref dst);
            string response_msg = Encoding.UTF8.GetString(response_data);

            if (response_msg.StartsWith($"{gameIdentifier}|Correct"))
            {
                string[] msgParts = response_msg.Split('|');
                if (msgParts.Length == 3)
                {
                    if (msgParts[2] == "True") { return true; }
                    else { return false; }
                }
                else { throw new Exception($"Expected length of 5 but got message of length: {msgParts.Length}"); }
            }
            return false;
        }
        catch (SocketException e) { Debug.LogError("Socket excpetion!"); return false; }
        catch (Exception e) { Debug.LogError($"Error receiving broadcast: {e.Message}"); return false; }
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
