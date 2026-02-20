using UnityEngine;

public class DungeonVisibilityController : MonoBehaviour
{
    public DungeonGenerator dungeonGenerator;

    private DungeonRoom currentRoom;

    public void EnterRoom(DungeonRoom room)
    {
        currentRoom = room;
        room.visited = true;

        UpdateRoomStates();
    }

    void UpdateRoomStates()
    {
        foreach (var room in dungeonGenerator.GetAllRooms())
        {
            RoomVisualController visual =
                room.spawnedObject.GetComponent<RoomVisualController>();

            if (room == currentRoom)
            {
                visual.SetState(RoomVisualState.Active);
            }
            else if (AreAdjacent(currentRoom, room))
            {
                visual.SetState(RoomVisualState.Adjacent);
            }
            else if (room.visited)
            {
                visual.SetState(RoomVisualState.Dark);
            }
            else
            {
                visual.SetState(RoomVisualState.Hidden);
            }
        }
    }

    bool AreAdjacent(DungeonRoom a, DungeonRoom b)
    {
        Vector2Int delta = b.gridPosition - a.gridPosition;

        if (delta == Vector2Int.up)
            return a.north.exists;
        if (delta == Vector2Int.down)
            return a.south.exists;
        if (delta == Vector2Int.left)
            return a.west.exists;
        if (delta == Vector2Int.right)
            return a.east.exists;

        return false;
    }
}