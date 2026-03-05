using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class ItemEffect
{
    public virtual void OnPickup(PhysicsBasedCharacterController player, int stacks) { }
    public virtual void OnHit(PhysicsBasedCharacterController player, Enemy enemy, int stacks) { }
    public virtual void OnJump(PhysicsBasedCharacterController player, int stacks) { }
    public virtual void Update(PhysicsBasedCharacterController player, int stacks) { }
}

[System.Serializable]
public class DamageEffect : ItemEffect
{
    [SerializeField] private float damagePerStack = 10f;

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        float totalDamage = damagePerStack * stacks;
        for (int i = 0; i < player.abilities.abilities.Length; i++)
        {
            player.abilities.abilities[i].currentAbilityDamage = 
                player.abilities.abilities[i].baseAbilityDamage + totalDamage;
        }
    }
}

[System.Serializable]
public class AttackSpeedEffect : ItemEffect
{
    [SerializeField] private float speedPerStack = 1f;

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        float totalSpeed = speedPerStack * stacks;
        for (int i = 0; i < player.abilities.abilities.Length; i++)
        {
            if (!player.abilities.abilities[i].affectedByAttackSpeed)
                continue;

            player.abilities.abilities[i].currentAbilitySpeed = 
                player.abilities.abilities[i].baseAbilitySpeed + totalSpeed;
            player.Anim.SetFloat("AbilitySpeed" + (i + 1), player.abilities.abilities[i].currentAbilitySpeed);
        }
    }
}

[System.Serializable]
public class RangeEffect : ItemEffect
{
    [SerializeField] private float rangePerStack = 1f;

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        float totalRange = rangePerStack * stacks;
        for (int i = 0; i < player.abilities.abilities.Length; i++)
        {
            player.abilities.abilities[i].currentAbilityRange = 
                player.abilities.abilities[i].baseAbilityRange + totalRange;
        }
    }
}

[System.Serializable]
public class MovementSpeedEffect : ItemEffect
{
    [SerializeField] private float speedPerStack = 1f;

    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        float totalSpeed = speedPerStack * stacks;
        player.CurrentMaxSpeed = player.BaseMaxSpeed + totalSpeed;
        player.Anim.SetFloat("MovementSpeed", (player.CurrentMaxSpeed / player.BaseMaxSpeed));
    }
}

[System.Serializable]
public class HealingEffect : ItemEffect
{
    [SerializeField] private float healPerTickPerStack = 3f;
    [SerializeField] private float baseMultiplier = 2f;

    public override void Update(PhysicsBasedCharacterController player, int stacks)
    {
        float totalHeal = healPerTickPerStack * (baseMultiplier + stacks);
        player.Heal(totalHeal);
    }
}

[System.Serializable]
public class BleedItemEffect : ItemEffect
{
    [SerializeField] private float baseDuration = 5f;
    [SerializeField] private float additionalDurationPerStack = 0.5f;
    [SerializeField] private float damagePerTick = 2f;
    [SerializeField] private float baseTickInterval = 2f;
    [SerializeField] private float diminishingFactor = 10f;
    [SerializeField] private bool useDiminishingReturns = true;

    public override void OnHit(PhysicsBasedCharacterController player, Enemy enemy, int stacks)
    {
        if (enemy == null)
            return;

        int safeStacks = Mathf.Max(1, stacks);

        float tickInterval = baseTickInterval;
        if (useDiminishingReturns)
        {
            float rateBonus = safeStacks / (safeStacks + diminishingFactor);
            tickInterval = baseTickInterval / (1f + rateBonus);
        }

        float duration = baseDuration;
        if (safeStacks > 1)
            duration += additionalDurationPerStack * safeStacks;

        BleedEffect bleed = new BleedEffect(damagePerTick, tickInterval);
        enemy.ApplyStatusEffect(bleed, safeStacks, duration);
    }
}

[System.Serializable]
public class HealingAreaItemEffect : ItemEffect
{
    [SerializeField] private GameObject healingAreaPrefab;
    [SerializeField] private float yOffset = -2f;
    [SerializeField] private float cooldownSeconds = 10f;
    [SerializeField] private bool reduceCooldownWithStacks;
    [SerializeField] private float cooldownReductionPerStack = 0.25f;
    [SerializeField] private float minimumCooldown = 1f;

    private readonly Dictionary<int, float> nextCastTimesByPlayerId = new Dictionary<int, float>();

    public override void OnJump(PhysicsBasedCharacterController player, int stacks)
    {
        if (player == null)
            return;

        int playerId = player.GetInstanceID();
        float now = Time.time;

        float effectiveCooldown = cooldownSeconds;
        if (reduceCooldownWithStacks && stacks > 1)
        {
            effectiveCooldown = Mathf.Max(
                minimumCooldown,
                cooldownSeconds - cooldownReductionPerStack * (stacks - 1)
            );
        }

        if (nextCastTimesByPlayerId.TryGetValue(playerId, out float nextCastTime) && now < nextCastTime)
            return;

        GameObject prefab = healingAreaPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"HealingAreaItemEffect could not load prefab");
            return;
        }

        if(!player.IsGrounded())
            return;

        Vector3 spawnPosition = player.transform.position + new Vector3(0f, yOffset, 0f);
        if (PoolManager.Instance != null)
            PoolManager.Instance.Spawn(prefab, spawnPosition, Quaternion.identity);
        else
            Object.Instantiate(prefab, spawnPosition, Quaternion.identity);

        nextCastTimesByPlayerId[playerId] = now + effectiveCooldown;
    }
}

[System.Serializable]
public class ExtraJumpEffect : ItemEffect
{
    public override void OnPickup(PhysicsBasedCharacterController player, int stacks)
    {
        player.SetExtraJumps(stacks);
    }
}
