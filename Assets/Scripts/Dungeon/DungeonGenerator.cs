using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

[System.Serializable]
public class RoomConfig
{
    public int width = 10;
    public int length = 8;
    public int height = 3;

    [Header("Door Settings")]
    public int doorHeight = 1;
    public int doorWidth = 2;
    public Vector3 doorSize = Vector3.one;

    // the basic tile/wall/door prefabs used to build the room
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public SpawnCardPool spawnPool;
}

public class DungeonGenerator : MonoBehaviour
{
    public float tileSize = 1f;
    [Tooltip("Additional spacing between rooms to prevent overlaps with variable sizes (in world units)")]
    public float roomSpacing = 0f;
    public int targetRoomCount = 12;

    [Header("Special Room Weights")]
    [Range(0f, 1f)] public float treasureChance = 0.4f;
    [Range(0f, 1f)] public float shopChance = 0.3f;
    [Range(0f, 1f)] public float secretChance = 0.2f;

    public int maxTreasureRooms = 1;
    public int maxShopRooms = 1;
    public int maxSecretRooms = 1;

    [Header("Room Visibility Settings")]
    public float roomVisibilityTriggerPadding = 1f;

    [Header("Room templates")]
    public List<RoomConfig> roomConfigs = new List<RoomConfig>();

    public NavMeshSurface navMeshSurface;

    private Dictionary<Vector2Int, DungeonRoom> rooms =
        new Dictionary<Vector2Int, DungeonRoom>();

    // Spatial grid for fast overlap queries
    private Dictionary<Vector3Int, List<DungeonRoom>> spatialGrid = 
        new Dictionary<Vector3Int, List<DungeonRoom>>();
    private float spatialGridCellSize = 20f;  // Size of each grid cell in world units

    public void GenerateDungeon()
    {
        Vector2Int startPos = Vector2Int.zero;

        DungeonRoom startRoom = new DungeonRoom();
        startRoom.gridPosition = startPos;
        startRoom.roomType = RoomType.Start;
        startRoom.distanceFromStart = 0;

        // choose a configuration for the start room (fall back to first if list empty)
        RoomConfig startConfig = ChooseConfig();
        if (startConfig != null)
        {
            startRoom.config = startConfig;
            startRoom.width = startConfig.width;
            startRoom.length = startConfig.length;
            startRoom.height = startConfig.height;
        }

        // Calculate initial world position for start room
        startRoom.worldPosition = Vector3.zero;
        rooms.Add(startPos, startRoom);
        RegisterRoomInSpatialGrid(startRoom);

        int failedAttempts = 0;

        while (rooms.Count < targetRoomCount && failedAttempts < targetRoomCount * 10)
        {
            List<DungeonRoom> existingRooms =
                new List<DungeonRoom>(rooms.Values);

            DungeonRoom randomRoom =
                existingRooms[Random.Range(0, existingRooms.Count)];

            Vector2Int dir = GetRandomDirection();
            Vector2Int newPos = randomRoom.gridPosition + dir;

            if (!rooms.ContainsKey(newPos))
            {
                // Try different room configurations until one fits
                bool roomPlaced = false;
                
                // Shuffle configs to try them in random order
                List<RoomConfig> configsToTry = new List<RoomConfig>(roomConfigs);
                for (int i = 0; i < configsToTry.Count; i++)
                {
                    int randomIndex = Random.Range(i, configsToTry.Count);
                    var temp = configsToTry[i];
                    configsToTry[i] = configsToTry[randomIndex];
                    configsToTry[randomIndex] = temp;
                }

                foreach (RoomConfig cfg in configsToTry)
                {
                    if (cfg == null) continue;

                    // Create temporary room with this config
                    DungeonRoom testRoom = new DungeonRoom();
                    testRoom.gridPosition = newPos;
                    testRoom.roomType = RoomType.Normal;
                    testRoom.parent = randomRoom;
                    testRoom.distanceFromStart = randomRoom.distanceFromStart + 1;
                    testRoom.config = cfg;
                    testRoom.width = cfg.width;
                    testRoom.length = cfg.length;
                    testRoom.height = cfg.height;

                    // Calculate what world position this would have
                    Vector3 proposedWorldPos = CalculateRoomWorldPosition(randomRoom, testRoom, dir);
                    testRoom.worldPosition = proposedWorldPos;

                    // Check if this configuration would overlap with existing rooms
                    if (!WouldRoomOverlap(testRoom))
                    {
                        // Success! This config fits
                        ConnectRooms(randomRoom, testRoom, dir);
                        rooms.Add(newPos, testRoom);
                        RegisterRoomInSpatialGrid(testRoom);
                        roomPlaced = true;
                        failedAttempts = 0; // Reset failure counter
                        break;
                    }
                }

                if (!roomPlaced)
                {
                    failedAttempts++;
                }
            }
        }

        if (rooms.Count < targetRoomCount)
        {
            Debug.LogWarning($"Could only place {rooms.Count}/{targetRoomCount} rooms due to size constraints. Consider using smaller/more uniform room sizes.");
        }

        AssignSpecialRooms();

        // Safety net: calculate positions for any rooms that somehow don't have one
        // (Most rooms already positioned during generation and AssignSpecialRooms)
        CalculateWorldPositions();
        
        // Validate no overlaps exist (should pass with size-aware generation)
        ValidateRoomPositions();

        SpawnRooms();
        GenerateAllWalls();
        navMeshSurface.RemoveData();
        navMeshSurface.BuildNavMesh();
    }

