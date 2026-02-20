using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public DungeonRoom roomData;
    public DungeonVisibilityController visibilityController;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            visibilityController.EnterRoom(roomData);
        }
    }
}