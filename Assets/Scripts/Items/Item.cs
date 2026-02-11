using UnityEngine;

[System.Serializable]
public abstract class Item
{
    public abstract string GiveName();

    public abstract string GiveDescription();

    public virtual void Update(PhysicsBasedCharacterController player, int stacks)
    {
        
    }

    public virtual void OnHit(PhysicsBasedCharacterController player, Enemy enemy, int stacks)
    {
        
    }

    public virtual void OnJump(PhysicsBasedCharacterController player, int stacks)
    {
        
    }

    public virtual void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        
    }
}

public class BlankItem : Item
{
    public override string GiveName()
    {
        return "Blank Item";
    }

    public override string GiveDescription()
    {
        return "This is a blank item with no effects.";
    }
}

public class HealingItem : Item
{
    public override string GiveName()
    {
        return "Healing Item";
    }

    public override string GiveDescription()
    {
        return "Heals the player over time.";
    }

    public override void Update(PhysicsBasedCharacterController player, int stacks)
    {
        player.Heal(3 * (2 + stacks));
    }
}

public class FireDamageItem : Item
{
    public override string GiveName()
    {
        return "Fire Damage Item";
    }

    public override string GiveDescription()
    {
        return "Deals extra damage on hit.";
    }

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        for (int i = 0; i < player.abilities.abilities.Length; i++)
        {
            player.abilities.abilities[i].currentAbilityDamage = player.abilities.abilities[i].baseAbilityDamage + (10f * stacks);
        }
    }
}

public class HealingAreaItem: Item
{
    private float internalCooldown;
    private GameObject effect;
    public override string GiveName()
    {
        return "Healing Area Item";
    }

    public override string GiveDescription()
    {
        return "Creates a healing area upon jumping.";
    }

    public override void Update(PhysicsBasedCharacterController player, int stacks)
    {
        internalCooldown -= 1;
    }

    public override void OnJump(PhysicsBasedCharacterController player, int stacks)
    {
        if(internalCooldown <= 0)
        {
            if(effect == null) 
                effect = (GameObject)Resources.Load("Item Effects/HealingArea", typeof(GameObject));
            
            PoolManager.Instance.Spawn(effect, new Vector3(player.transform.position.x, player.transform.position.y - 2, player.transform.position.z), Quaternion.Euler(Vector3.zero));
            internalCooldown = 10;
        }
    }
}

public class AttackSpeedItem: Item
{
    public override string GiveName()
    {
        return "Attack Speed Item";
    }

    public override string GiveDescription()
    {
        return "Increases attack speed.";
    }

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        for (int i = 0; i < player.abilities.abilities.Length; i++)
        {
            if(!player.abilities.abilities[i].affectedByAttackSpeed)
                continue;

            player.abilities.abilities[i].currentAbilitySpeed = player.abilities.abilities[i].baseAbilitySpeed + (1f * stacks);
            player.Anim.SetFloat("AbilitySpeed" + (i + 1), player.abilities.abilities[i].currentAbilitySpeed);
        }
    }
}

public class AttackRangeItem: Item
{
    public override string GiveName()
    {
        return "Attack Range Item";
    }

    public override string GiveDescription()
    {
        return "Increases attack range.";
    }

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        for (int i = 0; i < player.abilities.abilities.Length; i++)
        {
            player.abilities.abilities[i].currentAbilityRange = player.abilities.abilities[i].baseAbilityRange + (1f * stacks);
        }
    }
}

public class BleedItem : Item
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float damagePerTick = 2f;
    [SerializeField] private float tickInterval = 1f;

    private float baseInterval = 2f;

    public override string GiveName()
    {
        tickInterval = baseInterval;
        return "Bleed Item";
    }

    public override string GiveDescription()
    {
        return "Applies bleeding on hit, dealing damage over time.";
    }

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        float k = 10f;
        float rateBonus = stacks / (stacks + k);

        // increase duration and interval based on stacks if needed
        if(stacks > 1)
        {
            duration += 0.5f * stacks;
            // calculate diminished returns for tick interval reduction
            tickInterval = baseInterval / (1f + rateBonus);
        }
    }

    public override void OnHit(PhysicsBasedCharacterController player, Enemy enemy, int stacks)
    {
        if (enemy == null) return;

        BleedEffect bleed = new BleedEffect(damagePerTick, tickInterval);
        enemy.ApplyStatusEffect(bleed, stacks, duration);
    }
}

public class MovementSpeedItem: Item
{
    public override string GiveName()
    {
        return "Movement Speed Item";
    }

    public override string GiveDescription()
    {
        return "Increases movement speed.";
    }

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        player.CurrentMaxSpeed = player.BaseMaxSpeed + (1f * stacks);
        player.Anim.SetFloat("MovementSpeed", (player.CurrentMaxSpeed / player.BaseMaxSpeed));
    }
}

public class ExtraJumpItem : Item
{
    public override string GiveName()
    {
        return "Extra Jump Item";
    }

    public override string GiveDescription()
    {
        return "Grants additional mid-air jumps.";
    }

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        player.SetExtraJumps(stacks);
    }
}