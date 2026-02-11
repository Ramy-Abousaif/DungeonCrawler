using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPool
{
    public GameObject prefab;
    public int initialSize = 10;
    public bool expandable = true;

    private readonly Queue<GameObject> pool = new();
    private Transform parent;

    public void Initialize(Transform root)
    {
        parent = new GameObject(prefab.name + "_Pool").transform;
        parent.SetParent(root);

        for (int i = 0; i < initialSize; i++)
        {
            var obj = CreateNew();
            pool.Enqueue(obj);
        }
    }

    private GameObject CreateNew()
    {
        var obj = Object.Instantiate(prefab, parent);
        obj.SetActive(false);

        var pooled = obj.GetComponent<PooledObject>();
        if (pooled == null)
            pooled = obj.AddComponent<PooledObject>();

        pooled.Pool = this;

        return obj;
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            if (!expandable)
                return null;

            obj = CreateNew();
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        obj.GetComponent<PooledObject>()?.OnSpawned();

        return obj;
    }

    public void Despawn(GameObject obj)
    {
        obj.GetComponent<PooledObject>()?.OnDespawned();
        obj.SetActive(false);
        obj.transform.SetParent(parent);
        pool.Enqueue(obj);
    }
}