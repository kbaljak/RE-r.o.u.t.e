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
    
    [Header("Countdown & Timer UI")]
    [SerializeField] private GameObject countdownUI;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI timerText;

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

        if (scoreText == null)
        {
            GameObject scoreObject = GameObject.Find("PlayerUI/Canvas/ScorePanel/PlayerScore");
            if (scoreObject != null) { scoreText = scoreObject.GetComponent<TextMeshProUGUI>(); }
            else { Debug.LogWarning("Score UI Text not found. Assign it manually or update the path."); }
        }

        GameObject speedSliderObj = GameObject.Find("PlayerUI/Canvas/SpeedUI/Slider");
        if (speedSliderObj != null) { speedSlider = speedSliderObj.GetComponent<Slider>(); }
        else { Debug.LogWarning("Speed UI Slider not found. Assign it manually or update the path."); }

        GameObject speedTextObj = GameObject.Find("PlayerUI/Canvas/SpeedUI/SpeedText");
        if (speedTextObj != null) { speedText = speedTextObj.GetComponent<TextMeshProUGUI>(); }
        else { Debug.LogWarning("Speed UI Text not found. Assign it manually or update the path."); }
        UpdateScoreUI();
    }

    private void Start()
    {
        Invoke(nameof(SetName), 0.1f);
    }

    void Update()
    {
        UpdateSpeed();
    }
    void UpdateSpeed()
    {
        if (speedSlider == null) {return;}
        if (speedText == null) {return;}
        float curSpeed = plCont.moveSpeed;
        speedSlider.value = curSpeed;
        speedText.text = (curSpeed).ToString("#.#");
    }
    private void SetName()
    {
        string name = plCont.GetPlayerName();
        
        if (string.IsNullOrEmpty(name))
        {
            Invoke(nameof(SetName), 0.1f);
            return;
        }
        
        playerName.text = name;
        Debug.Log($"Set nametag to: {name}");
    }

    public void OnVaultPerformedScore()
    {
        if (!IsOwner) return;
        RequestServerAddVaultScore();
    }
    public void OnCombatRollPerformedScore()
    {
        if (!IsOwner) return;
        RequestServerAddCombatRollScore();
    }

    public void OnBananaThrownScore()
    {
        Debug.LogWarning("Called!");
        if (!IsOwner) return;
        RequestServerAddBananaThrownScore();
    }

    public void OnOilAppliedScore()
    {
        if (!IsOwner) { return; }
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
        if (!IsOwner) return;
        
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
    
    [ServerRpc]
    private void ServerNotifyFinishLine()
    {
        if (RaceTimeManager.Instance != null)
        {
            RaceTimeManager.Instance.ServerPlayerFinished(Owner);
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
    private void AddScore(int points, string action)
    {
        if (!IsServerStarted) return;

        currentScore += points;
        Debug.Log($"[Server] Player {OwnerId} earned {points} points for {action}. Total: {currentScore}");

        TargetUpdateScore(Owner, currentScore, points);
    }
    private void UpdateScoreUI()
    {
        if (!IsOwner) return;

        if (scoreText != null)
        {
            scoreText.text = $"{displayedScore}";
        }
    }
}