using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public enum HitscanMode
{
    Single,
    Continuous
}

public class HitscanEmitter : AbilityEmitter
{
    [Header("Effects")]
    [SerializeField] private GameObject impactFX;
    
    [Header("Hitscan")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float baseRange = 100f;
    [SerializeField] private float damageTickRate = 0.1f;

    public HitscanMode mode = HitscanMode.Single;

    private float curveTime = 0f; // Drives VFX graph

    // Adjustable timing for curve segments
    [Header("Curve Segments")]
    [SerializeField] private float growSpeed = 6f;     // how fast 0 → 0.2
    [SerializeField] private float wiggleSpeed = 1f;   // how fast we move inside 0.2–0.9
    [SerializeField] private float shrinkSpeed = 4f;   // how fast 0.9 → 1
    private bool isShrinking = false;


    protected override void PerformFire(Abilities.Ability a)
    {
        if (mode == HitscanMode.Single)
        {
            curveTime = 0f;
            ApplyHitscanDamage(a);

            if (abilityEffect != null)
            {
                abilityEffect.SetActive(true);
                isShrinking = true;   // immediately play shrink
            }

            return;
        }

        // Continuous mode
        if (isFiring) return;

        curveTime = 0f;
        isFiring = true;
        isShrinking = false;

        firingRoutine = StartCoroutine(ContinuousFireRoutine(a));

        if (abilityEffect != null)
            abilityEffect.SetActive(true);
    }

    public override void StopFire()
    {
        isFiring = false;
        isShrinking = true;
    }

    private void Update()
    {
        if (!isFiring && !isShrinking)
            return;

        UpdateCurveTime();
        UpdateLaserVisuals();
    }

    private void UpdateCurveTime()
    {
        // Grow phase
        if (isFiring && curveTime < 0.2f)
        {
            curveTime += Time.deltaTime * growSpeed;
            curveTime = Mathf.Min(curveTime, 0.2f);
        }
        // Stable / wiggle phase
        else if (isFiring)
        {
            // Move slowly inside the wiggle range
            curveTime += Time.deltaTime * wiggleSpeed;
            curveTime = Mathf.Clamp(curveTime, 0.2f, 0.9f);
        }
        // Shrink phase
        else if (isShrinking)
        {
            curveTime += Time.deltaTime * shrinkSpeed;
            if (curveTime >= 1f)
            {
                curveTime = 0f;
                isShrinking = false;
                isFiring = false;

                var vfx = abilityEffect.GetComponent<VisualEffect>();
                if (vfx != null)
                {
                    vfx.SetFloat("LaserTime", curveTime);
                }

                if (abilityEffect != null)
                    abilityEffect.SetActive(false);
            }
        }
    }

    private void UpdateLaserVisuals()
    {
        if (abilityEffect == null || firePoint == null)
            return;

        if (!abilityEffect.activeSelf)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        abilityEffect.transform.position = firePoint.position;
        abilityEffect.transform.up = firePoint.transform.forward;

        var vfx = abilityEffect.GetComponent<VisualEffect>();
        if (vfx != null)
        {
            vfx.SetFloat("LaserTime", curveTime);
            vfx.SetFloat("Length", baseRange);
        }
    }

    private IEnumerator ContinuousFireRoutine(Abilities.Ability a)
    {
        while (isFiring)
        {
            ApplyHitscanDamage(a);
            yield return new WaitForSeconds(damageTickRate);
        }
    }

    private void ApplyHitscanDamage(Abilities.Ability a)
    {
        if (firePoint == null) return;

        float range = baseRange * a.currentAbilityRange;

        Camera cam = Camera.main;

        Ray ray = new Ray(firePoint.transform.position, firePoint.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            if (cam != null)
                cam.GetComponent<CameraShake>().ShakeCamera(2, 1, 0.3f);
            
            PoolManager.Instance.Spawn(impactFX, hit.point, Quaternion.LookRotation(hit.normal));
            if (hit.collider.transform.root.TryGetComponent(out Enemy enemy))
            {
                player.abilities.OnHit(enemy, abilityIndex, true);
                player.CallItemOnHit(enemy);
            }
        }
    }
}
