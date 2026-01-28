using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI Instance { get; private set; }

    private PlayerController playerCont;

    [SerializeField] private Slider speedSlider;
    [SerializeField] public TextMeshProUGUI speedText;

    [SerializeField] public GameObject throwChargeMeter;
    [SerializeField] public GameObject applyOilPrompt;


    void Awake() 
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        //GetComponent<Canvas>().enabled = false; enabled = false;
    }
    public static void InitializePlayerController(PlayerController target)
    { 
        if (Instance != null) { Instance.InitializePlayerController_Local(target); }
        else { Debug.LogWarning("No existing UI GameObject or Instance set too late."); }
    }
    void InitializePlayerController_Local(PlayerController target)
    {
        playerCont = target;
        speedSlider.maxValue = playerCont.runMaxSpeed;

        // Pass on to PlayerUI
        //playerUI.SetPlayer(target);

        transform.GetChild(0).GetComponent<Canvas>().enabled = true; enabled = true;
    }


    void Update()
    {
        UpdateSpeed();
    }



    void UpdateSpeed()
    {
        if (playerCont)
        {
            float curSpeed = playerCont.moveSpeed;
            speedSlider.value = curSpeed;
            speedText.text = (curSpeed).ToString("#.#");
        }
        else { speedSlider.value = 0;  speedText.text = ""; }
    }

    public static void EnableCountdown(bool value)
    {
        Find("PlayerUI/CountDown").gameObject.SetActive(value);
    }

    public static Transform Find(string path) => Instance.transform.Find("Canvas/" + path);
}
