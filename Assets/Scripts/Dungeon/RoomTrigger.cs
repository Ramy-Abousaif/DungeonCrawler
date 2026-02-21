using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public DungeonRoom roomData;
    public DungeonVisibilityController visibilityController;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        visibilityController.EnterRoom(roomData);
        if (!roomData.IsCleared)
        {
            var combat = roomData.spawnedObject.GetComponent<RoomCombatController>();
            if (combat != null)
            {
                combat.Initialize(roomData);
                combat.StartCombat();
            }
        }
    }
}