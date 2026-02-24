using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
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

    [Header("Prefabs/Assigns")]
    public GameObject startRoomPrefab;
    public GameObject normalRoomPrefab;
    public GameObject bossRoomPrefab;
    public GameObject treasureRoomPrefab;
    public GameObject shopRoomPrefab;
    public GameObject secretRoomPrefab;
    public RoomGenerator roomTemplate;
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

                ConnectRooms(randomRoom, newRoom, dir);

                rooms.Add(newPos, newRoom);
            }
        }

        AssignSpecialRooms();
        SpawnRooms();
        navMeshSurface.RemoveData();
        navMeshSurface.BuildNavMesh();
        FindFirstObjectByType<SpawnNodeManager>().Initialize();
    }

    void SpawnRooms()
    {
        float roomWidthWorld  = roomTemplate.width  * roomTemplate.tileSize;
        float roomLengthWorld = roomTemplate.length * roomTemplate.tileSize;

        foreach (var room in rooms.Values)
        {
            Vector3 worldPos = new Vector3(
                room.gridPosition.x * roomWidthWorld,
                0,
                room.gridPosition.y * roomLengthWorld
            );

            GameObject prefabToUse = normalRoomPrefab;

            switch (room.roomType)
            {
                case RoomType.Start:
                    prefabToUse = startRoomPrefab;
                    break;
                case RoomType.Normal:
                    prefabToUse = normalRoomPrefab;
                    break;
                case RoomType.Boss:
                    prefabToUse = bossRoomPrefab;
                    break;
                case RoomType.Treasure:
                    prefabToUse = treasureRoomPrefab;
                    break;
                case RoomType.Shop:
                    prefabToUse = shopRoomPrefab;
                    break;
                case RoomType.Secret:
                    prefabToUse = secretRoomPrefab;
                    break;
            }

            GameObject roomObj = Instantiate(prefabToUse, worldPos, Quaternion.identity);
            room.spawnedObject = roomObj;

            RoomTrigger trigger = roomObj.AddComponent<RoomTrigger>();
            roomObj.layer = LayerMask.NameToLayer("WorldBounds");
            trigger.roomData = room;
            trigger.visibilityController = FindFirstObjectByType<DungeonVisibilityController>();

            RoomGenerator generator = roomObj.GetComponent<RoomGenerator>();
            generator.GenerateRoom(room);

            BoxCollider col = roomObj.AddComponent<BoxCollider>();
            col.isTrigger = true;

            var size = new Vector3(generator.width * generator.tileSize, generator.height * generator.tileSize, generator.length * generator.tileSize);
            col.center = size / 2;
            col.size = new Vector3(size.x - roomVisibilityTriggerPadding, size.y, size.z - roomVisibilityTriggerPadding);

            roomObj.AddComponent<RoomVisualController>();
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
        if (rooms == null || roomTemplate == null)
            return;

        foreach (var room in rooms.Values)
        {
            float roomWidthWorld  = roomTemplate.width  * roomTemplate.tileSize;
            float roomLengthWorld = roomTemplate.length * roomTemplate.tileSize;

            Vector3 pos = new Vector3(
                room.gridPosition.x * roomWidthWorld + roomWidthWorld * 0.5f,
                0,
                room.gridPosition.y * roomLengthWorld + roomLengthWorld * 0.5f
            );

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