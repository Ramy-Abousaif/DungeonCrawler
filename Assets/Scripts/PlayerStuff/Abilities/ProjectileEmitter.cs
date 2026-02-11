using Unity.VisualScripting;
using UnityEngine;

public class ProjectileEmitter : AbilityEmitter
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float baseLaunchForce = 25f;
    [SerializeField] private float baseRange = 10f;
    [SerializeField] private float spin = 20f;
    
    protected override void PerformFire(Abilities.Ability ability)
    {
        GameObject proj = PoolManager.Instance.Spawn(projectilePrefab, firePoint.position, firePoint.rotation);

        if (!proj.TryGetComponent(out Rigidbody rb)) return;

        rb.linearVelocity = Vector3.zero;

        float force = baseLaunchForce;
        Vector3 dir = firePoint.transform.forward;

        rb.AddForce(dir * force, ForceMode.VelocityChange);
        rb.AddTorque(firePoint.up * spin, ForceMode.VelocityChange);

        if (proj.TryGetComponent(out Projectile projectile))
        {
            projectile.SetOwner(player);
            projectile.SetDamage(ability.currentAbilityDamage);
            projectile.SetAbilityIndex(abilityIndex);
            projectile.SetRange(ability.currentAbilityRange * baseRange);
        }
    }
}
