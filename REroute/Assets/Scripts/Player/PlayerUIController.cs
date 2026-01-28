using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController: NetworkBehaviour
{
    //private static PlayerUIController Instance;

    [Header("Score Settings")]
    [SerializeField] private int vaultPoints = 10;
    [SerializeField] private int combatRollPoints = 5;
    [SerializeField] private int oilApplicationPoints = 2;
    [SerializeField] private int bananThrownPoints = 1;
    
    [Space(20)]
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private RectTransform playerNameTagCanvas;
    
    public GameObject countDownUI;
    public TextMeshProUGUI countDownTimeText;
    public GameObject timerUI;
    public TextMeshProUGUI timerText;

    private PlayerController plCont;

    private TextMeshProUGUI scoreText;
    private int currentScore = 0;
    private int displayedScore = 0;
    private Slider speedSlider;
    private TextMeshProUGUI speedText;

    private bool isTimerRunning = false;
    private float elapsedTime = 0f;

    public override void OnStartClient()
    {
        base.OnStartClient();

        plCont = GetComponent<PlayerController>();

        if (!plCont.IsOwner) return;

        Transform scoreTransform = UI.Find("PlayerUI/ScorePanel/PlayerScore");
        if (scoreTransform != null) { scoreText = scoreTransform.GetComponent<TextMeshProUGUI>(); }
        else { Debug.LogWarning("Score UI Text not found. Assign it manually or update the path."); }

        Transform speedSliderT = UI.Find("PlayerUI/SpeedUI/Slider");
        if (speedSliderT != null) { speedSlider = speedSliderT.GetComponent<Slider>(); }
        else { Debug.LogWarning("Speed UI Slider not found. Assign it manually or update the path."); }

        Transform speedTextT = UI.Find("PlayerUI/SpeedUI/SpeedText");
        if (speedTextT != null) { speedText = speedTextT.GetComponent<TextMeshProUGUI>(); }
        else { Debug.LogWarning("Speed UI Text not found. Assign it manually or update the path."); }

        countDownUI = UI.Find("PlayerUI/CountDown").gameObject;
        if (countDownUI == null) { Debug.LogWarning("CountDown not found. Assign it manually or update the path."); }

        timerUI = UI.Find("PlayerUI/Timer").gameObject;
        if (timerUI == null) { Debug.LogWarning("Timer not found. Assign it manually or update the path."); }

        countDownTimeText = UI.Find("PlayerUI/CountDown/CountDownTimerText").GetComponent<TextMeshProUGUI>();
        if (countDownTimeText == null) { Debug.LogWarning("CountDownTimerText not found. Assign it manually or update the path."); }

        timerText = UI.Find("PlayerUI/Timer/TimerText").GetComponent<TextMeshProUGUI>();
        if (timerText == null) { Debug.LogWarning("TimerText not found. Assign it manually or update the path."); }

        UpdateScoreUI();
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.name == "FinishLine")
        {
            Debug.Log("Finished!");
            OnPlayerCrossedFinishLine();
        }
    }
    /*public void SetPlayer(PlayerController plCont)
    {
        if (!plCont.IsOwner) { return; }

        this.plCont = plCont;
    }*/

    void Update()
    {
        if (plCont) 
        { 
            UpdateSpeed(); UpdateTimer(); 
        }
    }
    void UpdateSpeed()
    {
        if (speedSlider == null) {return;}
        if (speedText == null) {return;}
        float curSpeed = plCont.moveSpeed;
        speedSlider.value = curSpeed;
        speedText.text = (curSpeed).ToString("#.#");
    }
    void UpdateTimer()
    {
        if (!isTimerRunning) return;
        if (timerText == null) return;
        
        elapsedTime += Time.deltaTime;
        
        int hours = Mathf.FloorToInt(elapsedTime / 3600f);
        int minutes = Mathf.FloorToInt((elapsedTime % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }
    /*private void SetName()
    {
        string name = plCont.GetPlayerName();
        
        if (string.IsNullOrEmpty(name))
        {
            Invoke(nameof(SetName), 0.1f);
            return;
        }
        
        playerName.text = name;
        Debug.Log($"Set nametag to: {name}");
    }*/

    public void OnVaultPerformedScore()
    {
        Debug.Log("OnVaultPerformedScore() IsOwner: " + IsOwner);
        if (!plCont.IsOwner) return;
        RequestServerAddVaultScore();
    }
    public void OnCombatRollPerformedScore()
    {
        if (!plCont.IsOwner) return;
        RequestServerAddCombatRollScore();
    }

    public void OnBananaThrownScore()
    {
        Debug.LogWarning("Called!");
        if (!plCont.IsOwner) return;
        RequestServerAddBananaThrownScore();
    }

    public void OnOilAppliedScore()
    {
        if (!plCont.IsOwner) { return; }
        RequestServerAddOilApplicationScore();
    }

    public int GetTotalScore()
    {
        if (!IsServerStarted)
        {
            Debug.LogWarning("GetTotalScore should only be called on server!");
            return 0;
        }
        return currentScore;
    }

    public void OnPlayerCrossedFinishLine()
    {
        if (!plCont.IsOwner) return;
        
        ServerNotifyFinishLine();
    }

    public int GetDisplayedScore()
    {
        return displayedScore;
    }

    [ServerRpc]
    private void RequestServerAddVaultScore()
    {
        AddScore(vaultPoints, "Vault");
    }

    [ServerRpc]
    private void RequestServerAddCombatRollScore()
    {
        AddScore(combatRollPoints, "Combat Roll");
    }

    [ServerRpc]
    private void RequestServerAddBananaThrownScore()
    {
        Debug.LogWarning("RequestServerAddBananaThrownScore()");
        AddScore(bananThrownPoints, "Banana Thrown");
    }

    [ServerRpc]
    private void RequestServerAddOilApplicationScore()
    {
        AddScore(oilApplicationPoints, "Oil Applied");
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ServerNotifyFinishLine(NetworkConnection conn = null)
    {
        if (RaceTimeManager.Instance != null)
        {
            Debug.LogWarning("Network conn: " + conn);
            RaceTimeManager.Instance.ServerPlayerFinished(conn);
        }
        else
        {
            Debug.LogError("[Server] RaceScoreManager not found!");
        }
    }

    [TargetRpc]
    private void TargetUpdateScore(NetworkConnection connection, int totalScore, int pointsAdded)
    {
        displayedScore = totalScore;
        UpdateScoreUI();

        if (pointsAdded > 0)
        {
            Debug.Log($"[Client] +{pointsAdded} points! Total: {totalScore}");
        }
    }

    [TargetRpc]
    public void TargetNotifyFinished(NetworkConnection connection, int position, float time)
    {
        Debug.Log($"[Client] You finished in position {position} with time {time:F2}s!");
        //TODO:
        // Maybe some UI to show you finished, some text or something, idk
    }

    [TargetRpc]
    public void TargetShowFinalScores(NetworkConnection connection, PlayerFinalScore[] scores)
    {
        Debug.Log("=== FINAL RACE RESULTS ===");
        foreach (var score in scores)
        {
            Debug.Log($"Position {score.finishPosition}: {score.playerName}");
            Debug.Log($"  Time: {score.raceTime:F2}s | Action Score: {score.actionScore} | Time Penalty: -{score.timePenalty} | Final: {score.finalScore}");
        }
        
        //TODO:
        // Maybe trigger a UI to show each player their score
    }

    [ObserversRpc]
    public void ObserversUpdateCountdown(int countdownNumber)
    {
        if (!plCont.IsOwner) return;
        
        countDownUI.SetActive(true);
        
        countDownTimeText.text = countdownNumber.ToString();

        Debug.Log($"[Client] Countdown: {countdownNumber}");
    }

    [ObserversRpc]
    public void ObserversStartRaceTimer()
    {
        if (!plCont.IsOwner) return;
        
        countDownUI.SetActive(false);
        timerUI.SetActive(true);
        // Start the timer
        isTimerRunning = true;
        elapsedTime = 0f;
        
        Debug.Log("[Client] Race started! Timer running.");
    }

    [ObserversRpc]
    public void ObserversStopRaceTimer()
    {
        if (!plCont.IsOwner) return;
        
        isTimerRunning = false;
        
        Debug.Log($"[Client] Race ended! Final time: {timerText.text}");
    }

    private void AddScore(int points, string action)
    {
        if (!IsServerStarted) return;

        currentScore += points;
        Debug.Log($"[Server] Player {OwnerId} earned {points} points for {action}. Total: {currentScore}");

        TargetUpdateScore(Owner, currentScore, points);
    }
    private void UpdateScoreUI()
    {
        if (!plCont.IsOwner) return;

        if (scoreText != null)
        {
            scoreText.text = $"{displayedScore}";
        }
    }
}