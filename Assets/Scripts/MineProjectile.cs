using System.Collections;
using UnityEngine;

public class MineProjectile : Projectile
{
    [Header("Mine")]
    [SerializeField] private float lifeTime = 8f;
    [SerializeField] private float mineRadius = 2.5f;

    [Header("Explosion")]
    [SerializeField] private float explosionDamage = 25f;
    [SerializeField] private LayerMask explosionMask;
    [SerializeField] private GameObject explosionFX;
    [SerializeField] private float explosionRadiusVisualMult = 1f;

    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private float minSpeedToConsiderAirborne = 0.2f;

    private bool armed;
    private bool detonated;
    private SphereCollider triggerCol;
    private Coroutine lifeRoutine;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();

        // Trigger radius (for enemy proximity)
        triggerCol = GetComponent<SphereCollider>();
        if (triggerCol == null)
            triggerCol = gameObject.AddComponent<SphereCollider>();

        triggerCol.isTrigger = true;
        triggerCol.radius = mineRadius;
    }

    public override void OnSpawned()
    {
        base.OnSpawned();

        armed = false;
        detonated = false;

        if (lifeRoutine != null) StopCoroutine(lifeRoutine);
        lifeRoutine = StartCoroutine(LifeRoutine());
    }

    protected override void Update()
    {
        if (detonated) return;

        if (!armed && IsGrounded())
            Arm();
    }

    private void Arm()
    {
        armed = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        Detonate();
    }

    private void Detonate()
    {
        if (detonated) return;
        detonated = true;

        if (explosionFX != null)
        {
            GameObject explosionGO = PoolManager.Instance.Spawn(explosionFX, transform.position, Quaternion.identity);
            explosionGO.transform.localScale = new Vector3(mineRadius * explosionRadiusVisualMult, mineRadius * explosionRadiusVisualMult, mineRadius * explosionRadiusVisualMult);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, mineRadius, explosionMask);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.root.TryGetComponent(out Character c))
                c.TakeDamage(explosionDamage, true);
        }

        Despawn();
    }

    private bool IsGrounded()
    {
        // if still moving fast, assume not settled yet
        if (rb != null && rb.linearVelocity.magnitude > minSpeedToConsiderAirborne)
            return false;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, mineRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, mineRadius);
    }
}