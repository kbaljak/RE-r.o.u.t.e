
using System;
using FishNet.Connection;

[Serializable]
public class PlayerRaceData
{
    public NetworkConnection connection;
    public string playerName;
    public float startTime;
    public float finishTime;
    public int actionScore;
    public int finalScore;
    public int finishPosition;
    public bool hasFinished;
}