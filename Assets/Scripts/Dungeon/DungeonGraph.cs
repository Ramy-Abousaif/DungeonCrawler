using System.Collections.Generic;

public class DungeonGraph
{
    public List<RoomNode> Rooms = new List<RoomNode>();

    public RoomNode GetStartRoom()
    {
        return Rooms.Find(r => r.Type == RoomType.Start);
    }
}