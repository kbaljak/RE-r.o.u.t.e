using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class RaceTimeManager : MonoBehaviour
{
    public static RaceTimeManager Instance { get; private set; }

    [Header("Score Penalty Settings")]
    [SerializeField] private float pointsLostPerSecond = 1f; 
    [SerializeField] private bool useTimePenalty = true;

    [Header("Countdown Settings")]
    [SerializeField] private int countdownFrom = 3;
    [SerializeField] private float delayBetweenNumbers = 1f;

    [Header("Race State")]
    private bool raceStarted = false;
    private float raceStartTime = 0f;
    private float bestFinishTime = float.MaxValue;
    private int finishedPlayerCount = 0;

    private NetworkManager _networkManager;

    private Dictionary<string, PlayerRaceData> playerRaceDataDict = new Dictionary<string, PlayerRaceData>();

    private List<PlayerFinalScore> finalScores = new List<PlayerFinalScore>();

    private List<PlayerUIController> playerUIControllers = new List<PlayerUIController>();

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

        UI.EnableCountdown(false);
    }

    public void StartRaceWithCountdown(PlayerController myPlCont)
    {
        Debug.Log("[Client] Starting race countdown sequence...");
        StartCoroutine(CountdownSequence(myPlCont));
    }

    private IEnumerator CountdownSequence(PlayerController myPlCont)
    {
        LoadScenes sceneLoader = DDOL.GetSceneLoader();
        PlayerUIController myUICont = myPlCont.GetComponent<PlayerUIController>();

        myPlCont.EnablePlayerControl(false); //sceneLoader.FreezeAllPlayers(true);
        UI.EnableCountdown(true);

        /*foreach (var kvp in playerRaceDataDict)
        {
            NetworkConnection conn = kvp.Value.connection;
            PlayerUIController plUICont = conn.FirstObject.gameObject.GetComponent<PlayerUIController>();
            if (plUICont == null) { Debug.LogError($"For connection {conn} could not find PlayerUIController script"); }
            playerUIControllers.Add(plUICont);
        }*/

        for (int i = countdownFrom; i >= 1; i--)
        {
            Debug.Log($"[Server] Countdown: {i}");

            //foreach (PlayerUIController uiController in playerUIControllers) { uiController.ObserversUpdateCountdown(i); }
            myUICont.UpdateCountdown(i);

            yield return new WaitForSeconds(delayBetweenNumbers);
        }
        Debug.Log("[Server] Race starting NOW!");

        UI.EnableCountdown(false);
        StartRace();
        myPlCont.EnablePlayerControl(true); //sceneLoader.FreezeAllPlayers(false);
    }

    public void StartRace()
    {
        if (!IsServer()) return;

        raceStarted = true;
        raceStartTime = Time.time;
        bestFinishTime = float.MaxValue;
        finishedPlayerCount = 0;
        //playerRaceDataDict.Clear();
        finalScores.Clear();

        Debug.LogWarning($"[Server] Race started at {raceStartTime}");

        BroadcastRaceStarted();
    }
    /*public static void FreezeAllPlayers(bool freeze)
    {
        NetworkManager networkManager = DDOL.GetNetworkManager();
        foreach (NetworkConnection conn in networkManager.ServerManager.Clients.Values)
        {
            PlayerController plCont = conn.FirstObject.GetComponent<PlayerController>();
            if (plCont != null)
            {
                plCont.EnablePlayerControl_RPC(!freeze);
                //plCont.EnablePlayerControl(!freeze);
            }
            else { Debug.LogError($"For connection {conn} could not find PlayerController script"); }

            Debug.Log($"[Server] {(freeze ? "Freezing" : "Unfreezing")} player {conn}");
        }
    }*/

    public void RegisterPlayer(string playerIdHash, NetworkConnection connection, string playerName)
    {
        if (!IsServer()) return;

        if (!playerRaceDataDict.ContainsKey(playerIdHash))
        {
            PlayerRaceData data = new PlayerRaceData
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

            Debug.Log("player data: " + data);

            playerRaceDataDict.Add(playerIdHash, data);
            // playerRaceData[playerIdHash] = new PlayerRaceData
            // {
            //     connection = connection,
            //     playerName = playerName,
            //     startTime = raceStartTime,
            //     finishTime = 0f,
            //     actionScore = 0,
            //     finalScore = 0,
            //     finishPosition = 0,
            //     hasFinished = false
            // };

            Debug.LogWarning($"[Server] Registered player: {playerName} with conn: {connection} and hash: {playerIdHash} for race");
        }
    }

    /// <summary>
    /// Server receives notification that a player finished
    /// Called via PlayerScoreController's ServerRPC
    /// </summary>
    public void ServerPlayerFinished(string playerHash)
    {
        if (!IsServer()) return;
        if (!raceStarted) return;

        foreach (string kvp in playerRaceDataDict.Keys) { Debug.Log($"Found keys:\n{kvp}"); }

        Debug.Log("ServerPlayerFinished(), checking for " + playerHash);

        if (!playerRaceDataDict.ContainsKey(playerHash)) 
        {
            Debug.LogError($"[Server] Unknown player tried to finish race!");
            return;
        }

        PlayerRaceData data = playerRaceDataDict[playerHash];
        NetworkConnection connection = data.connection;
        
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

        if (connection.FirstObject != null)
        {
            PlayerUIController scoreController = connection.FirstObject.GetComponent<PlayerUIController>();
            if (scoreController != null)
            {
                data.actionScore = scoreController.GetTotalScore();
            }
        }

        NotifyPlayerFinished(connection, data.finishPosition, raceTime);

        if (finishedPlayerCount >= playerRaceDataDict.Count)
        {
            EndRace();
        }
    }


    private void BroadcastRaceStarted()
    {
        if (!IsServer()) return;

        foreach (PlayerUIController uiController in playerUIControllers) { uiController.ObserversStartRaceTimer(); }
        Debug.Log("[Server] Broadcasting race start to all clients");
    }

    private void NotifyPlayerFinished(NetworkConnection connection, int position, float time)
    {
        if (!IsServer()) return;

        if (connection.FirstObject != null)
        {
            PlayerUIController scoreController = connection.FirstObject.GetComponent<PlayerUIController>();
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
                PlayerUIController scoreController = conn.FirstObject.GetComponent<PlayerUIController>();
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

        StopAllTimers();

        foreach (var kvp in playerRaceDataDict)
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

    private void StopAllTimers()
    {
        if (!IsServer()) return;

        foreach (PlayerUIController uiController in playerUIControllers)
        {
            uiController.ObserversStopRaceTimer();
        }

        Debug.Log("[Server] All race timers stopped.");
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

    private bool IsServer()
    {
        return _networkManager != null && _networkManager.IsServerStarted;
    }
}
