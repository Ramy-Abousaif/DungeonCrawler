using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class Projectile : PooledObject
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private bool directDamage = true;

    private Character owner;
    private int abilityIndex = -1;

    private float baseRange = 30;
    private float range;
    private Vector3 startPos;
    private float forceDestroyTimer = 0f;
    private float forceDestroyCapacity = 5f;

    private MaterialPropertyBlock _mpb;
    private Renderer[] _renderers;
    protected Rigidbody rb;

    [Header("Impact")]
    [SerializeField] private bool stickable;
    [SerializeField] private float rotXOffset, rotZOffset;
    [SerializeField] private GameObject impactFX;
    [SerializeField] private GameObject ongoingEffect;

    [Header("Dissolve")]
    [SerializeField] private bool dissolveable = false;
    [SerializeField] private float dissolveDelay = 1.0f;
    [SerializeField] private float dissolveDuration = 0.4f;
    [SerializeField] private VisualEffect[] vfx;
    private Coroutine dissolveRoutine;

    private bool hasHit = false;
    private float rotYOffset;

    protected virtual void Awake()
    {
        startPos = transform.position;
        rotYOffset = transform.eulerAngles.y;
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnSpawned()
    {
        if (dissolveable && dissolveRoutine != null)
        {
            StopCoroutine(dissolveRoutine);
            dissolveRoutine = null;
        }

        hasHit = false;
        forceDestroyTimer = 0f;
        startPos = transform.position;

        transform.SetParent(null, true);

        if (ongoingEffect != null)
            ongoingEffect.SetActive(true);

        if (dissolveable && _renderers != null)
        {
            foreach (Renderer r in _renderers)
            {
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_DissolveAmount", 0f);
                r.SetPropertyBlock(_mpb);
            }
        }

        if (vfx != null && vfx.Length > 0)
        {
            for (int i = 0; i < vfx.Length; i++)
            {
                if (vfx[i] == null) continue;
                vfx[i].Stop();
                vfx[i].Reinit();
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var c = GetComponent<Collider>();
        if (c != null) c.enabled = true;
    }

    protected virtual void Update()
    {
        if (hasHit && !dissolveable)
            return;

        forceDestroyTimer += Time.deltaTime;

        if ((Vector3.Distance(startPos, transform.position) >= range) ||
            (forceDestroyTimer >= forceDestroyCapacity))
        {
            Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;

        // ignore owner collisions
        if (owner != null && other.transform.root == owner.transform)
            return;

        // only hit allowed layers
        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;

        if (stickable)
        {
            Vector3 contactPoint = transform.position;
            Vector3 contactNormal = Vector3.up;

            Vector3 dir = rb != null && rb.linearVelocity.sqrMagnitude > 0.01f
                ? rb.linearVelocity.normalized
                : transform.forward;

            if (Physics.Raycast(transform.position - dir * 0.2f, dir,
                out RaycastHit hit, 1f, ~0, QueryTriggerInteraction.Ignore))
            {
                contactPoint = hit.point;
                contactNormal = hit.normal;
            }
            else
            {
                contactPoint = transform.position;
                contactNormal = -dir;
            }

            StickIntoSurface(other, contactPoint, contactNormal);
        }
        else
        {
            if (impactFX != null)
                PoolManager.Instance.Spawn(impactFX, transform.position, Quaternion.identity);
        }

        // Deal damage to ANY Character (player or enemy)
        if (other.transform.root.TryGetComponent(out Character target))
        {
            // prevent friendly fire if needed
            if (owner != null && target == owner)
                return;

            if(owner is PhysicsBasedCharacterController)
            {
                (owner as PhysicsBasedCharacterController).abilities.OnHit(target as Enemy, abilityIndex, directDamage);
                (owner as PhysicsBasedCharacterController).CallItemOnHit(target as Enemy);
            }
            else
            {
                target.TakeDamage(damage, directDamage);
            }
        }

        hasHit = true;

        // if not stickable, despawn immediately after hit
        if (!stickable)
            Despawn();
    }

    private void StickIntoSurface(Collider other, Vector3 contactPoint, Vector3 contactNormal)
    {
        if (ongoingEffect != null)
            ongoingEffect.SetActive(false);

        transform.position = contactPoint;

        Quaternion alignNormal = Quaternion.FromToRotation(Vector3.up, contactNormal);
        Quaternion prefabOffset = Quaternion.Euler(rotXOffset, rotYOffset, rotZOffset);
        transform.rotation = alignNormal * prefabOffset;

        transform.SetParent(other.transform);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (impactFX != null)
            PoolManager.Instance.Spawn(impactFX, transform.position, Quaternion.identity);

        if (dissolveable)
        {
            if (dissolveRoutine != null)
                StopCoroutine(dissolveRoutine);

            dissolveRoutine = StartCoroutine(DissolveRoutine());
        }
    }

    private IEnumerator DissolveRoutine()
    {
        yield return new WaitForSeconds(dissolveDelay);

        float elapsed = 0f;

        if (vfx != null && vfx.Length > 0)
        {
            for (int i = 0; i < vfx.Length; i++)
            {
                if (vfx[i] == null) continue;
                vfx[i].Play();
                vfx[i].SetFloat("Duration", dissolveDuration);
            }
        }

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveDuration);

            foreach (Renderer r in _renderers)
            {
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_DissolveAmount", t);
                r.SetPropertyBlock(_mpb);
            }

            yield return null;
        }

        if (vfx != null && vfx.Length > 0)
        {
            for (int i = 0; i < vfx.Length; i++)
            {
                if (vfx[i] == null) continue;
                vfx[i].Stop();
            }
        }

        yield return new WaitForSeconds(0.5f);
        Despawn();
    }

    // ---------- API ----------
    public void SetOwner(Character c) => owner = c;
    public void SetDamage(float dmg) => damage = dmg;
    public void SetHitMask(LayerMask mask) => hitMask = mask;

    public void SetRange(float rng)
    {
        range = baseRange + (rng * 10);
    }

    public void SetAbilityIndex(int i)
    {
        abilityIndex = i;
    }
}