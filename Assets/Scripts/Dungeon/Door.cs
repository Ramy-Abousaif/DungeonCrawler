using UnityEngine;

public class Door : Interactable
{
    public RoomNode RoomA;
    public RoomNode RoomB;

    private bool isLocked;

    public void Initialize(RoomNode a, RoomNode b, bool locked)
    {
        RoomA = a;
        RoomB = b;
        SetLocked(locked);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        // TODO:
        // - Change material
        // - Enable collider
        // - Play animation
    }

    public RoomNode GetOtherRoom(RoomNode current)
    {
        return current == RoomA ? RoomB : RoomA;
    }

    public override void OnInteract(PhysicsBasedCharacterController player)
    {
        if(!isLocked)
        {
            // TODO: Play anim rather than destroy
            isLocked = false;
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("CANT OPEN DOOR AS IT IS LOCKED");
        }
    }
}