using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [SerializeField] private List<ObjectPool> pools = new();

    private Dictionary<GameObject, ObjectPool> poolLookup = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (var p in pools)
        {
            if (p.prefab == null) continue;

            p.Initialize(transform);
            poolLookup[p.prefab] = p;
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        if (poolLookup.TryGetValue(prefab, out var pool))
        {
            return pool.Spawn(pos, rot);
        }

        Debug.LogWarning($"No pool exists for prefab: {prefab.name}. Instantiating instead.");
        return Instantiate(prefab, pos, rot);
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        var pooled = obj.GetComponent<PooledObject>();
        if (pooled != null && pooled.Pool != null)
        {
            pooled.Pool.Despawn(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    public void Despawn(GameObject obj, float delay)
    {
        StartCoroutine(DespawnRoutine(obj, delay));
    }

    private IEnumerator DespawnRoutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(obj);
    }
}