using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public RoomNode RoomNode;
    public DungeonVisibilityManager visibilityManager;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Entering room {RoomNode.Id}, cleared = {RoomNode.IsCleared}");
        if (!other.CompareTag("Player"))
            return;

        visibilityManager.SetCurrentRoom(RoomNode);

        if (!RoomNode.IsCleared)
        {
            var combat = RoomNode.RoomObject.GetComponent<RoomCombatController>();
            if (combat != null)
            {
                combat.Initialize(RoomNode);
                combat.StartCombat();
            }
        }
    }
}