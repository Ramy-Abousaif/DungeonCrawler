using UnityEngine;

/// <summary>
/// ScriptableObject definition for items. This decouples item data from logic,
/// making it easy to create and balance items in the inspector.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string itemName;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Rarity & Categorization")]
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [SerializeField] private ItemCategory category = ItemCategory.Buff;

    [Header("Drop Settings")]
    [SerializeField] private bool canStack = true;
    [SerializeField] private int maxStacks = 99;

    [Header("Effects")]
    [Tooltip("Inline item effects. Use the custom inspector Add Effect dropdown to create entries.")]
    [SerializeReference] private ItemEffect[] effects = new ItemEffect[0];

    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemRarity Rarity => rarity;
    public ItemCategory Category => category;
    public bool CanStack => canStack;
    public int MaxStacks => maxStacks;
    public ItemEffect[] Effects => effects;

    public void ApplyEffects(PhysicsBasedCharacterController player, int stacks, string effectType)
    {
        if (effects == null)
            return;

        foreach (ItemEffect effect in effects)
        {
            if (effect == null)
                continue;

            switch (effectType)
            {
                case "Pickup":
                    effect.OnPickup(player, stacks);
                    break;
                case "Hit":
                    effect.OnHit(player, null, stacks);
                    break;
                case "Jump":
                    effect.OnJump(player, stacks);
                    break;
                case "Update":
                    effect.Update(player, stacks);
                    break;
            }
        }
    }
}
