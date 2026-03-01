using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class RoomGenerator : MonoBehaviour
{
    public int width = 10;
    public int length = 8;
    public int height = 3;

    [Header("Door Settings")]
    public int doorHeight = 1;
    public int doorWidth = 2;
    public Vector3 doorSize = Vector3.one;

    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;

    public void GenerateRoom(DungeonRoom roomData, float tileSize)
    {
        GenerateFloor(tileSize);
        GenerateWalls(roomData, tileSize);
        SetupRoomLighting(tileSize);
    }

    public void GenerateBorderWalls(DungeonRoom roomData, float tileSize, Dictionary<Vector2Int, DungeonRoom> allRooms)
    {
        // Check if a room actually exists in those directions (not just connection exists)
        Vector2Int northPos = roomData.gridPosition + Vector2Int.up;
        Vector2Int eastPos = roomData.gridPosition + Vector2Int.right;

        // Only generate north wall if no room exists to the north
        if (!allRooms.ContainsKey(northPos))
        {
            GenerateNorthEdge(roomData, tileSize);
        }

        // Only generate east wall if no room exists to the east
        if (!allRooms.ContainsKey(eastPos))
        {
            GenerateEastEdge(roomData, tileSize);
        }
    }

    void GenerateFloor(float tileSize)
    {
        GameObject floorRoot = new GameObject("Floors");
        floorRoot.transform.SetParent(transform);
        floorRoot.transform.localPosition = Vector3.zero;
        floorRoot.isStatic = true;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                GameObject tile = Instantiate(floorPrefab, floorRoot.transform);
                tile.transform.localPosition = new Vector3(
                    (x * tileSize) + (tileSize / 2),
                    0,
                    z * tileSize + (tileSize / 2)
                );

                tile.layer = LayerMask.NameToLayer("Ground");
                tile.name = "Floor";

                int randomStep = Random.Range(0, 4);
                float randomYRotation = randomStep * 90f;
                Quaternion rotation = Quaternion.Euler(0f, randomYRotation, 0f);
                tile.transform.localRotation = rotation;
                tile.transform.localScale = new Vector3(floorPrefab.transform.localScale.x * tileSize, floorPrefab.transform.localScale.y, floorPrefab.transform.localScale.z * tileSize);
                tile.isStatic = true;
            }
        }
    }

    void GenerateWalls(DungeonRoom roomData, float tileSize)
    {
        GenerateSouthEdge(roomData, tileSize);
        GenerateWestEdge(roomData, tileSize);
        SpawnDoors(roomData, tileSize);
    }

    void GenerateSouthEdge(DungeonRoom roomData, float tileSize)
    {
        GameObject wallParent = CreateWallParent("SouthWalls");
        int z = 0;

        bool hasNeighbor = roomData.south.exists;

        int midX = width / 2;
        int halfDoor = doorWidth / 2;
        int doorStart = midX - halfDoor;
        int doorEnd = doorStart + doorWidth;

        for (int x = 0; x < width; x++)
        {
            for (int h = 0; h < height; h++)
            {
                bool carveDoor = false;

                if (hasNeighbor)
                {
                    bool inDoorRange = x >= doorStart && x < doorEnd;
                    bool belowDoorHeight = h < doorHeight;
                    carveDoor = inDoorRange && belowDoorHeight;
                }

                if (!carveDoor)
                {
                    GameObject wall = SpawnWall(x, z, h, false, tileSize);
                    wall.transform.SetParent(wallParent.transform);
                    AddWallToSegment(wallParent, wall);
                }
            }
        }
    }

    void GenerateWestEdge(DungeonRoom roomData, float tileSize)
    {
        GameObject wallParent = CreateWallParent("WestWalls");
        int x = 0;

        bool hasNeighbor = roomData.west.exists;

        int midZ = length / 2;
        int halfDoor = doorWidth / 2;
        int doorStart = midZ - halfDoor;
        int doorEnd = doorStart + doorWidth;

        for (int z = 0; z < length; z++)
        {
            for (int h = 0; h < height; h++)
            {
                bool carveDoor = false;

                if (hasNeighbor)
                {
                    bool inDoorRange = z >= doorStart && z < doorEnd;
                    bool belowDoorHeight = h < doorHeight;
                    carveDoor = inDoorRange && belowDoorHeight;
                }

                if (!carveDoor)
                {
                    GameObject wall = SpawnWall(x, z, h, true, tileSize);
                    wall.transform.SetParent(wallParent.transform);
                    AddWallToSegment(wallParent, wall);
                }
            }
        }
    }

    void GenerateNorthEdge(DungeonRoom roomData, float tileSize)
    {
        GameObject wallParent = CreateWallParent("NorthWalls");
        int z = length - 1;

        for (int x = 0; x < width; x++)
        {
            for (int h = 0; h < height; h++)
            {
                GameObject wall = SpawnWall(x, z, h, false, tileSize);
                wall.transform.SetParent(wallParent.transform);
                AddWallToSegment(wallParent, wall);
            }
        }
    }

    void GenerateEastEdge(DungeonRoom roomData, float tileSize)
    {
        GameObject wallParent = CreateWallParent("EastWalls");
        int x = width - 1;

        for (int z = 0; z < length; z++)
        {
            for (int h = 0; h < height; h++)
            {
                GameObject wall = SpawnWall(x, z, h, true, tileSize);
                wall.transform.SetParent(wallParent.transform);
                AddWallToSegment(wallParent, wall);
            }
        }
    }

    GameObject CreateWallParent(string parentName)
    {
        GameObject parent = new GameObject(parentName);
        parent.transform.SetParent(transform);
        return parent;
    }

    void AddWallToSegment(GameObject parent, GameObject wall)
    {
        // Add WallSegment to the parent object of the wall
        WallSegment wallSegment = parent.GetComponent<WallSegment>();
        if (wallSegment == null)
        {
            wallSegment = parent.AddComponent<WallSegment>();
        }

        // Get all the MeshRenderers in the wall and add them to the WallSegment
        Renderer[] renderers = wall.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            wallSegment.AddMesh(renderer);
        }
    }

    void SetupRoomLighting(float tileSize)
    {
        GameObject lightingRoot = new GameObject("RoomLighting");
        lightingRoot.transform.SetParent(transform);
        lightingRoot.transform.localPosition = Vector3.zero;

        float roomWidthWorld  = width  * tileSize;
        float roomLengthWorld = length * tileSize;
        float roomHeightWorld = height * tileSize;

        Vector3 roomCenter = new Vector3(
            roomWidthWorld / 2f,
            roomHeightWorld * 0.8f,
            roomLengthWorld / 2f
        );

        // MAIN LIGHT (soft overhead bounce)
        Light mainLight = CreateLight(
            "MainLight",
            lightingRoot.transform,
            roomCenter,
            new Color(1f, 0.92f, 0.8f),
            500f,
            Mathf.Max(roomWidthWorld, roomLengthWorld) * 0.9f,
            true
        );

        // CORNER FILL LIGHTS
        float offsetX = roomWidthWorld * 0.35f;
        float offsetZ = roomLengthWorld * 0.35f;

        Vector3[] corners =
        {
            roomCenter + new Vector3(-offsetX, 0, -offsetZ),
            roomCenter + new Vector3(offsetX, 0, -offsetZ),
            roomCenter + new Vector3(-offsetX, 0, offsetZ),
            roomCenter + new Vector3(offsetX, 0, offsetZ),
        };

        foreach (var corner in corners)
        {
            CreateLight(
                "CornerLight",
                lightingRoot.transform,
                corner,
                new Color(1f, 0.85f, 0.7f),
                100f,
                Mathf.Max(roomWidthWorld, roomLengthWorld) * 0.7f,
                false
            );
        }
    }

    Light CreateLight(
    string name,
    Transform parent,
    Vector3 localPos,
    Color color,
    float intensity,
    float range,
    bool castShadows)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(parent);
        lightObj.transform.localPosition = localPos;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = 110f;
        light.innerSpotAngle = 90f;
        light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
        lightObj.AddComponent<VolumetricAdditionalLight>();

        return light;
    }

    GameObject SpawnWall(int x, int z, int h, bool isRotated, float tileSize)
    {
        GameObject wall = Instantiate(wallPrefab, transform);
        wall.transform.localPosition = new Vector3(
            (x * tileSize) + (tileSize / 2f),
            (h + 0.5f) * tileSize,
            (z * tileSize) + (tileSize / 2f)
        );

        if (isRotated)
            wall.transform.localRotation = Quaternion.Euler(0, 90, 0);

        wall.layer = LayerMask.NameToLayer("Wall");
        wall.name = "Wall";
        wall.isStatic = true;
        wall.transform.localScale = new Vector3(
            wallPrefab.transform.localScale.x * tileSize,
            wallPrefab.transform.localScale.y * tileSize,
            wallPrefab.transform.localScale.z * tileSize
        );

        return wall;
    }

    void SpawnDoors(DungeonRoom roomData, float tileSize)
    {
        if (doorPrefab == null)
            return;

        float roomWidthWorld = width * tileSize;
        float roomLengthWorld = length * tileSize;

        // NORTH (only if neighbor is above)
        if (roomData.north.exists &&
            roomData.north.neighbor.gridPosition.y > roomData.gridPosition.y)
        {
            Vector3 pos = new Vector3(
                (roomWidthWorld * 0.5f) + (tileSize / 2),
                0,
                roomLengthWorld + (tileSize / 2)
            );

            CreateDoor(roomData, roomData.north.neighbor, pos, Quaternion.identity);
        }

        // EAST (only if neighbor is right)
        if (roomData.east.exists &&
            roomData.east.neighbor.gridPosition.x > roomData.gridPosition.x)
        {
            Vector3 pos = new Vector3(
                roomWidthWorld + (tileSize / 2),
                0,
                (roomLengthWorld * 0.5f) + (tileSize / 2)
            );

            CreateDoor(roomData, roomData.east.neighbor, pos, Quaternion.Euler(0, 90, 0));
        }
    }

    void CreateDoor(DungeonRoom a, DungeonRoom b, Vector3 localPosition, Quaternion rotation)
    {
        GameObject doorObj = Instantiate(doorPrefab, transform);
        doorObj.transform.localRotation = rotation;
        doorObj.transform.localPosition = localPosition;
        doorObj.transform.localScale = doorSize;

        Door door = doorObj.GetComponentInChildren<Door>();

        if (door != null)
        {
            // temp apply locks dependant on connection
            door.Initialize(a, b, false);
            a.Doors.Add(door);
            b.Doors.Add(door);
        }
    }

    void OnValidate()
    {
        if (doorWidth % 2 == 0)
            doorWidth += 1;
    }
}