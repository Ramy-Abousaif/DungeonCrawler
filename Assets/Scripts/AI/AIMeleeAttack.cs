using UnityEngine;

public class AIMeleeAttack : AIAttack
{
    [Header("Melee Settings")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float range = 2f;
    [SerializeField] private float hitAngle = 60f; // Cone angle
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private bool useAnimationTrigger = true;
    [SerializeField] private string animTriggerName = "Attack";

    public override bool CanUse(AIBase ai)
    {
        if (!IsReady) return false;
        if (ai.Target == null) return false;

        float dist = Vector3.Distance(ai.transform.position, ai.Target.position);
        return dist <= range;
    }

    public override void Execute(AIBase ai)
    {
        if (ai.Target == null) return;

        // Optional animation trigger
        if (useAnimationTrigger && ai.Anim != null)
        {
            ai.Anim.SetTrigger(animTriggerName);
        }

        // Deal damage to targets within range and angle
        Collider[] hits = Physics.OverlapSphere(ai.transform.position, range, hitMask);
        foreach (Collider col in hits)
        {
            if (col.transform == ai.Target) // Could do team/faction checks here
            {
                Vector3 toTarget = (col.transform.position - ai.transform.position).normalized;
                float angle = Vector3.Angle(ai.transform.forward, toTarget);
                if (angle <= hitAngle * 0.5f)
                {
                    if (col.TryGetComponent(out Character targetChar))
                    {
                        targetChar.TakeDamage(damage, true);
                    }
                }
            }
        }

        TriggerCooldown();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize the attack cone
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        Vector3 forward = transform.forward;
        Quaternion leftRot = Quaternion.Euler(0, -hitAngle * 0.5f, 0);
        Quaternion rightRot = Quaternion.Euler(0, hitAngle * 0.5f, 0);

        Gizmos.DrawLine(transform.position, transform.position + leftRot * forward * range);
        Gizmos.DrawLine(transform.position, transform.position + rightRot * forward * range);
    }
#endif
}
