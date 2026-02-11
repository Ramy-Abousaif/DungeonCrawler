using UnityEngine;

public abstract class AIAttack : MonoBehaviour
{
    [SerializeField] protected float cooldown = 1.2f;
    private float timer;

    public void Tick(float dt) => timer -= dt;
    public bool IsReady => timer <= 0f;

    public void TriggerCooldown() => timer = cooldown;

    public abstract bool CanUse(AIBase ai);
    public abstract void Execute(AIBase ai);
}