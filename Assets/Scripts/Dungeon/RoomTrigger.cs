using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public RoomNode RoomNode;
    public DungeonVisibilityManager visibilityManager;

    private void OnTriggerEnter(Collider other)
    {
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