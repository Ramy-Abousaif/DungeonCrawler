using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HeadLookAt : MonoBehaviour
{
    private Rig rig;
    private float targetWeight = 1f;
    [SerializeField] private Transform target;
    [Tooltip("How often (seconds) the target selection updates. Lower = more frequent.")]
    [SerializeField] private float updateInterval = 0.05f;

    private readonly List<Transform> nearbyEnemies = new List<Transform>();
    private bool createdRuntimeTarget;

    void Start()
    {
        rig = GetComponent<Rig>();

        if (target == null)
        {
            GameObject go = new GameObject("HeadLookTarget");
            go.transform.SetParent(null);
            target = go.transform;
            createdRuntimeTarget = true;
        }

        StartCoroutine(UpdateTargetLoop());
    }

    void OnDestroy()
    {
        if (createdRuntimeTarget && target != null)
            Destroy(target.gameObject);
    }

    void Update()
    {
        rig.weight = Mathf.Lerp(rig.weight, targetWeight, Time.deltaTime * 5f);
    }

    private IEnumerator UpdateTargetLoop()
    {
        var wait = new WaitForSeconds(updateInterval);
        while (true)
        {
            UpdateTargetOnce();
            yield return wait;
        }
    }

    private void UpdateTargetOnce()
    {
        for (int i = nearbyEnemies.Count - 1; i >= 0; i--)
        {
            if (nearbyEnemies[i] == null) nearbyEnemies.RemoveAt(i);
        }

        if (nearbyEnemies.Count > 0)
        {
            Transform nearest = nearbyEnemies[0];
            float bestSqr = (nearest.position - transform.position).sqrMagnitude;
            for (int i = 1; i < nearbyEnemies.Count; i++)
            {
                var e = nearbyEnemies[i];
                if (e == null) continue;
                float sqr = (e.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = e;
                }
            }

            if (nearest != null)
            {
                target.position = nearest.position;
                targetWeight = 1f;
            }
        }
        else
        {
            targetWeight = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            var t = other.transform;
            if (!nearbyEnemies.Contains(t))
                nearbyEnemies.Add(t);
            targetWeight = 1f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            nearbyEnemies.Remove(other.transform);
            if (nearbyEnemies.Count == 0)
                targetWeight = 1f;
        }
    }
}
