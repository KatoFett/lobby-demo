using UnityEngine;

public enum CommandType
{
    Move
}

public struct Command
{
    public Command(int playerID, int[] unitIDs, CommandType type, Vector3 destination)
    {
        PlayerID = playerID;
        UnitIDs = unitIDs;
        Type = type;
        Destination = destination;
    }

    public int PlayerID { get; }
    public int[] UnitIDs { get; }
    public CommandType Type { get; }
    public Vector3? Destination { get; }
}