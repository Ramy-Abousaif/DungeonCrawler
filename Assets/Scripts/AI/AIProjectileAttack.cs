using UnityEngine;

public class AIProjectileAttack : AIAttack
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float projectileSpeed = 25f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private LayerMask projectileHitMask;

    [Header("Aim")]
    [SerializeField] private float aimHeightOffset = 1.0f;

    [Header("Leading")]
    [SerializeField] private bool useLeading = true;

    public override bool CanUse(AIBase ai)
    {
        if (!IsReady) return false;
        if (projectilePrefab == null) return false;
        if (shootPoint == null) return false;
        if (ai.Target == null) return false;
        return true;
    }

    public override void Execute(AIBase ai)
    {
        Vector3 shooterPos = shootPoint.position;
        Vector3 targetPos = ai.Target.position + Vector3.up * aimHeightOffset;

        Vector3 dir;

        if (useLeading && ai.TryGetTargetVelocity(out Vector3 targetVel))
        {
            dir = AIBase.GetLeadDirection(shooterPos, targetPos, targetVel, projectileSpeed);
        }
        else
        {
            dir = (targetPos - shooterPos).normalized;
        }

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        GameObject obj = PoolManager.Instance.Spawn(projectilePrefab, shooterPos, rot);

        if (obj.TryGetComponent(out Projectile proj))
        {
            proj.SetOwner(ai.SelfCharacter);
            proj.SetDamage(projectileDamage);
            proj.SetHitMask(projectileHitMask);
            proj.SetRange(ai.AttackRange);
        }

        if (obj.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = dir * projectileSpeed;
        }

        TriggerCooldown();
    }
}