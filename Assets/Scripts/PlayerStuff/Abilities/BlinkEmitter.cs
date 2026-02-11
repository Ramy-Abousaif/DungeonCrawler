using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class BlinkEmitter : AbilityEmitter
{
    [Header("Dash")]
    [SerializeField] private float dashDistance = 8f;
    [SerializeField] private float dashDuration = 0.08f;
    [SerializeField] private bool lockControlDuringDash = true;

    [Header("Dash Extension")]
    [SerializeField] private float dashExtensionMultiplier = 1f; 
    private bool hasExtendedDash;
    private float remainingDashDistance;

    [Header("Damage")]
    [SerializeField] private LayerMask enemyLayer;
    private int playerMask;
    private int enemyMask;

    [Header("Dash VFX")]
    [SerializeField] private ParticleSystem dashParticles;
    [SerializeField] private Volume dashVolume;
    [SerializeField] private float dashVolumeInSpeed = 12f;
    [SerializeField] private float dashVolumeOutSpeed = 8f;
    private float _currentFresnelAmount;
    private float _initialFresnelAmount = 6.68f;
    private float _dashFresnelTargetAmount = 2.0f;
    private float dashVolumeWeight = 0f;

    private bool isDashing;
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    private Coroutine dashCoroutine;
    private Camera cam;

    void Start()
    {
        playerMask = LayerMask.NameToLayer("Player");
        enemyMask = LayerMask.NameToLayer("Enemy");

        cam = Camera.main;
    }

    void Update()
    {
        UpdateDashVolume();
    }

    protected override void PerformFire(Abilities.Ability ability)
    {
        if (isDashing)
            return;

        dashCoroutine = StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        dashParticles.Play();
        StartCoroutine(DashFresnel(_dashFresnelTargetAmount));
        hasExtendedDash = false;
        remainingDashDistance = dashDistance;
        Physics.IgnoreLayerCollision(playerMask, enemyMask, true);
        hitEnemies.Clear();

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            isDashing = false;
            yield break;
        }

        if (lockControlDuringDash)
            player.SetMovementEnabled(false);

        Vector3 dashDir = firePoint.transform.forward;

        float speed = dashDistance / dashDuration;

        while (remainingDashDistance > 0f)
        {
            float step = speed * Time.deltaTime;
            rb.linearVelocity = dashDir * speed;
            remainingDashDistance -= step;

            CheckDashHits();

            // FX

            yield return null;
        }

        rb.linearVelocity = Vector3.zero;
        StartCoroutine(DashFresnel(_initialFresnelAmount));
        EndDash();
    }

    IEnumerator DashFresnel(float targetAmount)
    {
        float elapsed = 0f;
        float start = _currentFresnelAmount;
        float target = targetAmount;

        while (elapsed < (dashDuration / 2))
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (dashDuration / 2);

            _currentFresnelAmount = Mathf.Lerp(start, target, t);
            player.abilities.SetFresnelAmount(_currentFresnelAmount);

            yield return null;
        }
    }
    
    public override void StopFire()
    {
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        EndDash();
        player.abilities.SetFresnelAmount(_initialFresnelAmount);
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        hitEnemies.Clear();

        base.StopFire();
    }

    private void EndDash()
    {
        if (lockControlDuringDash)
            player.SetMovementEnabled(true);

        Physics.IgnoreLayerCollision(playerMask, enemyMask, false);
        isDashing = false;
        dashParticles.Stop();
    }

    private void CheckDashHits()
    {
        Vector3 center = transform.position;
        float radius = 0.75f;
        float height = 1.8f;

        Vector3 top = center + Vector3.up * (height * 0.5f);
        Vector3 bottom = center - Vector3.up * (height * 0.5f);

        Collider[] hits = Physics.OverlapCapsule(top, bottom, radius, enemyLayer);

        bool hitSomething = false;

        foreach (var col in hits)
        {
            if (!col.transform.root.TryGetComponent(out Enemy enemy))
                continue;

            if (hitEnemies.Contains(enemy))
                continue;

            hitEnemies.Add(enemy);
            hitSomething = true;

            player.abilities.OnHit(enemy, abilityIndex, true);
            player.CallItemOnHit(enemy);
        }

        if (hitSomething && !hasExtendedDash)
        {
            hasExtendedDash = true;
            remainingDashDistance += dashDistance * dashExtensionMultiplier;
        }
    }

    private void UpdateDashVolume()
    {
        if (dashVolume == null)
            return;

        float target = isDashing ? 1f : 0f;
        float speed = isDashing ? dashVolumeInSpeed : dashVolumeOutSpeed;

        dashVolumeWeight = Mathf.MoveTowards(
            dashVolumeWeight,
            target,
            speed * Time.deltaTime
        );

        dashVolume.weight = dashVolumeWeight;
    }
}