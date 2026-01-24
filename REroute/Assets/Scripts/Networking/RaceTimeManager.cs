using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class RaceTimeManager : NetworkBehaviour
{
    public static RaceTimeManager Instance { get; private set; }

    [Header("Score Penalty Settings")]
    [SerializeField] private float pointsLostPerSecond = 1f; 
    [SerializeField] private bool useTimePenalty = true;

    [Header("Race State")]
    private bool raceStarted = false;
    private float raceStartTime = 0f;
    private float bestFinishTime = float.MaxValue;
    private int finishedPlayerCount = 0;

    private Dictionary<NetworkConnection, PlayerRaceData> playerRaceData = new Dictionary<NetworkConnection, PlayerRaceData>();

    private List<PlayerFinalScore> finalScores = new List<PlayerFinalScore>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Starts the race - call this when all players are ready (Server only)
    /// </summary>
    public void StartRace()
    {
        if (!IsServerStarted) return;
        if (raceStarted) { Debug.LogWarning("Race already started!"); return; }

        raceStarted = true;
        raceStartTime = Time.time;
        bestFinishTime = float.MaxValue;
        finishedPlayerCount = 0;
        playerRaceData.Clear();
        finalScores.Clear();

        Debug.Log($"[Server] Race started at {raceStartTime}");
    }

    /// <summary>
    /// Registers a player for the race (Server only)
    /// Call this when a player spawns or joins before race starts
    /// </summary>
    public void RegisterPlayer(NetworkConnection connection, string playerName)
    {
        if (!IsServerStarted) return;

        if (!playerRaceData.ContainsKey(connection))
        {
            playerRaceData[connection] = new PlayerRaceData
            {
                connection = connection,
                playerName = playerName,
                startTime = raceStartTime,
                finishTime = 0f,
                actionScore = 0,
                finalScore = 0,
                finishPosition = 0,
                hasFinished = false
            };

            Debug.Log($"[Server] Registered player {playerName} for race");
        }
    }

    /// <summary>
    /// Player calls this when they cross the finish line
    /// </summary>
    public void OnPlayerFinishRace()
    {
        if (!IsOwner) return;
        if (!raceStarted) { Debug.LogWarning("Race hasn't started yet!"); return; }

        // Request server to record finish time
        ServerPlayerFinishedRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerPlayerFinishedRPC(NetworkConnection sender = null)
    {
        if (!raceStarted) return;
        if (!playerRaceData.ContainsKey(sender)) 
        {
            Debug.LogError($"[Server] Unknown player tried to finish race!");
            return;
        }

        PlayerRaceData data = playerRaceData[sender];
        
        if (data.hasFinished)
        {
            Debug.LogWarning($"[Server] Player {data.playerName} already finished!");
            return;
        }

        float currentTime = Time.time;
        float raceTime = currentTime - raceStartTime;
        data.finishTime = raceTime;
        data.hasFinished = true;
        finishedPlayerCount++;
        data.finishPosition = finishedPlayerCount;

        if (raceTime < bestFinishTime)
        {
            bestFinishTime = raceTime;
            Debug.Log($"[Server] New best time: {bestFinishTime:F2}s by {data.playerName}");
        }

        Debug.Log($"[Server] Player {data.playerName} finished in position {data.finishPosition} with time {raceTime:F2}s");

        PlayerScoreController scoreController = sender.FirstObject?.GetComponent<PlayerScoreController>();
        if (scoreController != null)
        {
            data.actionScore = scoreController.GetTotalScore();
        }

        TargetNotifyPlayerFinished(sender, data.finishPosition, raceTime);

        if (finishedPlayerCount >= playerRaceData.Count)
        {
            EndRace();
        }
    }

    [TargetRpc]
    private void TargetNotifyPlayerFinished(NetworkConnection connection, int position, float time)
    {
        Debug.Log($"[Client] You finished in position {position} with time {time:F2}s!");
        //TODO:
        // Maybe add a UI element notifying player he finihsed the race
    }

    [ObserversRpc]
    private void ObserversShowFinalScores(PlayerFinalScore[] scores)
    {
        Debug.Log("=== FINAL RACE RESULTS ===");
        foreach (var score in scores)
        {
            Debug.Log($"Position {score.finishPosition}: {score.playerName}");
            Debug.Log($"  Time: {score.raceTime:F2}s | Action Score: {score.actionScore} | Time Penalty: -{score.timePenalty} | Final: {score.finalScore}");
        }
        
        //TODO:
        // Notify each player to activate their UI element showing them their score
    }

    private void EndRace()
    {
        if (!IsServerStarted) return;

        Debug.Log("[Server] All players finished! Calculating final scores...");

        raceStarted = false;
        finalScores.Clear();

        foreach (var kvp in playerRaceData)
        {
            PlayerRaceData data = kvp.Value;
            
            if (!data.hasFinished) continue;

            // Calculate time penalty (seconds behind leader * penalty per second)
            float timeDelta = data.finishTime - bestFinishTime;
            int timePenalty = useTimePenalty ? Mathf.RoundToInt(timeDelta * pointsLostPerSecond) : 0;

            data.finalScore = Mathf.Max(0, data.actionScore - timePenalty);

            PlayerFinalScore finalScore = new PlayerFinalScore(
                data.playerName,
                data.finishPosition,
                data.finishTime,
                data.actionScore,
                timePenalty,
                data.finalScore
            );

            finalScores.Add(finalScore);
        }

        finalScores = finalScores.OrderByDescending(s => s.finalScore)
                                 .ThenBy(s => s.finishPosition)
                                 .ToList();

        Debug.Log("=== FINAL SCORES (Server) ===");
        Debug.Log($"Best Time: {bestFinishTime:F2}s");
        foreach (var score in finalScores)
        {
            Debug.Log($"{score.finishPosition}. {score.playerName}: {score.finalScore} pts " +
                     $"(Time: {score.raceTime:F2}s, Actions: {score.actionScore}, Penalty: -{score.timePenalty})");
        }

        ObserversShowFinalScores(finalScores.ToArray());
    }

    public List<PlayerFinalScore> GetFinalScores()
    {
        return new List<PlayerFinalScore>(finalScores);
    }
    public bool IsRaceActive()
    {
        return raceStarted;
    }
    public float GetBestTime()
    {
        return bestFinishTime;
    }
}
