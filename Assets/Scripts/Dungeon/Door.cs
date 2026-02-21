using UnityEngine;

public class Door : Interactable
{
    public DungeonRoom RoomA;
    public DungeonRoom RoomB;
    private Animator anim;

    private bool isLocked;
    private bool connectionLocked;
    private bool combatLocked;

    void Awake()
    {
        anim = GetComponentInParent<Animator>();
    }

    public void Initialize(DungeonRoom a, DungeonRoom b, bool locked)
    {
        RoomA = a;
        RoomB = b;

        connectionLocked = locked;
        combatLocked = false;

        UpdateLockState();
    }

    public void SetCombatLocked(bool locked)
    {
        combatLocked = locked;
        UpdateLockState();
    }

    public void SetConnectionLocked(bool locked)
    {
        connectionLocked = locked;
        UpdateLockState();
    }

    void UpdateLockState()
    {
        isLocked = connectionLocked || combatLocked;
    }

    public void PlayDoorAnim(bool open)
    {
        anim.SetBool("Open", open);
    }

    public DungeonRoom GetOtherRoom(DungeonRoom current)
    {
        return current == RoomA ? RoomB : RoomA;
    }

    public override void OnInteract(PhysicsBasedCharacterController player)
    {
        if(!isLocked)
        {
            isLocked = false;
            PlayDoorAnim(true);
        }
        else
        {
            Debug.Log("CANT OPEN DOOR AS IT IS LOCKED");
        }
    }
}