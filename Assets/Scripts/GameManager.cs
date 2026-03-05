using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private PhysicsBasedCharacterController player;
    [SerializeField] private GameObject shopkeeperPrefab;
    [SerializeField] private DungeonGenerator dungeonGenerator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        dungeonGenerator.GenerateDungeon();
        SetPlayerPosition();
        InitializeShopKeepers();
    }

    private void SetPlayerPosition()
    {
        player = FindFirstObjectByType<PhysicsBasedCharacterController>();
        DungeonRoom start = dungeonGenerator.GetRoomsByType(RoomType.Start)[0];
        Vector3 offset = new Vector3(start.width * dungeonGenerator.tileSize * 0.5f,
                            2.5f,
                            start.length * dungeonGenerator.tileSize * 0.5f);
        Vector3 newPos = start.worldPosition + offset;
        player.transform.position = newPos;
    }

    private void InitializeShopKeepers()
    {
        List<DungeonRoom> shopRooms = dungeonGenerator.GetRoomsByType(RoomType.Shop);
        foreach(DungeonRoom room in shopRooms)
        {
            Vector3 spawnPos = room.worldPosition + new Vector3(room.width * dungeonGenerator.tileSize * 0.5f, 1.5f, room.length * dungeonGenerator.tileSize * 0.5f);
            Quaternion spawnRot = Quaternion.Euler(0f, 180f, 0f);

            if (room.north.exists)
                spawnRot = Quaternion.Euler(0f, 0, 0f);
            else if (room.south.exists)
                spawnRot = Quaternion.Euler(0f, 180f, 0f);
            else if (room.east.exists)
                spawnRot = Quaternion.Euler(0f, 90f, 0f);
            else if (room.west.exists)
                spawnRot = Quaternion.Euler(0f, 270f, 0f);

            PoolManager.Instance.Spawn(shopkeeperPrefab, spawnPos, spawnRot);
        }
    }
}
