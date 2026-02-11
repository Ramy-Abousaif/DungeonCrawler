using UnityEngine;
public abstract class StatusEffect
{
    protected Character target;
    protected int stacks;
    protected float duration;

    protected float tickInterval;
    protected float tickTimer;

    public void Initialize(Character target, int stacks, float duration)
    {
        this.target = target;
        this.stacks = stacks;
        this.duration = duration;
        tickTimer = 0f;
    }

    public void Refresh(int stacks, float duration)
    {
        this.stacks = Mathf.Max(this.stacks, stacks);
        this.duration = duration;
    }

    public void Update(float deltaTime)
    {
        duration -= deltaTime;
        tickTimer += deltaTime;

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            OnTick();
        }
    }

    protected abstract void OnTick();

    public bool IsExpired => duration <= 0f;
}