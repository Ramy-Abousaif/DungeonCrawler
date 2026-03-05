using UnityEngine;

/// <summary>
/// Represents a single item slot in the player's inventory
/// </summary>
[System.Serializable]
public class ItemList
{
    public Item item;

    public int stacks
    {
        get => item != null ? item.Stacks : 0;
        set
        {
            if (item != null)
                item.SetStacks(value);
        }
    }

    public ItemList(ItemData itemData, int stackCount = 1)
    {
        item = new Item(itemData, stackCount);
    }

    public string GetName() => item.GetName();
    public ItemRarity GetRarity() => item.GetRarity();
}