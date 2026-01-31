using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSpeed : NetworkBehaviour
{
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedText;
    private PlayerController playerCont;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner) return;

        playerCont = GetComponent<PlayerController>();
        if (playerCont == null) { Debug.LogError("Could not find PlayerController!"); }
    }
    private void Start()
    {
        speedSlider = GameObject.Find("PlayerUI/Canvas/SpeedUI/Slider").GetComponent<Slider>();
        speedText = GameObject.Find("PlayerUI/Canvas/SpeedUI/SpeedText").GetComponent<TextMeshProUGUI>();
        speedSlider.maxValue = playerCont.runMaxSpeed;
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
