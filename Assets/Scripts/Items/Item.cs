using UnityEngine;

/// <summary>
/// Instance of an item that the player has. Holds reference to ItemData and stack count.
/// This is much simpler now that effects are data-driven.
/// </summary>
[System.Serializable]
public class Item
{
    public ItemData data;
    [SerializeField] private int stacks = 1;
    public int Stacks => stacks;

    public Item(ItemData itemData, int stackCount = 1)
    {
        data = itemData;
        SetStacks(stackCount);
    }

    public void SetStacks(int stackCount)
    {
        if (data == null)
        {
            stacks = Mathf.Max(0, stackCount);
            return;
        }

        stacks = Mathf.Clamp(stackCount, 1, Mathf.Max(1, data.MaxStacks));
    }

    public void AddStacks(int amount = 1)
    {
        SetStacks(stacks + amount);
    }

    public string GetName() => data.ItemName;
    public string GetDescription() => data.Description;
    public ItemRarity GetRarity() => data.Rarity;
    public Sprite GetIcon() => data.Icon;

    public void ApplyPickupEffects(PhysicsBasedCharacterController player)
    {
        if (data == null)
            return;

        data.ApplyEffects(player, stacks, "Pickup");
    }

    public void ApplyHitEffects(PhysicsBasedCharacterController player, Enemy enemy)
    {
        if (data == null || data.Effects == null)
            return;

        foreach (ItemEffect effect in data.Effects)
        {
            if (effect == null)
                continue;

            effect.OnHit(player, enemy, stacks);
        }
    }

    public void ApplyJumpEffects(PhysicsBasedCharacterController player)
    {
        if (data == null)
            return;

        data.ApplyEffects(player, stacks, "Jump");
    }

    public void ApplyUpdateEffects(PhysicsBasedCharacterController player)
    {
        if (data == null)
            return;

        data.ApplyEffects(player, stacks, "Update");
    }
}

/// <summary>
/// OLD CODE REMOVED - Items are now data-driven using ItemData ScriptableObjects.
/// Create items in the inspector using Create > Items > Item Data
/// 
/// All item effects are defined in ItemEffect classes and can be mixed and matched
/// on ItemData assets without writing any code.
/// </summary>