    void SpawnRooms()
    {
        foreach (var room in GetRoomsInBuildOrder())
        {
            // use the calculated world position (takes varying sizes into account)
            Vector3 worldPos = room.worldPosition;

            // create a new GameObject and add a RoomGenerator component
            GameObject roomObj = new GameObject(room.roomType + " Room");
            roomObj.transform.position = worldPos;
            RoomGenerator generator = roomObj.AddComponent<RoomGenerator>();

            // copy settings from the room's config (if any)
            if (room.config != null)
            {
                generator.width = room.config.width;
                generator.length = room.config.length;
                generator.height = room.config.height;
                generator.doorHeight = room.config.doorHeight;
                generator.doorWidth = room.config.doorWidth;
                generator.doorSize = room.config.doorSize;
                generator.floorPrefab = room.config.floorPrefab;
                generator.wallPrefab = room.config.wallPrefab;
                generator.doorPrefab = room.config.doorPrefab;
                if (room.roomType != RoomType.Start)
                {
                    RoomCombatController roomCombatController = roomObj.AddComponent<RoomCombatController>();
                    roomCombatController.spawnPool = room.config.spawnPool;
                }
            }

            room.spawnedObject = roomObj;

            GameObject triggerChild = new GameObject("RoomTrigger");
            triggerChild.transform.parent = roomObj.transform;
            RoomTrigger trigger = triggerChild.AddComponent<RoomTrigger>();
            triggerChild.layer = LayerMask.NameToLayer("RoomTrigger");
            trigger.roomData = room;
            trigger.visibilityController = FindFirstObjectByType<DungeonVisibilityController>();
            trigger.transform.position = roomObj.transform.position;

            generator.GenerateRoom(room, tileSize);

            BoxCollider col = triggerChild.AddComponent<BoxCollider>();
            col.isTrigger = true;

            var size = new Vector3(generator.width * tileSize, generator.height * tileSize, generator.length * tileSize);
            col.center = size / 2;
            col.size = new Vector3(size.x - roomVisibilityTriggerPadding, size.y, size.z - roomVisibilityTriggerPadding);

            roomObj.AddComponent<RoomVisualController>();
        }
    }

    void GenerateAllWalls()
    {
        foreach (var room in GetRoomsInBuildOrder())
        {
            RoomGenerator generator = room.spawnedObject.GetComponent<RoomGenerator>();
            if (generator != null)
            {
                generator.GenerateAllWalls(room, tileSize);
            }
        }
    }

