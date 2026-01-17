using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    private static UI Instance;

    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI fpsText;

    private PlayerController playerCont;

    void Awake() 
    {
        if (Instance != null) { Debug.LogError("Double UI???"); }
        Instance = this;

        GetComponent<Canvas>().enabled = false; enabled = false; 
    }
    public static void InitializePlayerController(PlayerController target)
    { Instance.InitializePlayerController_Local(target); }
    void InitializePlayerController_Local(PlayerController target)
    {
        playerCont = target;
        speedSlider.maxValue = playerCont.runMaxSpeed;

        GetComponent<Canvas>().enabled = true; enabled = true;
    }


    void Update()
    {
        fpsText.text = Mathf.RoundToInt(1.0f / Time.deltaTime).ToString();

        UpdateSpeed();
    }



    void UpdateSpeed()
    {
        float curSpeed = playerCont.moveSpeed;
        speedSlider.value = curSpeed;
        speedText.text = (curSpeed).ToString("#.#");
    }
}
