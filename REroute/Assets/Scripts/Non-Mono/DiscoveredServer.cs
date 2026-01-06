using System;

[Serializable]
public class DiscoveredServer
{
    public string hostName;
    public string hostAddress;
    public int connectedPlayerCount;
    public int maxPlayerCount;
    public DateTime lastBroadcast;

    public override string ToString()
    {
        return $"New server discovered => [{hostName}|{hostAddress}|{connectedPlayerCount}|{maxPlayerCount}], last broadcast: {lastBroadcast}";
    }
}
