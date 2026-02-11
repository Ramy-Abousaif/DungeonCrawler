using UnityEngine;

public class AutoDespawn : PooledObject
{
    [SerializeField] private float lifetime = 5f;

    public override void OnSpawned()
    {
        PoolManager.Instance.Despawn(gameObject, lifetime);
    }
}