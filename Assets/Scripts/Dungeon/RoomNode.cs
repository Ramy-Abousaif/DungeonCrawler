using System.Collections.Generic;
using UnityEngine;

public class RoomNode
{
    public int Id;
    public RoomType Type;
    public int Depth;
    public Vector2Int WorldSize = Vector2Int.one;
    public Vector3Int GridPosition;

    public List<RoomConnection> Connections = new List<RoomConnection>();
    public Vector2Int Size = Vector2Int.one;
    public List<Vector3Int> OccupiedTiles = new List<Vector3Int>();
    public int PlacementAttempts;
}

public class RoomConnection
{
    public RoomNode Target;
    public ConnectionType ConnectionType;
    public bool IsLocked;
}