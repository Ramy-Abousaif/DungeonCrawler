using Unity.Mathematics;
using UnityEngine;

public class Enemy : Character
{
    [Header("Effects")]
    public GameObject deathEffect;
    public System.Action OnDeath;

    protected override void OnDamageTaken(float damage, bool direct = false)
    {
        // Hit flash
        Flash();
        if(currentHealth > 0f)
        {
            ShowNumber(damage);            
        }
        // Sound
        if(direct)
        {
            camShake.ShakeCamera(2f, 2f, 0.3f);
        }
    }

    protected override void onHeal(float amount)
    {
        ShowNumber(amount);
    }

    protected override void Die()
    {
        currentHealth = 0f;
        PoolManager.Instance.Spawn(deathEffect, transform.position, Quaternion.identity);
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}