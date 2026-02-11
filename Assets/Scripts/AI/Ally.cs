using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Ally : Character
{
    [Header("Effects")]
    public GameObject deathEffect;

    [Header("Dissolve")]
    [SerializeField] private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    [SerializeField] private bool dissolveable = false;
    [SerializeField] private float dissolveDelay = 1.0f;
    [SerializeField] private float dissolveDuration = 0.4f;
    private Coroutine dissolveRoutine;

    private float lifetime = 10f;
    private float timer;
    private bool isDestroyed = false;

    protected override void Awake()
    {
        base.Awake();
        _mpb = new MaterialPropertyBlock();
        Fade(true);
    }

    public void Initialize(float summonDuration)
    {
        lifetime = summonDuration;
        timer = 0f;
    }

    protected override void Update()
    {
        base.Update();
        timer += Time.deltaTime;
        if (timer >= lifetime && !isDestroyed)
        {
            isDestroyed = true;
            Fade(false);
        }
    }

    protected override void OnDamageTaken(float damage, bool direct = false)
    {
        // Hit flash
        Flash();
        if(currentHealth > 0f)
        {
            ShowNumber(damage);            
        }
        // Sound
    }

    protected override void onHeal(float amount)
    {
        ShowNumber(amount);
    }

    public void Fade(bool fadeIn)
    {
        // Optional: play fade effect here
        if (dissolveable)
        {
            if (dissolveRoutine != null)
                StopCoroutine(dissolveRoutine);

            dissolveRoutine = StartCoroutine(DissolveRoutine(fadeIn));
        }
    }

    private IEnumerator DissolveRoutine(bool fadeIn)
    {
        if(!fadeIn)
            yield return new WaitForSeconds(dissolveDelay);

        float elapsed = 0f;

        float start = fadeIn ? 1f : 0f;
        float end   = fadeIn ? 0f : 1f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveDuration);

            float dissolve = Mathf.Lerp(start, end, t);

            foreach (Renderer r in _renderers)
            {
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_DissolveAmount", dissolve);
                r.SetPropertyBlock(_mpb);
            }

            yield return null;
        }

        foreach (Renderer r in _renderers)
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat("_DissolveAmount", end);
            r.SetPropertyBlock(_mpb);
        }

        if (!fadeIn)
        {
            Destroy(gameObject);
        }
    }

    protected override void Die()
    {
        currentHealth = 0f;
        PoolManager.Instance.Spawn(deathEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
