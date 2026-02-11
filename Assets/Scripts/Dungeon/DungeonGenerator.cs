using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

enum Cardinal { North, South, East, West }

public class DungeonGenerator : MonoBehaviour
{
    [Header("Seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    [Header("Generation Settings")]
    public int maxRooms = 20;
    public int minBranches = 1;
    public int maxBranches = 3;

    [Header("Validation")]
    public int minimumBossDepth = 6;
    public int maxGenerationAttempts = 10;

    [Header("Loops")]
    [Range(0f, 1f)]
    public float loopChance = 0.15f;

    [Header("Layout Settings")]
    public int roomSize = 20;
    public int floorHeight = 10;

    [Header("Wall/Door")]
    public GameObject doorPrefab;
    public GameObject wallSolidPrefab;
    public GameObject wallDoorPrefab;
    public float wallHalfHeightOffset = 7.5f; // Adjust based on wall prefab
    public float doorHeightOffset = 0f; // Adjust based on door prefab

    [Header("Locks")]
    [Range(0f, 1f)]
    public float lockChance = 0.2f;

    [Header("Secrets")]
    public int secretRoomCount = 1;

    [Header("Prefabs")]
    public RoomPrefabDatabase prefabDatabase;
    [Header("Navmesh")]
    public NavMeshSurface navMeshSurface;

    public DungeonGraph Graph { get; private set; }

    private Dictionary<Vector3Int, RoomNode> tileToRoom = new Dictionary<Vector3Int, RoomNode>();

    private void Start()
    {
        navMeshSurface.RemoveData();
        GenerateDungeon();
        navMeshSurface.BuildNavMesh();
    }

    public void GenerateDungeon()
    {
        int attempts = 0;
        bool valid = false;

        while (!valid && attempts < maxGenerationAttempts)
        {
            attempts++;

            Graph = new DungeonGraph();

            if (useRandomSeed)
                seed = Random.Range(int.MinValue, int.MaxValue);

            Random.InitState(seed);

            GenerateGraph();
            AssignRoomTypes();
            LayoutRooms();
            GenerateSecretRooms();
            GenerateLoops();
            ApplyLocks();

            valid = ValidateDungeon();
        }

        if (!valid)
        {
            Debug.LogError("Failed to generate a valid dungeon.");
            return;
        }

        Debug.Log($"Dungeon Generated in {attempts} attempt(s). Seed: {seed}");

        SpawnRooms();
        SpawnDoors();
        BuildDungeonBoundaries();
    }

    void SpawnDoors()
    {
        foreach (var room in Graph.Rooms)
        {
            foreach (var connection in room.Connections)
            {
                // Prevent duplicates
                if (room.Id > connection.Target.Id)
                    continue;

                SpawnDoorBetween(room, connection.Target, connection.IsLocked);
            }
        }
    }

    void SpawnDoorBetween(RoomNode a, RoomNode b, bool isLocked)
    {
        foreach (var tile in a.OccupiedTiles)
        {
            foreach (Cardinal dir in System.Enum.GetValues(typeof(Cardinal)))
            {
                Vector3Int neighborTile = tile + DirectionToGridOffset(dir);

                if (tileToRoom.TryGetValue(neighborTile, out var neighbor))
                {
                    if (neighbor == b)
                    {
                        Vector3 pos = GetWallWorldPosition(tile, dir, doorHeightOffset);
                        Quaternion rot = GetWallRotation(dir);

                        GameObject doorGO = Instantiate(doorPrefab, pos, rot, transform);

                        Door door = doorGO.GetComponentInChildren<Door>();
                        door.Initialize(a, b, isLocked);

                        return; // Only one door per connection
                    }
                }
            }
        }

        Debug.LogWarning($"Could not find door position between {a.Id} and {b.Id}");
    }

    void GenerateGraph()
    {
        Graph = new DungeonGraph();

        RoomNode start = new RoomNode
        {
            Id = 0,
            Type = RoomType.Start,
            Depth = 0
        };

        Graph.Rooms.Add(start);

        Queue<RoomNode> frontier = new Queue<RoomNode>();
        frontier.Enqueue(start);

        while (Graph.Rooms.Count < maxRooms && frontier.Count > 0)
        {
            RoomNode current = frontier.Dequeue();

            int branches = Random.Range(minBranches, maxBranches + 1);

            for (int i = 0; i < branches; i++)
            {
                if (Graph.Rooms.Count >= maxRooms)
                    break;

                RoomNode newRoom = new RoomNode
                {
                    Id = Graph.Rooms.Count,
                    Depth = current.Depth + 1,
                    Type = RoomType.Undefined
                };

                current.Connections.Add(new RoomConnection
                {
                    Target = newRoom,
                    ConnectionType = ConnectionType.Door
                });

                newRoom.Connections.Add(new RoomConnection
                {
                    Target = current,
                    ConnectionType = ConnectionType.Door
                });

                Graph.Rooms.Add(newRoom);
                frontier.Enqueue(newRoom);
            }
        }

        // Assign Boss to deepest room
        RoomNode deepest = Graph.Rooms
            .OrderByDescending(r => r.Depth)
            .First();

        deepest.Type = RoomType.Boss;
    }

    bool ValidateDungeon()
    {
        RoomNode boss = Graph.Rooms.FirstOrDefault(r => r.Type == RoomType.Boss);
        if (boss == null)
            return false;

        if (boss.Depth < minimumBossDepth)
        {
            Debug.Log("Boss too close. Regenerating...");
            return false;
        }

        if (!IsBossReachable())
        {
            Debug.Log("Boss unreachable. Regenerating...");
            return false;
        }

        return true;
    }

    void AssignRoomTypes()
    {
        int treasureCount = 0;
        int maxTreasureRooms = Mathf.Max(1, maxRooms / 6);

        RoomNode start = Graph.GetStartRoom();

        RoomNode boss = Graph.Rooms
            .OrderByDescending(r => r.Depth)
            .First();

        boss.Type = RoomType.Boss;

        foreach (var room in Graph.Rooms)
        {
            if (room == start || room == boss)
                continue;

            if (room.Connections.Count == 1 &&
                room.Type != RoomType.Boss &&
                treasureCount < maxTreasureRooms)
            {
                room.Type = RoomType.Treasure;
                treasureCount++;
            }

            int maxDepth = Graph.Rooms.Max(r => r.Depth);
            float depth01 = room.Depth / (float)maxDepth;

            if (room.Connections.Count == 1 && depth01 > 0.3f)
            {
                room.Type = RoomType.Treasure;
            }
            else if (depth01 > 0.6f)
            {
                room.Type = Random.value < 0.8f ? RoomType.Combat : RoomType.Shop;
            }
            else
            {
                room.Type = RoomType.Combat;
            }
        }
    }

    bool IsBossReachable()
    {
        RoomNode start = Graph.GetStartRoom();
        RoomNode boss = Graph.Rooms
            .FirstOrDefault(r => r.Type == RoomType.Boss);

        HashSet<RoomNode> visited = new HashSet<RoomNode>();
        Queue<RoomNode> queue = new Queue<RoomNode>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == boss)
                return true;

            foreach (var conn in current.Connections)
            {
                if (!visited.Contains(conn.Target))
                {
                    visited.Add(conn.Target);
                    queue.Enqueue(conn.Target);
                }
            }
        }

        return false;
    }