    List<DungeonRoom> GetRoomsInBuildOrder()
    {
        List<DungeonRoom> orderedRooms = new List<DungeonRoom>(rooms.Values);

        orderedRooms.Sort((a, b) =>
        {
            int areaA = a.width * a.length;
            int areaB = b.width * b.length;

            // larger footprint first
            int areaCompare = areaB.CompareTo(areaA);
            if (areaCompare != 0)
                return areaCompare;

            // then longer side first
            int maxSideA = Mathf.Max(a.width, a.length);
            int maxSideB = Mathf.Max(b.width, b.length);
            int sideCompare = maxSideB.CompareTo(maxSideA);
            if (sideCompare != 0)
                return sideCompare;

            // stable tie-breaker for deterministic generation
            int xCompare = a.gridPosition.x.CompareTo(b.gridPosition.x);
            if (xCompare != 0)
                return xCompare;

            return a.gridPosition.y.CompareTo(b.gridPosition.y);
        });

        return orderedRooms;
    }

    public IEnumerable<DungeonRoom> GetAllRooms()
    {
        return rooms.Values;
    }

    Vector2Int GetRandomDirection()
    {
        int r = Random.Range(0, 4);

        switch (r)
        {
            case 0: return Vector2Int.up;
            case 1: return Vector2Int.down;
            case 2: return Vector2Int.left;
            default: return Vector2Int.right;
        }
    }

    void ConnectRooms(DungeonRoom a, DungeonRoom b, Vector2Int dir)
    {
        if (dir == Vector2Int.up)
        {
            a.north.exists = true;
            a.north.neighbor = b;
            b.south.exists = true;
            b.south.neighbor = a;
        }
        else if (dir == Vector2Int.down)
        {
            a.south.exists = true;
            a.south.neighbor = b;
            b.north.exists = true;
            b.north.neighbor = a;
        }
        else if (dir == Vector2Int.left)
        {
            a.west.exists = true;
            a.west.neighbor = b;
            b.east.exists = true;
            b.east.neighbor = a;
        }
        else if (dir == Vector2Int.right)
        {
            a.east.exists = true;
            a.east.neighbor = b;
            b.west.exists = true;
            b.west.neighbor = a;
        }
    }

    Vector3 CalculateRoomWorldPosition(DungeonRoom parentRoom, DungeonRoom newRoom, Vector2Int dir)
    {
        Vector3 offset = Vector3.zero;

        if (dir == Vector2Int.up) // North
        {
            float xOffset = (parentRoom.width - newRoom.width) * 0.5f * tileSize;
            offset = new Vector3(xOffset, 0, parentRoom.length * tileSize + roomSpacing);
        }
        else if (dir == Vector2Int.down) // South
        {
            float xOffset = (parentRoom.width - newRoom.width) * 0.5f * tileSize;
            offset = new Vector3(xOffset, 0, -newRoom.length * tileSize - roomSpacing);
        }
        else if (dir == Vector2Int.right) // East
        {
            float zOffset = (parentRoom.length - newRoom.length) * 0.5f * tileSize;
            offset = new Vector3(parentRoom.width * tileSize + roomSpacing, 0, zOffset);
        }
        else if (dir == Vector2Int.left) // West
        {
            float zOffset = (parentRoom.length - newRoom.length) * 0.5f * tileSize;
            offset = new Vector3(-newRoom.width * tileSize - roomSpacing, 0, zOffset);
        }

        return parentRoom.worldPosition + offset;
    }

