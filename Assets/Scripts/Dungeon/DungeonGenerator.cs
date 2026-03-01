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

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
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

        rooms.Add(startPos, startRoom);

        while (rooms.Count < targetRoomCount)
        {
            List<DungeonRoom> existingRooms =
                new List<DungeonRoom>(rooms.Values);

            DungeonRoom randomRoom =
                existingRooms[Random.Range(0, existingRooms.Count)];

            Vector2Int dir = GetRandomDirection();
            Vector2Int newPos = randomRoom.gridPosition + dir;

            if (!rooms.ContainsKey(newPos))
            {
                DungeonRoom newRoom = new DungeonRoom();
                newRoom.gridPosition = newPos;
                newRoom.roomType = RoomType.Normal;
                newRoom.parent = randomRoom;
                newRoom.distanceFromStart = randomRoom.distanceFromStart + 1;

                // assign a random template for this new room
                RoomConfig cfg = ChooseConfig();
                if (cfg != null)
                {
                    newRoom.config = cfg;
                    newRoom.width = cfg.width;
                    newRoom.length = cfg.length;
                    newRoom.height = cfg.height;
                }

                ConnectRooms(randomRoom, newRoom, dir);

                rooms.Add(newPos, newRoom);
            }
        }

        AssignSpecialRooms();

        // calculate all room world positions now that secret rooms (and any extras)
        // have been added and have proper size data
        CalculateWorldPositions();

        SpawnRooms();
        GenerateBorderWalls();
        navMeshSurface.RemoveData();
        navMeshSurface.BuildNavMesh();

        // move the player into the start room now that we know its world position
        PositionPlayerAtStart();

        FindFirstObjectByType<SpawnNodeManager>().Initialize();
    }

    void SpawnRooms()
    {
        foreach (var room in rooms.Values)
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

    void GenerateBorderWalls()
    {
        foreach (var room in rooms.Values)
        {
            RoomGenerator generator = room.spawnedObject.GetComponent<RoomGenerator>();
            if (generator != null)
            {
                generator.GenerateBorderWalls(room, tileSize, rooms);
            }
        }
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

    void CalculateWorldPositions()
    {
        if (rooms.Count == 0)
            return;

        // anchor start room at origin if possible
        DungeonRoom start;
        if (!rooms.TryGetValue(Vector2Int.zero, out start))
            start = new List<DungeonRoom>(rooms.Values)[0];

        // initialize positions (necessary for neighbors to reference)
        foreach (var r in rooms.Values)
            r.worldPosition = Vector3.zero;
        start.worldPosition = Vector3.zero;

        bool changed = true;
        int iterations = 0;
        int maxIterations = rooms.Count * 10;

        while (changed && iterations < maxIterations)
        {
            iterations++;
            changed = false;

            // keep anchor locked
            start.worldPosition = Vector3.zero;

            foreach (var room in rooms.Values)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;

                // for each neighbor compute where *this* room should be relative to them
                if (room.north.exists && room.north.neighbor != null)
                {
                    float offset = (room.length * tileSize + room.north.neighbor.length * tileSize) * 0.5f;
                    Vector3 candidate = room.north.neighbor.worldPosition - Vector3.forward * offset;
                    sum += candidate;
                    count++;
                }
                if (room.south.exists && room.south.neighbor != null)
                {
                    float offset = (room.length * tileSize + room.south.neighbor.length * tileSize) * 0.5f;
                    Vector3 candidate = room.south.neighbor.worldPosition - Vector3.back * offset; // back = -forward
                    sum += candidate;
                    count++;
                }
                if (room.east.exists && room.east.neighbor != null)
                {
                    float offset = (room.width * tileSize + room.east.neighbor.width * tileSize) * 0.5f;
                    Vector3 candidate = room.east.neighbor.worldPosition - Vector3.right * offset;
                    sum += candidate;
                    count++;
                }
                if (room.west.exists && room.west.neighbor != null)
                {
                    float offset = (room.width * tileSize + room.west.neighbor.width * tileSize) * 0.5f;
                    Vector3 candidate = room.west.neighbor.worldPosition - Vector3.left * offset; // left = -right
                    sum += candidate;
                    count++;
                }

                if (count > 0)
                {
                    Vector3 desired = sum / count;
                    if ((room.worldPosition - desired).sqrMagnitude > 0.0001f)
                    {
                        room.worldPosition = desired;
                        changed = true;
                    }
                }
            }
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

    void PositionPlayerAtStart()
    {
        
        if (rooms.TryGetValue(Vector2Int.zero, out DungeonRoom start))
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                // center on floor, preserve current y position or bump slightly above floor
                Vector3 offset = new Vector3(start.width * tileSize * 0.5f,
                                             0f,
                                             start.length * tileSize * 0.5f);
                Vector3 newPos = start.worldPosition + offset;
                // keep existing Y if above zero, otherwise set to 1
                if (playerObj.transform.position.y > 0f)
                    newPos.y = playerObj.transform.position.y;
                else
                    newPos.y = 1f;
                playerObj.transform.position = newPos;
            }
        }
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
        LockEntranceToRoom(bossRoom);

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
                    treasureCount++;
                    continue;
                }

                if (shopCount < maxShopRooms && roll < shopChance)
                {
                    room.roomType = RoomType.Shop;
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

            DungeonRoom secretRoom = new DungeonRoom();
            secretRoom.gridPosition = chosen;
            secretRoom.roomType = RoomType.Secret;

            // choose a configuration for the secret room as well
            RoomConfig cfg = ChooseConfig();
            if (cfg != null)
            {
                secretRoom.config = cfg;
                secretRoom.width = cfg.width;
                secretRoom.length = cfg.length;
                secretRoom.height = cfg.height;
            }

            // connect secret room to one of its adjacent existing rooms
            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighborPos = chosen + dir;
                if (rooms.TryGetValue(neighborPos, out DungeonRoom neighbour))
                {
                    ConnectRooms(neighbour, secretRoom, dir * -1); // dir from neighbour to secret
                    break;
                }
            }

            rooms.Add(chosen, secretRoom);
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