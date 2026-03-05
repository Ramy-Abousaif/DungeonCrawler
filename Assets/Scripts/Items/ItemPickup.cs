using UnityEngine;

public class ItemPickup : Interactable
{
    [SerializeField] private ItemData itemData;

    private void Start()
    {
        if (itemData == null)
        {
            Debug.LogWarning("ItemPickup has no ItemData assigned!", gameObject);
            Destroy(gameObject);
        }
    }

    public override void OnInteract(PhysicsBasedCharacterController player)
    {
        AddItemToPlayer(player);
        Destroy(gameObject);
    }

    private void AddItemToPlayer(PhysicsBasedCharacterController player)
    {
        // Check if player already has this item
        foreach (ItemList itemSlot in player.items)
        {
            if (itemSlot.GetName() == itemData.ItemName)
            {
                // Stack item
                if (itemData.CanStack)
                {
                    itemSlot.stacks = Mathf.Min(itemSlot.stacks + 1, itemData.MaxStacks);
                    itemSlot.item.ApplyPickupEffects(player);
                    return;
                }
                else
                {
                    // Item doesn't stack, just ignore
                    return;
                }
            }
        }

        ItemList newItemSlot = new ItemList(itemData, 1);
        player.items.Add(newItemSlot);
        newItemSlot.item.ApplyPickupEffects(player);
    }

    public void SetItem(ItemData data)
    {
        itemData = data;
    }

    public ItemData GetItemData()
    {
        return itemData;
    }
}