    bool WouldRoomOverlap(DungeonRoom testRoom)
    {
        // Get all grid cells this room occupies
        HashSet<Vector3Int> roomCells = GetGridCellsForRoom(testRoom);

        // Check only against rooms in nearby cells
        HashSet<DungeonRoom> checkSet = new HashSet<DungeonRoom>();
        foreach (Vector3Int cell in roomCells)
        {
            if (spatialGrid.TryGetValue(cell, out List<DungeonRoom> cellRooms))
            {
                foreach (DungeonRoom room in cellRooms)
                {
                    checkSet.Add(room);
                }
            }
        }

        Vector3 testMin = testRoom.worldPosition;
        Vector3 testMax = new Vector3(
            testRoom.worldPosition.x + testRoom.width * tileSize,
            testRoom.worldPosition.y + testRoom.height * tileSize,
            testRoom.worldPosition.z + testRoom.length * tileSize
        );

        foreach (DungeonRoom existingRoom in checkSet)
        {
            Vector3 existMin = existingRoom.worldPosition;
            Vector3 existMax = new Vector3(
                existingRoom.worldPosition.x + existingRoom.width * tileSize,
                existingRoom.worldPosition.y + existingRoom.height * tileSize,
                existingRoom.worldPosition.z + existingRoom.length * tileSize
            );

            // Check for overlap with small tolerance
            const float tolerance = 0.01f;
            bool overlapsX = testMin.x < existMax.x - tolerance && testMax.x > existMin.x + tolerance;
            bool overlapsY = testMin.y < existMax.y - tolerance && testMax.y > existMin.y + tolerance;
            bool overlapsZ = testMin.z < existMax.z - tolerance && testMax.z > existMin.z + tolerance;

            if (overlapsX && overlapsY && overlapsZ)
            {
                return true; // Overlap detected
            }
        }

        return false; // No overlaps
    }

    void RegisterRoomInSpatialGrid(DungeonRoom room)
    {
        HashSet<Vector3Int> cells = GetGridCellsForRoom(room);
        foreach (Vector3Int cell in cells)
        {
            if (!spatialGrid.ContainsKey(cell))
            {
                spatialGrid[cell] = new List<DungeonRoom>();
            }
            spatialGrid[cell].Add(room);
        }
    }

    void UnregisterRoomFromSpatialGrid(DungeonRoom room)
    {
        HashSet<Vector3Int> cells = GetGridCellsForRoom(room);
        foreach (Vector3Int cell in cells)
        {
            if (spatialGrid.TryGetValue(cell, out List<DungeonRoom> cellRooms))
            {
                cellRooms.Remove(room);
                if (cellRooms.Count == 0)
                {
                    spatialGrid.Remove(cell);
                }
            }
        }
    }

    HashSet<Vector3Int> GetGridCellsForRoom(DungeonRoom room)
    {
        HashSet<Vector3Int> cells = new HashSet<Vector3Int>();

        Vector3 min = room.worldPosition;
        Vector3 max = new Vector3(
            room.worldPosition.x + room.width * tileSize,
            room.worldPosition.y + room.height * tileSize,
            room.worldPosition.z + room.length * tileSize
        );

        int minCellX = Mathf.FloorToInt(min.x / spatialGridCellSize);
        int maxCellX = Mathf.FloorToInt(max.x / spatialGridCellSize);
        int minCellY = Mathf.FloorToInt(min.y / spatialGridCellSize);
        int maxCellY = Mathf.FloorToInt(max.y / spatialGridCellSize);
        int minCellZ = Mathf.FloorToInt(min.z / spatialGridCellSize);
        int maxCellZ = Mathf.FloorToInt(max.z / spatialGridCellSize);

        for (int x = minCellX; x <= maxCellX; x++)
        {
            for (int y = minCellY; y <= maxCellY; y++)
            {
                for (int z = minCellZ; z <= maxCellZ; z++)
                {
                    cells.Add(new Vector3Int(x, y, z));
                }
            }
        }

        return cells;
    }

