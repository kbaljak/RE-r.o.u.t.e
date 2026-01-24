using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerScoreController : NetworkBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int vaultPoints = 10;
    [SerializeField] private int combatRollPoints = 5;
    [SerializeField] private int oilApplicationPoints = 2;
    [SerializeField] private int bananThrownPoints = 1;

    private TextMeshProUGUI scoreText;
    private int currentScore = 0;
    private int displayedScore = 0;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return;

        if (scoreText == null)
        {
            GameObject scoreObject = GameObject.Find("PlayerUI/Canvas/ScorePanel/PlayerScore");
            if (scoreObject != null) { scoreText = scoreObject.GetComponent<TextMeshProUGUI>(); }
            else { Debug.LogWarning("Score UI Text not found. Assign it manually or update the path."); }
        }
        UpdateScoreUI();
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
        
        if (RaceTimeManager.Instance != null)
        {
            RaceTimeManager.Instance.OnPlayerFinishRace();
        }
        else
        {
            Debug.LogError("RaceScoreManager not found!");
        }
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