using UnityEngine;
using System.Collections;

public class AuraEmitter : AbilityEmitter
{
    [Header("Aura Settings")]
    [SerializeField] private float auraHeight = 0.2f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float growTime = 0.4f;
    [SerializeField] private float shrinkTime = 0.4f;
    [SerializeField] private float tickRate = 0.5f;
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0,0,1,1);

    private float currentRadiusMultiplier = 0f;
    private Abilities.Ability _ability;

    protected override void PerformFire(Abilities.Ability ability)
    {
        if (isFiring) return;

        _ability = ability;
        firingRoutine = StartCoroutine(AuraRoutine());
    }

    private IEnumerator AuraRoutine()
    {
        isFiring = true;

        if (abilityEffect != null)
        {
            abilityEffect.SetActive(true);
            abilityEffect.transform.localScale = Vector3.zero;
        }

        yield return ScaleAura(0f, 1f, growTime);

        float activeTimer = 0f;

        while (activeTimer < duration)
        {
            ApplyAuraDamage();
            yield return new WaitForSeconds(tickRate);
            activeTimer += tickRate;
        }

        yield return ScaleAura(1f, 0f, shrinkTime);

        if (abilityEffect != null)
            abilityEffect.SetActive(false);

        isFiring = false;
    }

    private IEnumerator ScaleAura(float from, float to, float time)
    {
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);
            float curved = scaleCurve.Evaluate(t);

            currentRadiusMultiplier = Mathf.LerpUnclamped(from, to, curved);

            UpdateVisualScale();

            yield return null;
        }

        currentRadiusMultiplier = to;
        UpdateVisualScale();
    }

    private void UpdateVisualScale()
    {
        if (abilityEffect == null) return;

        float scaledRadius = baseRadius * _ability.currentAbilityRange * currentRadiusMultiplier;
        abilityEffect.transform.localScale = new Vector3(scaledRadius, auraHeight, scaledRadius);
    }

    private void ApplyAuraDamage()
    {
        float radius = baseRadius * _ability.currentAbilityRange * currentRadiusMultiplier;

        Collider[] hits = Physics.OverlapSphere(
            player.transform.position,
            radius,
            enemyMask
        );

        foreach (Collider col in hits)
        {
            if (col.transform.root.TryGetComponent(out Enemy enemy))
            {
                player.abilities.OnHit(enemy, abilityIndex, true);
                player.CallItemOnHit(enemy);
            }
        }
    }

    public override void StopFire()
    {
        if (!isFiring) return;

        StopCoroutine(firingRoutine);
        StartCoroutine(ScaleAura(currentRadiusMultiplier, 0f, shrinkTime));
        isFiring = false;
    }
}