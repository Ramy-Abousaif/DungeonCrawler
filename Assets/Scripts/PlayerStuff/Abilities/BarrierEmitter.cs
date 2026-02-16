using UnityEngine;
using System.Collections;

public class BarrierEmitter : AbilityEmitter
{
    [Header("Barrier Settings")]
    [SerializeField] private float duration = 3f;
    [SerializeField] private Vector3 barrierSize;
    [SerializeField] private float growTime = 0.25f;
    [SerializeField] private float shrinkTime = 0.25f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0,0,1,1);

    protected override void PerformFire(Abilities.Ability ability)
    {
        if (isFiring) return;

        firingRoutine = StartCoroutine(BarrierRoutine());
    }

    private IEnumerator BarrierRoutine()
    {
        isFiring = true;

        if (abilityEffect == null)
            yield break;

        abilityEffect.SetActive(true);
        abilityEffect.transform.localScale = Vector3.zero;

        yield return ScaleOverTime(Vector3.zero, barrierSize, growTime);

        player.IsInvulnerable = true;

        yield return new WaitForSeconds(duration);

        player.IsInvulnerable = false;

        yield return ScaleOverTime(barrierSize, Vector3.zero, shrinkTime);

        abilityEffect.SetActive(false);
        isFiring = false;
    }

    private IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float time)
    {
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);
            float curved = scaleCurve.Evaluate(t);

            abilityEffect.transform.localScale = Vector3.LerpUnclamped(from, to, curved);
            yield return null;
        }

        abilityEffect.transform.localScale = to;
    }

    public override void StopFire()
    {
        if (!isFiring) return;

        StopCoroutine(firingRoutine);
        player.IsInvulnerable = false;
        StartCoroutine(ForceShrink());
    }

    private IEnumerator ForceShrink()
    {
        yield return ScaleOverTime(abilityEffect.transform.localScale, Vector3.zero, shrinkTime);
        abilityEffect.SetActive(false);
        isFiring = false;
    }
}