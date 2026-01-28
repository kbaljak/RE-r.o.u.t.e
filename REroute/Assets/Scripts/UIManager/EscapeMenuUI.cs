using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EscapeMenuUI : MonoBehaviour
{
    public static EscapeMenuUI Instance { get; private set; }

    [SerializeField] Button settingsBtn;
    [SerializeField] Button quitBtn;
    private InputAction openEscapeMenu;
    private bool escapeMenuToggled = false;
    private bool visibleCursor = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        openEscapeMenu = InputSystem.actions.FindAction("OpenEscapeMenu");
        if (openEscapeMenu == null) { Debug.LogError("Could not find OpenEscapeMenu!");}

        if (settingsBtn == null) { Debug.LogError("No settings button, did you assign it in the inspector?"); }
        else { settingsBtn.onClick.AddListener(OnSettingsButtonPressed); }

        if (quitBtn == null) { Debug.LogError("No quit button, did you assign it in the inspector?"); }
        else { quitBtn.onClick.AddListener(OnQuitButtonPressed); }
    }
    void Update()
    {
        if (openEscapeMenu.WasPressedThisFrame())
        {
            //Debug.Log("Esc pressed! Open Menu");
            escapeMenuToggled = !escapeMenuToggled;
            visibleCursor = !visibleCursor;

            gameObject.SetActive(escapeMenuToggled);
            Cursor.lockState = escapeMenuToggled ? CursorLockMode.None: CursorLockMode.Locked;
            Cursor.visible = visibleCursor;

        }
    }
    private void OnSettingsButtonPressed()
    {
        Debug.Log("Settings button pressed!");
    }
    private void OnQuitButtonPressed()
    {
        //Debug.Log("Quit button pressed!");
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}