    void LayoutRooms()
    {
        if (Graph == null || Graph.Rooms.Count == 0)
            return;

        tileToRoom.Clear();

        Queue<RoomNode> queue = new Queue<RoomNode>();
        RoomNode start = Graph.GetStartRoom();
        var startEntry = prefabDatabase.GetEntry(start.Type);
        Vector2Int startSize = startEntry != null ? startEntry.gridSize : Vector2Int.one;

        ReserveTiles(start, Vector3Int.zero, startSize, tileToRoom);
        queue.Enqueue(start);

        Vector3Int[] directions =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.forward,
            Vector3Int.back
        };

        while (queue.Count > 0)
        {
            RoomNode current = queue.Dequeue();
            var currentEntry = prefabDatabase.GetEntry(current.Type);
            Vector2Int currentSize = currentEntry != null ? currentEntry.gridSize : Vector2Int.one;

            foreach (var connection in current.Connections)
            {
                RoomNode neighbor = connection.Target;
                if (neighbor.OccupiedTiles.Count > 0)
                    continue; // Already placed

                var neighborEntry = prefabDatabase.GetEntry(neighbor.Type);
                Vector2Int neighborSize = neighborEntry != null ? neighborEntry.gridSize : Vector2Int.one;

                bool placed = false;

                // Shuffle directions for procedural randomness
                directions = directions.OrderBy(_ => Random.value).ToArray();

                foreach (var dir in directions)
                {
                    Vector3Int candidate = current.GridPosition;

                    // Offset depends on direction and room sizes
                    if (dir == Vector3Int.right)
                        candidate += new Vector3Int(currentSize.x, 0, 0);
                    else if (dir == Vector3Int.left)
                        candidate += new Vector3Int(-neighborSize.x, 0, 0);
                    else if (dir == Vector3Int.forward)
                        candidate += new Vector3Int(0, 0, currentSize.y);
                    else if (dir == Vector3Int.back)
                        candidate += new Vector3Int(0, 0, -neighborSize.y);

                    // Check if tiles are free
                    if (CanPlaceRoom(candidate, neighborSize, tileToRoom))
                    {
                        ReserveTiles(neighbor, candidate, neighborSize, tileToRoom);
                        queue.Enqueue(neighbor);
                        placed = true;
                        break;
                    }
                }

                const int maxPlacementAttempts = 8;

                if (!placed)
                {
                    neighbor.PlacementAttempts++;

                    if (neighbor.PlacementAttempts < maxPlacementAttempts)
                    {
                        queue.Enqueue(neighbor);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to place room {neighbor.Id} after {maxPlacementAttempts} attempts.");
                    }
                }
            }
        }
    }

    void SpawnRooms()
    {
        foreach (var room in Graph.Rooms)
        {
            var entry = prefabDatabase.GetEntry(room.Type);
            if (entry == null || entry.prefab == null)
                continue;

            Vector3 worldPos = GetRoomWorldCenter(room);

            GameObject roomGO = Instantiate(entry.prefab, worldPos, Quaternion.identity, transform);

            // Optional: store size on node for later door alignment
            room.WorldSize = entry.gridSize;
        }
    }

    Vector3 GetRoomWorldCenter(RoomNode room)
    {
        float offsetX = (room.Size.x - 1) * 0.5f;
        float offsetZ = (room.Size.y - 1) * 0.5f;

        return new Vector3(
            (room.GridPosition.x + offsetX) * roomSize,
            room.GridPosition.y * floorHeight,
            (room.GridPosition.z + offsetZ) * roomSize
        );
    }

    bool CanPlaceRoom(Vector3Int basePos, Vector2Int size, Dictionary<Vector3Int, RoomNode> occupied)
    {
        for (int x = 0; x < size.x; x++)
            for (int z = 0; z < size.y; z++)
            {
                Vector3Int tile = basePos + new Vector3Int(x, 0, z);
                if (occupied.ContainsKey(tile))
                    return false;
            }
        return true;
    }

    void ReserveTiles(RoomNode room, Vector3Int basePos, Vector2Int size, Dictionary<Vector3Int, RoomNode> occupied)
    {
        room.OccupiedTiles.Clear();
        room.GridPosition = basePos;
        room.Size = size;

        for (int x = 0; x < size.x; x++)
            for (int z = 0; z < size.y; z++)
            {
                Vector3Int tile = basePos + new Vector3Int(x, 0, z);
                room.OccupiedTiles.Add(tile);
                tileToRoom[tile] = room;
            }
    }

    void BuildDungeonBoundaries()
    {
        HashSet<Vector3Int> allTiles = new HashSet<Vector3Int>();

        foreach (var room in Graph.Rooms)
            foreach (var tile in room.OccupiedTiles)
                allTiles.Add(tile);

        foreach (var tile in allTiles)
        {
            foreach (Cardinal dir in System.Enum.GetValues(typeof(Cardinal)))
            {
                Vector3Int neighborTile = tile + DirectionToGridOffset(dir);

                bool neighborExists = allTiles.Contains(neighborTile);

                RoomNode roomA = GetRoomAtTile(tile);
                RoomNode roomB = GetRoomAtTile(neighborTile);

                Vector3 wallWorldPos = GetWallWorldPosition(tile, dir, wallHalfHeightOffset);

                if (!neighborExists)
                {
                    InstantiateWall(tile, wallSolidPrefab, wallWorldPos, GetWallRotation(dir), transform);
                }
                else if (roomA != roomB && AreRoomsConnected(roomA, roomB))
                {
                    InstantiateWall(tile, wallDoorPrefab, wallWorldPos, GetWallRotation(dir), transform);
                }
                else if (roomA != roomB)
                {
                    InstantiateWall(tile, wallSolidPrefab, wallWorldPos, GetWallRotation(dir), transform);
                }
            }
        }
    }

    private void InstantiateWall(Vector3Int tile, GameObject prefab, Vector3 pos, Quaternion rot, Transform t)
    {
        GameObject wallGO = Instantiate(prefab, pos, rot, t);
        for(int i = 0; i < wallGO.transform.childCount; i++)
        {
            wallGO.transform.GetChild(i).GetComponent<Renderer>().material = prefabDatabase.GetEntry(GetRoomAtTile(tile).Type).mat;   
        }
    }

    RoomNode GetRoomAtTile(Vector3Int tile)
    {
        return tileToRoom.TryGetValue(tile, out var room) ? room : null;
    }

    bool AreRoomsConnected(RoomNode a, RoomNode b)
    {
        return a.Connections.Any(c => c.Target == b);
    }

    Vector3 GetWallWorldPosition(Vector3Int tile, Cardinal dir, float verticalOffset)
    {
        Vector3 basePos = new Vector3(
            tile.x * roomSize,
            tile.y * floorHeight + verticalOffset,
            tile.z * roomSize
        );

        switch (dir)
        {
            case Cardinal.North: return basePos + new Vector3(0, 0, roomSize * 0.5f);
            case Cardinal.South: return basePos + new Vector3(0, 0, -roomSize * 0.5f);
            case Cardinal.East:  return basePos + new Vector3(roomSize * 0.5f, 0, 0);
            case Cardinal.West:  return basePos + new Vector3(-roomSize * 0.5f, 0, 0);
        }

        return basePos;
    }

    Quaternion GetWallRotation(Cardinal dir)
    {
        switch (dir)
        {
            case Cardinal.North: return Quaternion.identity;
            case Cardinal.South: return Quaternion.Euler(0, 180, 0);
            case Cardinal.East:  return Quaternion.Euler(0, 90, 0);
            case Cardinal.West:  return Quaternion.Euler(0, -90, 0);
        }

        return Quaternion.identity;
    }

    Vector3Int DirectionToGridOffset(Cardinal dir)
    {
        switch (dir)
        {
            case Cardinal.North: return Vector3Int.forward;
            case Cardinal.South: return Vector3Int.back;
            case Cardinal.East:  return Vector3Int.right;
            case Cardinal.West:  return Vector3Int.left;
        }
        return Vector3Int.zero;
    }

    void GenerateLoops()
    {
        var roomLookup = new Dictionary<Vector3Int, RoomNode>();

        foreach (var room in Graph.Rooms)
        {
            if (!roomLookup.ContainsKey(room.GridPosition))
                roomLookup.Add(room.GridPosition, room);
        }

        Vector3Int[] directions =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.forward,
            Vector3Int.back
        };

        foreach (var room in Graph.Rooms)
        {
            foreach (var tile in room.OccupiedTiles)
            {
                foreach (var dir in directions)
                {
                    Vector3Int neighborTile = tile + dir;

                    if (!tileToRoom.TryGetValue(neighborTile, out var neighbor))
                        continue;

                    if (neighbor == room)
                        continue;

                    if (room.Connections.Any(c => c.Target == neighbor))
                        continue;

                    if (Random.value < loopChance &&
                        room.Type != RoomType.Boss &&
                        neighbor.Type != RoomType.Boss)
                    {
                        CreateConnection(room, neighbor);
                    }
                }
            }
        }
    }

    void CreateConnection(RoomNode a, RoomNode b)
    {
        a.Connections.Add(new RoomConnection { Target = b, ConnectionType = ConnectionType.Door });
        b.Connections.Add(new RoomConnection { Target = a, ConnectionType = ConnectionType.Door });
    }

    void ApplyLocks()
    {
        foreach (var room in Graph.Rooms)
        {
            // Dead end?
            if (room.Connections.Count != 1)
                continue;

            // Don't lock start or boss
            if (room.Type == RoomType.Start || room.Type == RoomType.Boss)
                continue;

            if (Random.value < lockChance)
            {
                RoomConnection conn = room.Connections[0];

                conn.IsLocked = true;

                // Make sure reverse connection is also locked
                RoomNode parent = conn.Target;
                var reverse = parent.Connections
                    .FirstOrDefault(c => c.Target == room);

                if (reverse != null)
                    reverse.IsLocked = true;

                // Mark this room as bonus
                room.Type = RoomType.Treasure;
            }
        }
    }

    void GenerateSecretRooms()
    {
        Vector3Int[] directions =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.forward,
            Vector3Int.back
        };

        for (int i = 0; i < secretRoomCount; i++)
        {
            var candidates = Graph.Rooms
                .Where(r => r.Type == RoomType.Combat)
                .OrderBy(x => Random.value)
                .ToList();

            foreach (var room in candidates)
            {
                foreach (var dir in directions)
                {
                    Vector3Int secretPos = room.GridPosition + dir;

                    if (tileToRoom.ContainsKey(secretPos))
                        continue;

                    // Create secret
                    RoomNode secret = new RoomNode
                    {
                        Id = Graph.Rooms.Count,
                        Type = RoomType.Secret,
                        Depth = room.Depth,
                    };

                    room.Connections.Add(new RoomConnection
                    {
                        Target = secret,
                        ConnectionType = ConnectionType.Door
                    });

                    secret.Connections.Add(new RoomConnection
                    {
                        Target = room,
                        ConnectionType = ConnectionType.Door
                    });

                    Vector2Int secretSize = prefabDatabase.GetEntry(RoomType.Secret).gridSize;

                    if (!CanPlaceRoom(secretPos, secretSize, tileToRoom))
                        continue;

                    ReserveTiles(secret, secretPos, secretSize, tileToRoom);
                    Graph.Rooms.Add(secret);
                    break;
                }

                break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (Graph == null || Graph.Rooms == null)
            return;

        foreach (var room in Graph.Rooms)
        {
            Vector3 pos = new Vector3(
                room.GridPosition.x * roomSize,
                room.GridPosition.y * floorHeight,
                room.GridPosition.z * roomSize
            );

            Gizmos.color = GetRoomColor(room.Type);
            Gizmos.DrawCube(pos, Vector3.one * 3f);

        foreach (var conn in room.Connections)
        {
            Vector3 targetPos = new Vector3(
                conn.Target.GridPosition.x * roomSize,
                conn.Target.GridPosition.y * floorHeight,
                conn.Target.GridPosition.z * roomSize
            );

            if (conn.IsLocked)
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.white;

            Gizmos.DrawLine(pos, targetPos);
        }
        }
    }

    Color GetRoomColor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start: return Color.green;
            case RoomType.Boss: return Color.red;
            case RoomType.Treasure: return Color.yellow;
            case RoomType.Shop: return Color.cyan;
            case RoomType.Secret: return Color.magenta;
            default: return Color.gray;
        }
    }
}