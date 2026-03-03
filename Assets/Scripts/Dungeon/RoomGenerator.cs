using System.Collections.Generic;
using UnityEngine;

// Generates room geometry including floors, walls, and doors
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
    }

    public void GenerateAllWalls(DungeonRoom roomData, float tileSize)
    {
        // TILE-BASED WALL GENERATION:
        // For each floor tile, check its 4 edges:
        // If edge is at room boundary → place wall
        // If edge connects to another room with door → skip wall (carve opening)
        // If edge connects to another room without door → skip wall (shared space)

        // Get actual room dimensions from config, not from inspector values
        int roomWidth = roomData.config.width;
        int roomLength = roomData.config.length;
        int roomHeight = roomData.config.height;
        int doorWidth = roomData.config.doorWidth;
        int doorHeight = roomData.config.doorHeight;

        // Create direction-specific wall containers
        GameObject northWallsParent = new GameObject("North");
        northWallsParent.transform.SetParent(transform);
        northWallsParent.transform.localPosition = Vector3.zero;
        northWallsParent.AddComponent<WallSegment>();

        GameObject southWallsParent = new GameObject("South");
        southWallsParent.transform.SetParent(transform);
        southWallsParent.transform.localPosition = Vector3.zero;
        southWallsParent.AddComponent<WallSegment>();

        GameObject eastWallsParent = new GameObject("East");
        eastWallsParent.transform.SetParent(transform);
        eastWallsParent.transform.localPosition = Vector3.zero;
        eastWallsParent.AddComponent<WallSegment>();

        GameObject westWallsParent = new GameObject("West");
        westWallsParent.transform.SetParent(transform);
        westWallsParent.transform.localPosition = Vector3.zero;
        westWallsParent.AddComponent<WallSegment>();

        GameObject doorRoot = new GameObject("Doors");
        doorRoot.transform.SetParent(transform);
        doorRoot.transform.localPosition = Vector3.zero;

        for (int x = 0; x < roomWidth; x++)
        {
            for (int z = 0; z < roomLength; z++)
            {
                for (int h = 0; h < roomHeight; h++)
                {
                    // Check all 4 edges of this tile
                    GenerateWallSegment(northWallsParent, doorRoot, roomData, x, z, h, Vector2Int.up, tileSize, roomWidth, roomLength, doorWidth, doorHeight);    // North
                    GenerateWallSegment(southWallsParent, doorRoot, roomData, x, z, h, Vector2Int.down, tileSize, roomWidth, roomLength, doorWidth, doorHeight);  // South
                    GenerateWallSegment(eastWallsParent, doorRoot, roomData, x, z, h, Vector2Int.right, tileSize, roomWidth, roomLength, doorWidth, doorHeight); // East
                    GenerateWallSegment(westWallsParent, doorRoot, roomData, x, z, h, Vector2Int.left, tileSize, roomWidth, roomLength, doorWidth, doorHeight);  // West
                }
            }
        }

        // Populate renderers in WallSegment components
        PopulateWallSegmentRenderers(northWallsParent);
        PopulateWallSegmentRenderers(southWallsParent);
        PopulateWallSegmentRenderers(eastWallsParent);
        PopulateWallSegmentRenderers(westWallsParent);
    }

    void PopulateWallSegmentRenderers(GameObject wallParent)
    {
        WallSegment wallSegment = wallParent.GetComponent<WallSegment>();
        if (wallSegment == null)
            return;

        // Get all renderers from this parent and its children
        Renderer[] renderers = wallParent.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            wallSegment.AddMesh(renderer);
        }
    }

    void GenerateWallSegment(GameObject wallRoot, GameObject doorRoot, DungeonRoom roomData, int x, int z, int h, Vector2Int direction, float tileSize, int roomWidth, int roomLength, int doorWidth, int doorHeight)
    {
        int nextX = x + direction.x;
        int nextZ = z + direction.y;

        // Check if neighbor tile is inside this room
        bool neighborInRoom = nextX >= 0 && nextX < roomWidth && nextZ >= 0 && nextZ < roomLength;

        if (neighborInRoom)
        {
            // Both tiles are in same room, no wall needed
            return;
        }

        // Edge is at room boundary
        // Check if this edge should have a door opening or be filled (for secret rooms)
        bool shouldCarveDoor = false;
        bool isSecretNeighbor = false;
        DungeonRoom neighborRoom = null;
        
        if (direction == Vector2Int.up && roomData.north.exists && h < doorHeight)
        {
            neighborRoom = roomData.north.neighbor;
            
            // Check if this tile is within the door width range (centered)
            int midX = roomWidth / 2;
            int halfDoor = doorWidth / 2;
            int doorStart = midX - halfDoor;
            int doorEnd = doorStart + doorWidth;
            
            if (x >= doorStart && x < doorEnd)
            {
                // Only set isSecretNeighbor when we're in the door opening range
                isSecretNeighbor = neighborRoom != null && neighborRoom.roomType == RoomType.Secret;
                
                if (isSecretNeighbor)
                {
                    // For secret rooms, fill with walls instead of carving
                    shouldCarveDoor = false;
                }
                else
                {
                    shouldCarveDoor = true;
                    
                    // Only spawn door once at center height and center position
                    // Don't spawn if current room is secret (other side will handle it as a filled wall)
                    if (h == 0 && x == midX && roomData.roomType != RoomType.Secret)
                    {
                        SpawnDoor(doorRoot, roomData, x, z, h, direction, tileSize);
                    }
                }
            }
        }
        else if (direction == Vector2Int.down && roomData.south.exists && h < doorHeight)
        {
            neighborRoom = roomData.south.neighbor;
            
            // Check if this tile is within the door width range (centered)
            int midX = roomWidth / 2;
            int halfDoor = doorWidth / 2;
            int doorStart = midX - halfDoor;
            int doorEnd = doorStart + doorWidth;
            
            if (x >= doorStart && x < doorEnd)
            {
                // Only set isSecretNeighbor when we're in the door opening range
                isSecretNeighbor = neighborRoom != null && neighborRoom.roomType == RoomType.Secret;
                
                if (isSecretNeighbor)
                {
                    shouldCarveDoor = false;
                }
                else
                {
                    shouldCarveDoor = true;
                    
                    // Only spawn door once at center height and center position
                    // Don't spawn if current room is secret (other side will handle it as a filled wall)
                    if (h == 0 && x == midX && roomData.roomType != RoomType.Secret)
                    {
                        SpawnDoor(doorRoot, roomData, x, z, h, direction, tileSize);
                    }
                }
            }
        }
        else if (direction == Vector2Int.right && roomData.east.exists && h < doorHeight)
        {
            neighborRoom = roomData.east.neighbor;
            
            // Check if this tile is within the door width range (centered)
            int midZ = roomLength / 2;
            int halfDoor = doorWidth / 2;
            int doorStart = midZ - halfDoor;
            int doorEnd = doorStart + doorWidth;
            
            if (z >= doorStart && z < doorEnd)
            {
                // Only set isSecretNeighbor when we're in the door opening range
                isSecretNeighbor = neighborRoom != null && neighborRoom.roomType == RoomType.Secret;
                
                if (isSecretNeighbor)
                {
                    shouldCarveDoor = false;
                }
                else
                {
                    shouldCarveDoor = true;
                    
                    // Only spawn door once at center height and center position
                    // Don't spawn if current room is secret (other side will handle it as a filled wall)
                    if (h == 0 && z == midZ && roomData.roomType != RoomType.Secret)
                    {
                        SpawnDoor(doorRoot, roomData, x, z, h, direction, tileSize);
                    }
                }
            }
        }
        else if (direction == Vector2Int.left && roomData.west.exists && h < doorHeight)
        {
            neighborRoom = roomData.west.neighbor;
            
            // Check if this tile is within the door width range (centered)
            int midZ = roomLength / 2;
            int halfDoor = doorWidth / 2;
            int doorStart = midZ - halfDoor;
            int doorEnd = doorStart + doorWidth;
            
            if (z >= doorStart && z < doorEnd)
            {
                // Only set isSecretNeighbor when we're in the door opening range
                isSecretNeighbor = neighborRoom != null && neighborRoom.roomType == RoomType.Secret;
                
                if (isSecretNeighbor)
                {
                    shouldCarveDoor = false;
                }
                else
                {
                    shouldCarveDoor = true;
                    
                    // Only spawn door once at center height and center position
                    // Don't spawn if current room is secret (other side will handle it as a filled wall)
                    if (h == 0 && z == midZ && roomData.roomType != RoomType.Secret)
                    {
                        SpawnDoor(doorRoot, roomData, x, z, h, direction, tileSize);
                    }
                }
            }
        }

        if (shouldCarveDoor)
        {
            // Skip this wall segment to carve an opening
            return;
        }

        // Place wall block
        
        // Determine parent: regular wall or secret wall
        Transform wallParent = wallRoot.transform;
        if (isSecretNeighbor && neighborRoom != null)
        {
            // Create or get "Secret Wall" parent
            Transform secretWallParent = wallRoot.transform.Find("Secret Wall");
            if (secretWallParent == null)
            {
                GameObject secretWallObj = new GameObject("Secret Wall");
                secretWallObj.transform.SetParent(wallRoot.transform);
                secretWallObj.transform.localPosition = Vector3.zero;
                secretWallParent = secretWallObj.transform;
            }
            wallParent = secretWallParent;
        }
        
        GameObject wall = Instantiate(wallPrefab, wallParent);
        
        // Position at edge center
        float posX = x * tileSize + (tileSize / 2);
        float posZ = z * tileSize + (tileSize / 2);
        
        // Offset to edge based on direction
        if (direction == Vector2Int.up)
            posZ += tileSize / 2;
        else if (direction == Vector2Int.down)
            posZ -= tileSize / 2;
        else if (direction == Vector2Int.right)
            posX += tileSize / 2;
        else if (direction == Vector2Int.left)
            posX -= tileSize / 2;

        wall.transform.localPosition = new Vector3(posX, (h + 0.5f) * tileSize, posZ);

        // Rotate if on east/west edge
        bool isVerticalWall = direction == Vector2Int.up || direction == Vector2Int.down;
        if (!isVerticalWall)
            wall.transform.localRotation = Quaternion.Euler(0, 90, 0);

        wall.layer = LayerMask.NameToLayer("Wall");
        wall.name = "Wall";
        wall.isStatic = true;
        wall.transform.localScale = new Vector3(
            wallPrefab.transform.localScale.x * tileSize,
            wallPrefab.transform.localScale.y * tileSize,
            wallPrefab.transform.localScale.z * tileSize
        );
    }

    void SpawnDoor(GameObject doorRoot, DungeonRoom roomData, int x, int z, int h, Vector2Int direction, float tileSize)
    {
        if (doorPrefab == null)
            return;

        // Get the neighbor room based on direction
        DungeonRoom neighborRoom = null;
        if (direction == Vector2Int.up && roomData.north.exists)
            neighborRoom = roomData.north.neighbor;
        else if (direction == Vector2Int.down && roomData.south.exists)
            neighborRoom = roomData.south.neighbor;
        else if (direction == Vector2Int.right && roomData.east.exists)
            neighborRoom = roomData.east.neighbor;
        else if (direction == Vector2Int.left && roomData.west.exists)
            neighborRoom = roomData.west.neighbor;

        if (neighborRoom == null)
            return;

        // Position at edge center
        float posX = x * tileSize + (tileSize / 2);
        float posZ = z * tileSize + (tileSize / 2);
        
        // Offset to edge based on direction
        if (direction == Vector2Int.up)
            posZ += tileSize / 2;
        else if (direction == Vector2Int.down)
            posZ -= tileSize / 2;
        else if (direction == Vector2Int.right)
            posX += tileSize / 2;
        else if (direction == Vector2Int.left)
            posX -= tileSize / 2;

        Vector3 doorPosition = new Vector3(posX, (h + 0.5f) * tileSize, posZ);
        
        // Set rotation based on direction
        Quaternion doorRotation = Quaternion.identity;
        if (direction == Vector2Int.right || direction == Vector2Int.left)
            doorRotation = Quaternion.Euler(0, 90, 0);

        // Use CreateDoor to properly initialize the door with room connections
        CreateDoor(roomData, neighborRoom, doorPosition, doorRotation);
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