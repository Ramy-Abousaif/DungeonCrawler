using Unity.Mathematics;
using UnityEngine;

public class MeleeEmitter : AbilityEmitter
{
    [Header("Melee")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float radiusBaseMultiplier = 2f;

    protected override void PerformFire(Abilities.Ability ability)
    {
        if (firePoint == null)
            return;

        float radius = ability.currentAbilityRange;
        Vector3 center = firePoint.position;

        if(ability.abilityEmitter.abilityEffect != null)
        {
            GameObject fx = PoolManager.Instance.Spawn(ability.abilityEmitter.abilityEffect, center, Quaternion.identity);
            fx.transform.localScale = new Vector3(radius, radius, radius);
        }
        else
        {
            Debug.Log("No Effect Prefab");
        }

        Collider[] hits = Physics.OverlapSphere(center, radius * radiusBaseMultiplier, enemyMask);

        foreach (Collider col in hits)
        {
            if (col.transform.root.TryGetComponent(out Enemy enemy))
            {
                player.abilities.OnHit(enemy, abilityIndex, true);
                player.CallItemOnHit(enemy);
            }
        }
    }
}