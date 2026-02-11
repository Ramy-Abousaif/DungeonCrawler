using UnityEngine;
public class BleedEffect : StatusEffect
{
    private float damagePerTick;

    public BleedEffect(float damagePerTick, float interval)
    {
        this.damagePerTick = damagePerTick;
        this.tickInterval = interval;
    }

    protected override void OnTick()
    {
        if (target == null) return;

        float damage = damagePerTick * stacks;
        target.TakeDamage(damage);
    }
}
