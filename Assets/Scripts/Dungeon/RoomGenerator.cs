using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class RoomGenerator : MonoBehaviour
{
    public int width = 10;
    public int length = 8;
    public int height = 3;
    public float tileSize = 1f;

    [Header("Door Settings")]
    public int doorHeight = 1;
    public int doorWidth = 2;
    public Vector3 doorSize = Vector3.one;

    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public Material floorMat;
    public Material wallMat;

    public void GenerateRoom(DungeonRoom roomData)
    {
        GenerateFloor();
        GenerateWalls(roomData);
        SetupRoomLighting(roomData);
    }

    void GenerateFloor()
    {
        List<GameObject> floorBlocks = new List<GameObject>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                GameObject temp = new GameObject("FloorBlock");
                temp.transform.SetParent(transform);
                temp.transform.localPosition = new Vector3(
                    x * tileSize,
                    -1,
                    z * tileSize
                );

                floorBlocks.Add(temp);
            }
        }

        CombineMesh(floorBlocks, "Floor", floorMat, LayerMask.NameToLayer("Ground"));
    }

    void GenerateWalls(DungeonRoom roomData)
    {
        int midX = width / 2;
        int midZ = length / 2;

        int halfDoor = doorWidth / 2;

        int doorStartX = midX - halfDoor;
        int doorEndX   = doorStartX + doorWidth;

        int doorStartZ = midZ - halfDoor;
        int doorEndZ   = doorStartZ + doorWidth;

        List<GameObject> northWalls = new List<GameObject>();
        List<GameObject> southWalls = new List<GameObject>();
        List<GameObject> eastWalls = new List<GameObject>();
        List<GameObject> westWalls = new List<GameObject>();
        for (int x = 0; x < width; x++)
        {
            for (int h = 0; h < height; h++)
            {
                bool isDoorOpening = x >= doorStartX && x < doorEndX && h < doorHeight;
                if (!(roomData.south.exists && isDoorOpening))
                {
                    southWalls.Add(SpawnWall(x, 0, h, false));
                }
                if (!(roomData.north.exists && isDoorOpening))
                {
                    northWalls.Add(SpawnWall(x, length - 1, h, false));
                }
            }
        }

        for (int z = 1; z < length - 1; z++)
        {
            for (int h = 0; h < height; h++)
            {
                bool isDoorOpening = z >= doorStartZ && z < doorEndZ && h < doorHeight;
                if (!(roomData.west.exists && isDoorOpening))
                {
                    westWalls.Add(SpawnWall(0, z, h, true));
                }
                if (!(roomData.east.exists && isDoorOpening))
                {
                    eastWalls.Add(SpawnWall(width - 1, z, h, true));
                }
            }
        }

        CombineMesh(northWalls, "NorthWall", wallMat, LayerMask.NameToLayer("Wall"), true);
        CombineMesh(southWalls, "SouthWall", wallMat, LayerMask.NameToLayer("Wall"), true);
        CombineMesh(eastWalls, "EastWall", wallMat, LayerMask.NameToLayer("Wall"), true);
        CombineMesh(westWalls, "WestWall", wallMat, LayerMask.NameToLayer("Wall"), true);
        SpawnDoors(roomData);
    }

    void SetupRoomLighting(DungeonRoom roomData)
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

    GameObject SpawnWall(int x, int z, int h, bool isRotated)
    {
        GameObject wall = Instantiate(wallPrefab, transform);
        wall.transform.localPosition = new Vector3(
            x * tileSize,
            (h + 0.5f) * tileSize,
            z * tileSize
        );

        if (isRotated)
            wall.transform.localRotation = Quaternion.Euler(0, 90, 0);

        wall.layer = LayerMask.NameToLayer("Wall");
        wall.GetComponent<MeshRenderer>().material = wallMat;
        wall.name = "Wall";
        wall.transform.localScale = new Vector3(
            wallPrefab.transform.localScale.x * tileSize,
            wallPrefab.transform.localScale.y * tileSize,
            wallPrefab.transform.localScale.z * tileSize
        );

        return wall;
    }

    void SpawnDoors(DungeonRoom roomData)
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
                roomLengthWorld
            );

            CreateDoor(roomData, roomData.north.neighbor, pos, Quaternion.identity);
        }

        // EAST (only if neighbor is right)
        if (roomData.east.exists &&
            roomData.east.neighbor.gridPosition.x > roomData.gridPosition.x)
        {
            Vector3 pos = new Vector3(
                roomWidthWorld,
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

    void CombineMesh(List<GameObject> pieces, string name, Material mat, LayerMask layer, bool isCarveable = false)
    {
        if (pieces.Count == 0) return;

        HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();

        // Convert cube positions into grid coordinates
        foreach (var piece in pieces)
        {
            Vector3 localPos = piece.transform.localPosition;
            Vector3Int gridPos = new Vector3Int(
                Mathf.RoundToInt(localPos.x / tileSize),
                Mathf.RoundToInt((localPos.y - 0.5f * tileSize) / tileSize),
                Mathf.RoundToInt(localPos.z / tileSize)
            );

            occupied.Add(gridPos);
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int vCount = 0;

        foreach (var pos in occupied)
        {
            vCount = AddVisibleFaces(pos, occupied, vertices, triangles, uvs, vCount);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.SetVertices(vertices);
        combinedMesh.SetTriangles(triangles, 0);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();
        combinedMesh.SetUVs(0, uvs);

        GameObject combinedObj = new GameObject(name);
        combinedObj.transform.SetParent(transform);
        combinedObj.transform.localPosition = Vector3.zero;
        combinedObj.layer = layer;
        combinedObj.isStatic = true;

        if (isCarveable)
            CarveNavMesh(combinedObj);

        MeshFilter mf = combinedObj.AddComponent<MeshFilter>();
        mf.mesh = combinedMesh;

        MeshRenderer mr = combinedObj.AddComponent<MeshRenderer>();
        mr.material = mat;

        MeshCollider collider = combinedObj.AddComponent<MeshCollider>();
        collider.sharedMesh = combinedMesh;
        collider.convex = false;

        foreach (var piece in pieces)
            Destroy(piece);
    }

    private void CarveNavMesh(GameObject combinedObj)
    {
        var nav = combinedObj.AddComponent<NavMeshObstacle>();
        nav.carving = true;
        if(combinedObj.name == "EastWall")
        {
            nav.center = new Vector3((width * tileSize) - (tileSize / 2), (height * tileSize) / 2, (length * tileSize) / 2);
            nav.size = new Vector3(tileSize, height * tileSize, length * tileSize);
        }
        if(combinedObj.name == "WestWall")
        {
            nav.center = new Vector3(tileSize / 2, (height * tileSize) / 2, (length * tileSize) / 2);
            nav.size = new Vector3(tileSize, height * tileSize, length * tileSize);
        }
        if(combinedObj.name == "SouthWall")
        {
            nav.center = new Vector3((width * tileSize) / 2, (height * tileSize) / 2, tileSize / 2);
            nav.size = new Vector3((width * tileSize), height * tileSize, tileSize);
        }
        if(combinedObj.name == "NorthWall")
        {
            nav.center = new Vector3((width * tileSize) / 2, (height * tileSize) / 2, (length * tileSize) - (tileSize / 2));
            nav.size = new Vector3((width * tileSize), height * tileSize, tileSize);
        }
    }

    int AddVisibleFaces(
    Vector3Int pos,
    HashSet<Vector3Int> occupied,
    List<Vector3> vertices,
    List<int> triangles,
    List<Vector2> uvs,
    int vCount)
    {
        Vector3 basePos = new Vector3(pos.x, pos.y, pos.z) * tileSize;

        Vector3Int[] directions =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(0,0,1),
            new Vector3Int(0,0,-1)
        };

        Vector3[,] faceVerts =
        {
            // +X
            { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
            // -X
            { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) },
            // +Y
            { new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(0,1,1) },
            // -Y
            { new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,0,0), new Vector3(0,0,0) },
            // +Z
            { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) },
            // -Z
            { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) }
        };

        for (int i = 0; i < 6; i++)
        {
            if (!occupied.Contains(pos + directions[i]))
            {
                for (int j = 0; j < 4; j++)
                    vertices.Add(basePos + faceVerts[i, j] * tileSize);

                bool flip = (i == 2 || i == 3);

                if (!flip)
                {
                    triangles.Add(vCount + 0);
                    triangles.Add(vCount + 1);
                    triangles.Add(vCount + 2);
                    triangles.Add(vCount + 0);
                    triangles.Add(vCount + 2);
                    triangles.Add(vCount + 3);
                }
                else
                {
                    triangles.Add(vCount + 0);
                    triangles.Add(vCount + 2);
                    triangles.Add(vCount + 1);
                    triangles.Add(vCount + 0);
                    triangles.Add(vCount + 3);
                    triangles.Add(vCount + 2);
                }

                if (i == 2) // +Y face (floor top)
                {
                    // World-aligned UVs
                    uvs.Add(new Vector2(pos.x, pos.z));
                    uvs.Add(new Vector2(pos.x + 1, pos.z));
                    uvs.Add(new Vector2(pos.x + 1, pos.z + 1));
                    uvs.Add(new Vector2(pos.x, pos.z + 1));
                }
                else
                {
                    // Default per-face tiling for walls
                    uvs.AddRange(new Vector2[]
                    {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(1, 1),
                        new Vector2(0, 1)
                    });
                }

                vCount += 4;
            }
        }

        return vCount;
    }

    void OnValidate()
    {
        if (doorWidth % 2 == 0)
            doorWidth += 1;
    }
}