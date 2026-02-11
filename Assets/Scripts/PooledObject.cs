using UnityEngine;

public class PooledObject : MonoBehaviour
{
    [HideInInspector] public ObjectPool Pool;

    /// <summary>Called when taken from the pool.</summary>
    public virtual void OnSpawned() { }

    /// <summary>Called when returned to the pool.</summary>
    public virtual void OnDespawned() { }

    public void Despawn()
    {
        if (Pool != null)
            Pool.Despawn(gameObject);
        else
            gameObject.SetActive(false);
    }
}
