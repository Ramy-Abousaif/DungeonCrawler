using UnityEngine;

public class ItemPickup: Interactable
{
    public Item item;
    public Items itemDrop;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        item = AssignItem(itemDrop);
    }

    public override void OnInteract(PhysicsBasedCharacterController player)
    {
        PhysicsBasedCharacterController p = player.GetComponent<PhysicsBasedCharacterController>();
        AddItem(p);
        // Call OnPickup only for the item that was just added/updated,
        // instead of calling all items' OnPickup (which would re-apply other items).
        foreach (ItemList il in p.items)
        {
            if (il.name == item.GiveName())
            {
                il.item.OnPickup(p, il.stacks);
                break;
            }
        }
        Destroy(this.gameObject);
    }

    public void AddItem(PhysicsBasedCharacterController player)
    {
        foreach(ItemList i in player.items)
        {
            if(i.name == item.GiveName())
            {
                i.stacks += 1;
                return;
            }
        }
        player.items.Add(new ItemList(item, item.GiveName(), 1));
    }

    public Item AssignItem(Items itemToAssign)
    {
        switch (itemToAssign)
        {
            case Items.HealingItem:
                return new HealingItem();
            case Items.FireDamageItem:
                return new FireDamageItem();
            case Items.HealingAreaItem:
                return new HealingAreaItem();
            case Items.AttackSpeedItem:
                return new AttackSpeedItem();
            case Items.AttackRangeItem:
                return new AttackRangeItem();
            case Items.BleedItem:
                return new BleedItem();
            case Items.MovementSpeedItem:
                return new MovementSpeedItem();
            case Items.ExtraJumpItem:
                return new ExtraJumpItem();
            default:
                Debug.Log("No item assigned");
                return new BlankItem();
        }
    }
}

public enum Items
{
    BlankItem,
    HealingItem,
    FireDamageItem,
    HealingAreaItem,
    AttackSpeedItem,
    AttackRangeItem,
    BleedItem,
    MovementSpeedItem,
    ExtraJumpItem
}