    void CalculateWorldPositions()
    {
        if (rooms.Count == 0)
            return;

        HashSet<DungeonRoom> unpositionedRooms = new HashSet<DungeonRoom>();

        foreach (var room in rooms.Values)
        {
            // Check if room has a position set
            // Note: Start room at origin is valid, non-start rooms at origin might indicate missing positioning
            bool hasNoPosition = room.worldPosition.sqrMagnitude < 0.001f && room.roomType != RoomType.Start;
            
            if (hasNoPosition)
            {
                unpositionedRooms.Add(room);
            }
        }

        if (unpositionedRooms.Count > 0)
        {
            Debug.LogError($"Found {unpositionedRooms.Count} rooms without world positions after generation. This indicates a bug in size-aware positioning.");
            
            // Attempt emergency positioning for these rooms
            foreach (var room in unpositionedRooms)
            {
                // Try to position based on any connected neighbor
                if (room.north.neighbor != null && room.north.neighbor.worldPosition.sqrMagnitude > 0.001f)
                {
                    room.worldPosition = CalculateRoomWorldPosition(room.north.neighbor, room, Vector2Int.down);
                }
                else if (room.south.neighbor != null && room.south.neighbor.worldPosition.sqrMagnitude > 0.001f)
                {
                    room.worldPosition = CalculateRoomWorldPosition(room.south.neighbor, room, Vector2Int.up);
                }
                else if (room.east.neighbor != null && room.east.neighbor.worldPosition.sqrMagnitude > 0.001f)
                {
                    room.worldPosition = CalculateRoomWorldPosition(room.east.neighbor, room, Vector2Int.left);
                }
                else if (room.west.neighbor != null && room.west.neighbor.worldPosition.sqrMagnitude > 0.001f)
                {
                    room.worldPosition = CalculateRoomWorldPosition(room.west.neighbor, room, Vector2Int.right);
                }
            }
        }
    }

    void ValidateRoomPositions()
    {
        List<DungeonRoom> roomList = new List<DungeonRoom>(rooms.Values);
        int overlapCount = 0;

        for (int i = 0; i < roomList.Count; i++)
        {
            for (int j = i + 1; j < roomList.Count; j++)
            {
                DungeonRoom roomA = roomList[i];
                DungeonRoom roomB = roomList[j];

                // Calculate bounding boxes in world space
                Vector3 aMin = roomA.worldPosition;
                Vector3 aMax = new Vector3(
                    roomA.worldPosition.x + roomA.width * tileSize,
                    roomA.worldPosition.y + roomA.height * tileSize,
                    roomA.worldPosition.z + roomA.length * tileSize
                );

                Vector3 bMin = roomB.worldPosition;
                Vector3 bMax = new Vector3(
                    roomB.worldPosition.x + roomB.width * tileSize,
                    roomB.worldPosition.y + roomB.height * tileSize,
                    roomB.worldPosition.z + roomB.length * tileSize
                );

                // Check for overlap (with small tolerance for edge-touching)
                const float tolerance = 0.01f;
                bool overlapsX = aMin.x < bMax.x - tolerance && aMax.x > bMin.x + tolerance;
                bool overlapsY = aMin.y < bMax.y - tolerance && aMax.y > bMin.y + tolerance;
                bool overlapsZ = aMin.z < bMax.z - tolerance && aMax.z > bMin.z + tolerance;

                if (overlapsX && overlapsY && overlapsZ)
                {
                    overlapCount++;
                    Debug.LogError(
                        $"UNEXPECTED: Room overlap detected after validation! {roomA.roomType} at {roomA.gridPosition} " +
                        $"({roomA.width}x{roomA.length}) overlaps with {roomB.roomType} at {roomB.gridPosition} " +
                        $"({roomB.width}x{roomB.length}). This should not happen with size-aware generation. " +
                        $"Please report this as a bug."
                    );
                }
            }
        }

        if (overlapCount == 0)
        {
            // Validation passed
        }
    }

    RoomConfig ChooseConfig()
    {
        if (roomConfigs == null || roomConfigs.Count == 0)
        {
            // return a default config so rooms still have a size
            return new RoomConfig();
        }
        return roomConfigs[Random.Range(0, roomConfigs.Count)];
    }

