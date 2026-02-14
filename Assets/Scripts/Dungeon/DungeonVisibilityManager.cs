using UnityEngine;
using System.Linq;

public class DungeonVisibilityManager : MonoBehaviour
{
    public DungeonGenerator generator;
    public RoomNode CurrentRoom;

    public void SetCurrentRoom(RoomNode newRoom)
    {
        CurrentRoom = newRoom;
        newRoom.Visited = true;

        UpdateRoomStates();
    }

    void UpdateRoomStates()
    {
        foreach (var room in generator.Graph.Rooms)
        {
            if (room == CurrentRoom)
            {
                room.VisualController.SetState(RoomVisualState.Active);
            }
            else if (CurrentRoom.Connections.Any(c => c.Target == room))
            {
                room.VisualController.SetState(RoomVisualState.Adjacent);
            }
            else if (room.Visited)
            {
                room.VisualController.SetState(RoomVisualState.Dark);
            }
            else
            {
                room.VisualController.SetState(RoomVisualState.Hidden);
            }
        }
    }
}