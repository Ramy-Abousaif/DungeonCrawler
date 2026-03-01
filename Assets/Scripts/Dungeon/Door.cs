using UnityEngine;

public class Door : Interactable
{
    public DungeonRoom RoomA;
    public DungeonRoom RoomB;
    private Animator anim;

    private bool isLocked;
    private bool connectionLocked;
    private bool combatLocked;

    [SerializeField] private Material normalMat;
    [SerializeField] private Material bossMat;
    [SerializeField] private Material treasureMat;
    [SerializeField] private Material shopMat;
    [SerializeField] private Material secretMat;
    [SerializeField] private Material startMat;

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

        ApplyDoorVisual();
    
        UpdateLockState();
    }

    void ApplyDoorVisual()
    {
        DungeonRoom higherPriorityRoom =
            GetRoomPriority(RoomA.roomType) >= GetRoomPriority(RoomB.roomType)
            ? RoomA
            : RoomB;

        foreach(Renderer r in transform.parent.GetComponentsInChildren<Renderer>())
        {
            SetDoorMaterialForRoomType(higherPriorityRoom.roomType, r);   
        }
    }

    void SetDoorMaterialForRoomType(RoomType type, Renderer renderer)
    {
        switch (type)
        {
            case RoomType.Boss:
                renderer.material = bossMat;
                break;

            case RoomType.Treasure:
                renderer.material = treasureMat;
                break;

            case RoomType.Shop:
                renderer.material = shopMat;
                break;

            case RoomType.Secret:
                renderer.material = secretMat;
                break;

            case RoomType.Start:
                renderer.material = startMat;
                break;

            default:
                renderer.material = normalMat;
                break;
        }
    }

    int GetRoomPriority(RoomType type)
    {
        switch (type)
        {
            case RoomType.Boss:     return 100;
            case RoomType.Treasure: return 80;
            case RoomType.Shop:     return 60;
            case RoomType.Secret:   return 50;
            case RoomType.Normal:   return 40;
            case RoomType.Start:    return 10;
            default:                return 0;
        }
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