    void LockEntranceToRoom(DungeonRoom room)
    {
        foreach (var other in rooms.Values)
        {
            Vector2Int dir = room.gridPosition - other.gridPosition;

            if (dir == Vector2Int.up && other.north.exists)
            {
                other.north.isLocked = true;
                room.south.isLocked = true;
            }
            else if (dir == Vector2Int.down && other.south.exists)
            {
                other.south.isLocked = true;
                room.north.isLocked = true;
            }
            else if (dir == Vector2Int.left && other.west.exists)
            {
                other.west.isLocked = true;
                room.east.isLocked = true;
            }
            else if (dir == Vector2Int.right && other.east.exists)
            {
                other.east.isLocked = true;
                room.west.isLocked = true;
            }
        }
    }

    public List<DungeonRoom> GetRoomsByType(RoomType type)
    {
        List<DungeonRoom> matchingRooms = new List<DungeonRoom>();
        foreach (var room in rooms.Values)
        {
            if (room.roomType == type)
            {
                matchingRooms.Add(room);
            }
        }

        if(matchingRooms.Count == 0)
        {
            Debug.LogWarning($"No rooms of type {type} found in dungeon.");
            return null;
        }

        return matchingRooms;
    }

    void AssignSpecialRooms()
    {
        DungeonRoom bossRoom = null;
        int maxDistance = 0;

        foreach (var room in rooms.Values)
        {
            if (room.distanceFromStart > maxDistance)
            {
                maxDistance = room.distanceFromStart;
                bossRoom = room;
            }
        }

        bossRoom.roomType = RoomType.Boss;
        MarkMainPath(bossRoom);

        int treasureCount = 0;
        int shopCount = 0;
        int secretCount = 0;

        // Assign other special rooms
        foreach (var room in rooms.Values)
        {
            if (room.roomType != RoomType.Normal || room.isMainPath)
                continue;

            int connections = room.ConnectionCount();

            // Dead ends are prime candidates
            if (connections == 1)
            {
                float roll = Random.value;

                if (treasureCount < maxTreasureRooms && roll < treasureChance)
                {
                    room.roomType = RoomType.Treasure;
                    LockEntranceToRoom(room);
                    treasureCount++;
                    continue;
                }

                if (shopCount < maxShopRooms && roll < shopChance)
                {
                    room.roomType = RoomType.Shop;
                    LockEntranceToRoom(room);
                    shopCount++;
                    continue;
                }
            }

            // Secret rooms prefer 3+ connections
            if (connections >= 3 && secretCount < maxSecretRooms)
            {
                if (Random.value < secretChance)
                {
                    room.roomType = RoomType.Secret;
                    secretCount++;
                }
            }
        }

        List<Vector2Int> secretSpots = FindSecretCandidates();

        if (secretSpots.Count > 0)
        {
            Vector2Int chosen = secretSpots[Random.Range(0, secretSpots.Count)];

            // Find adjacent rooms to this secret spot
            List<(DungeonRoom neighbour, Vector2Int dirFromSecret)> adjacent =
                new List<(DungeonRoom, Vector2Int)>();

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighborPos = chosen + dir;
                if (rooms.TryGetValue(neighborPos, out DungeonRoom neighbour))
                    adjacent.Add((neighbour, dir));
            }

            if (adjacent.Count > 0)
            {
                // Pick a random adjacent room to connect to
                var selected = adjacent[Random.Range(0, adjacent.Count)];
                DungeonRoom neighbour = selected.neighbour;
                Vector2Int dirFromSecret = selected.dirFromSecret;

                // Try different configs until we find one that doesn't overlap
                bool secretRoomPlaced = false;
                List<RoomConfig> configsToTry = new List<RoomConfig>(roomConfigs);
                
                // Shuffle configs
                for (int i = 0; i < configsToTry.Count; i++)
                {
                    int randomIndex = Random.Range(i, configsToTry.Count);
                    var temp = configsToTry[i];
                    configsToTry[i] = configsToTry[randomIndex];
                    configsToTry[randomIndex] = temp;
                }

                foreach (RoomConfig cfg in configsToTry)
                {
                    if (cfg == null) continue;

                    DungeonRoom secretRoom = new DungeonRoom();
                    secretRoom.gridPosition = chosen;
                    secretRoom.roomType = RoomType.Secret;
                    secretRoom.config = cfg;
                    secretRoom.width = cfg.width;
                    secretRoom.length = cfg.length;
                    secretRoom.height = cfg.height;
                    secretRoom.parent = neighbour;
                    secretRoom.distanceFromStart = neighbour.distanceFromStart + 1;

                    // Calculate world position for this secret room
                    // dirFromSecret points from secret to neighbor, so reverse it for positioning
                    Vector3 proposedWorldPos = CalculateRoomWorldPosition(neighbour, secretRoom, dirFromSecret * -1);
                    secretRoom.worldPosition = proposedWorldPos;

                    // Check if this configuration would overlap
                    if (!WouldRoomOverlap(secretRoom))
                    {
                        // Success! This config fits
                        // Connect: neighbour -> secretRoom (reverse direction)
                        ConnectRooms(neighbour, secretRoom, dirFromSecret * -1);
                        rooms.Add(chosen, secretRoom);
                        RegisterRoomInSpatialGrid(secretRoom);
                        secretRoomPlaced = true;
                        break;
                    }
                }

                if (!secretRoomPlaced)
                {
                    Debug.LogWarning($"Could not place secret room at {chosen} - all configurations would overlap with existing rooms.");
                }
            }
        }
    }

    void MarkMainPath(DungeonRoom bossRoom)
    {
        DungeonRoom current = bossRoom;

        while (current != null)
        {
            current.isMainPath = true;
            current = current.parent;
        }
    }

    List<Vector2Int> FindSecretCandidates()
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        foreach (var room in rooms.Values)
        {
            Vector2Int[] dirs =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (var dir in dirs)
            {
                Vector2Int checkPos = room.gridPosition + dir;

                if (rooms.ContainsKey(checkPos))
                    continue;

                int adjacentRooms = 0;

                foreach (var dir2 in dirs)
                {
                    if (rooms.ContainsKey(checkPos + dir2))
                        adjacentRooms++;
                }

                // Secret rooms prefer 2+ adjacent rooms
                if (adjacentRooms >= 2)
                {
                    candidates.Add(checkPos);
                }
            }
        }

        return candidates;
    }

    void OnDrawGizmos()
    {
        if (rooms == null)
            return;

        foreach (var room in rooms.Values)
        {
            float roomWidthWorld  = room.width * tileSize;
            float roomLengthWorld = room.length * tileSize;

            Vector3 pos = room.worldPosition + new Vector3(roomWidthWorld * 0.5f, 0, roomLengthWorld * 0.5f);

            // Draw node
            Gizmos.color = GetRoomColor(room);
            Gizmos.DrawSphere(pos, roomWidthWorld * 0.08f);

            // Draw connections
            DrawConnection(room, pos, Vector3.forward * roomLengthWorld, room.north);
            DrawConnection(room, pos, Vector3.back    * roomLengthWorld, room.south);
            DrawConnection(room, pos, Vector3.right   * roomWidthWorld,  room.east);
            DrawConnection(room, pos, Vector3.left    * roomWidthWorld,  room.west);
        }
    }

    void DrawConnection(DungeonRoom room, Vector3 pos, Vector3 offset, RoomConnection connection)
    {
        if (!connection.exists)
            return;

        Gizmos.color = connection.isLocked ? Color.red : Color.green;
        Gizmos.DrawLine(pos, pos + offset);
    }

    Color GetRoomColor(DungeonRoom room)
    {
        switch (room.roomType)
        {
            case RoomType.Start:
                return Color.pink;

            case RoomType.Boss:
                return Color.red;

            case RoomType.Treasure:
                return Color.yellow;

            case RoomType.Shop:
                return Color.green;

            case RoomType.Secret:
                return Color.cyan;

            default:
                return room.isMainPath ? Color.white : Color.orange;
        }
    }
}