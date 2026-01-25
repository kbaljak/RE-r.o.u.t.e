using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;

public class RaceTimeManager : MonoBehaviour
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

    private NetworkManager _networkManager;

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

        _networkManager = GetComponent<NetworkManager>();
        if (_networkManager == null) { Debug.LogError("Could not find Network Manager");}
    }

/// <summary>
    /// Starts the race - call this when all players are ready (Server only)
    /// </summary>
    public void StartRace()
    {
        if (!IsServer()) return;
        if (raceStarted) { Debug.LogWarning("Race already started!"); return; }

        raceStarted = true;
        raceStartTime = Time.time;
        bestFinishTime = float.MaxValue;
        finishedPlayerCount = 0;
        playerRaceData.Clear();
        finalScores.Clear();

        Debug.Log($"[Server] Race started at {raceStartTime}");
        
        // Notify all clients that race has started
        BroadcastRaceStarted();
    }

    /// <summary>
    /// Registers a player for the race (Server only)
    /// Call this when a player spawns or joins before race starts
    /// </summary>
    public void RegisterPlayer(NetworkConnection connection, string playerName)
    {
        if (!IsServer()) return;

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
    /// Server receives notification that a player finished
    /// Called via PlayerScoreController's ServerRPC
    /// </summary>
    public void ServerPlayerFinished(NetworkConnection connection)
    {
        if (!IsServer()) return;
        if (!raceStarted) return;
        if (!playerRaceData.ContainsKey(connection)) 
        {
            Debug.LogError($"[Server] Unknown player tried to finish race!");
            return;
        }

        PlayerRaceData data = playerRaceData[connection];
        
        if (data.hasFinished)
        {
            Debug.LogWarning($"[Server] Player {data.playerName} already finished!");
            return;
        }

        // Record finish time
        float currentTime = Time.time;
        float raceTime = currentTime - raceStartTime;
        data.finishTime = raceTime;
        data.hasFinished = true;
        finishedPlayerCount++;
        data.finishPosition = finishedPlayerCount;

        // Update best time if this is the first finisher or faster
        if (raceTime < bestFinishTime)
        {
            bestFinishTime = raceTime;
            Debug.Log($"[Server] New best time: {bestFinishTime:F2}s by {data.playerName}");
        }

        Debug.Log($"[Server] Player {data.playerName} finished in position {data.finishPosition} with time {raceTime:F2}s");

        // Get action score from PlayerScoreController
        if (connection.FirstObject != null)
        {
            PlayerScoreController scoreController = connection.FirstObject.GetComponent<PlayerScoreController>();
            if (scoreController != null)
            {
                data.actionScore = scoreController.GetTotalScore();
            }
        }

        // Notify this player that they finished
        NotifyPlayerFinished(connection, data.finishPosition, raceTime);

        // Check if all players finished
        if (finishedPlayerCount >= playerRaceData.Count)
        {
            EndRace();
        }
    }


    private void BroadcastRaceStarted()
    {
        if (!IsServer()) return;

        // You can implement client notification here if needed
        // For now, clients will know race started when they can earn points
        Debug.Log("[Server] Broadcasting race start to all clients");
    }

    private void NotifyPlayerFinished(NetworkConnection connection, int position, float time)
    {
        if (!IsServer()) return;

        // Find the player's PlayerScoreController and notify them
        if (connection.FirstObject != null)
        {
            PlayerScoreController scoreController = connection.FirstObject.GetComponent<PlayerScoreController>();
            if (scoreController != null)
            {
                scoreController.TargetNotifyFinished(connection, position, time);
            }
        }
    }

    private void BroadcastFinalScores(PlayerFinalScore[] scores)
    {
        if (!IsServer()) return;

        // Send to all connected clients
        foreach (NetworkConnection conn in _networkManager.ServerManager.Clients.Values)
        {
            if (conn.FirstObject != null)
            {
                PlayerScoreController scoreController = conn.FirstObject.GetComponent<PlayerScoreController>();
                if (scoreController != null)
                {
                    scoreController.TargetShowFinalScores(conn, scores);
                }
            }
        }
    }

    /// <summary>
    /// Called when all players have finished - calculates final scores (Server only)
    /// </summary>
    private void EndRace()
    {
        if (!IsServer()) return;

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

            // Final score = action score - time penalty
            data.finalScore = Mathf.Max(0, data.actionScore - timePenalty); // Don't go below 0

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

        // Sort by final score (highest first), then by position if tied
        finalScores = finalScores.OrderByDescending(s => s.finalScore)
                                 .ThenBy(s => s.finishPosition)
                                 .ToList();

        // Log final results
        Debug.Log("=== FINAL SCORES (Server) ===");
        Debug.Log($"Best Time: {bestFinishTime:F2}s");
        foreach (var score in finalScores)
        {
            Debug.Log($"{score.finishPosition}. {score.playerName}: {score.finalScore} pts " +
                     $"(Time: {score.raceTime:F2}s, Actions: {score.actionScore}, Penalty: -{score.timePenalty})");
        }

        // Send results to all clients
        BroadcastFinalScores(finalScores.ToArray());
    }

    /// <summary>
    /// Gets the final scores after race completion
    /// </summary>
    public List<PlayerFinalScore> GetFinalScores()
    {
        return new List<PlayerFinalScore>(finalScores);
    }

    /// <summary>
    /// Checks if the race is currently active
    /// </summary>
    public bool IsRaceActive()
    {
        return raceStarted;
    }

    /// <summary>
    /// Gets the best finish time
    /// </summary>
    public float GetBestTime()
    {
        return bestFinishTime;
    }

    /// <summary>
    /// Helper to check if this is the server
    /// </summary>
    private bool IsServer()
    {
        return _networkManager != null && _networkManager.IsServerStarted;
    }
}
