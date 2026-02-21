using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomConnection
{
    public bool exists;
    public bool isLocked;
    public DungeonRoom neighbor;
}

public class DungeonRoom
{
    public Vector2Int gridPosition;

    public RoomConnection north = new RoomConnection();
    public RoomConnection south = new RoomConnection();
    public RoomConnection east  = new RoomConnection();
    public RoomConnection west  = new RoomConnection();

    public RoomType roomType;

    public DungeonRoom parent;
    public int distanceFromStart;
    public bool isMainPath;
    
    public List<Door> Doors = new List<Door>();
    public bool IsCleared;

    public bool visited;
    public GameObject spawnedObject;

    public int ConnectionCount()
    {
        int count = 0;
        if (north.exists) count++;
        if (south.exists) count++;
        if (east.exists) count++;
        if (west.exists) count++;
        return count;
    }
}

public enum RoomType
{
    Start,
    Normal,
    Boss,
    Treasure,
    Shop,
    Secret
}