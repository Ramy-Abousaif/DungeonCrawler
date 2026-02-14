using System.Collections.Generic;
using UnityEngine;

public class RoomNode
{
    public GameObject RoomObject;
    public int Id;
    public RoomType Type;
    public int Depth;
    public Vector2 WorldSize = Vector2.one;
    public Vector3Int GridPosition;

    public List<RoomConnection> Connections = new List<RoomConnection>();
    public Vector2Int GridSize = Vector2Int.one;
    public List<Vector3Int> OccupiedTiles = new List<Vector3Int>();
    public int PlacementAttempts;
    public RoomVisualController VisualController;
    public bool Visited;
    public bool IsCleared = false;
    public List<Door> Doors = new List<Door>();
}

public class RoomConnection
{
    public RoomNode Target;
    public ConnectionType ConnectionType;
    public bool IsLocked;
}