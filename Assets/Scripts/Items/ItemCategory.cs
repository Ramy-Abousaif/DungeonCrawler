using System;

[Flags]
public enum ItemCategory
{
    Damage = 1 << 0,        // Damage-based items
    Utility = 1 << 1,       // Utility items (movement, jumps, etc.)
    Healing = 1 << 2,       // Healing items
    StatusEffect = 1 << 3,  // Items that apply status effects (bleed, fire, etc.)
    Buff = 1 << 4,          // Stat buff items
    All = ~0                // All categories
}
