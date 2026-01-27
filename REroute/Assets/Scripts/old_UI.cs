using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    private static UI Instance;

    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedText;

    private PlayerController playerCont;

    void Awake() 
    {
        if (Instance != null) { Debug.LogError("Double UI???"); }
        Instance = this;

        //GetComponent<Canvas>().enabled = false; enabled = false; 
    }
    public static void InitializePlayerController(PlayerController target)
    { 
        if (Instance != null) { Instance.InitializePlayerController_Local(target); }
        else { Debug.LogWarning("No existing UI GameObject or Instance set to late."); }
    }
    void InitializePlayerController_Local(PlayerController target)
    {
        playerCont = target;
        speedSlider.maxValue = playerCont.runMaxSpeed;

        GetComponent<Canvas>().enabled = true; enabled = true;
    }


    void Update()
    {
        UpdateSpeed();
    }



    void UpdateSpeed()
    {
        float curSpeed = playerCont.moveSpeed;
        speedSlider.value = curSpeed;
        speedText.text = (curSpeed).ToString("#.#");
    }
}
