using UnityEngine;
using UnityEngine.AI;

public class SummonEmitter : AbilityEmitter
{
    [Header("Summon Settings")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private bool summonAllAtOnce = false;
    [SerializeField] private int maxActiveSummons = 1;
    [SerializeField] private float spawnDistance = 2.5f;
    [SerializeField] private float spawnRadius = 1.0f;
    [SerializeField] private float summonDuration = 10f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private bool replaceOldest = true;
    [SerializeField] private bool onGround = true;

    private readonly System.Collections.Generic.List<Ally> activeSummons = new();

    protected override void PerformFire(Abilities.Ability ability)
    {
        if (summonPrefab == null)
        {
            Debug.LogWarning("SummonEmitter: No summonPrefab assigned.");
            return;
        }

        CleanupList();

        int desiredSpawnCount = summonAllAtOnce ? maxActiveSummons : 1;

        for (int i = 0; i < desiredSpawnCount; i++)
        {
            CleanupList();

            if (activeSummons.Count >= maxActiveSummons)
            {
                if (!replaceOldest)
                    return;

                while (activeSummons.Count >= maxActiveSummons)
                {
                    if (activeSummons[0] != null)
                    {
                        activeSummons[0].Fade(false);
                    }

                    activeSummons.RemoveAt(0);
                }
            }

            Vector3 spawnPos;
            Quaternion spawnRot;

            if (!TryGetSpawnPoint(out spawnPos, out spawnRot))
            {
                spawnPos = player.transform.position + player.transform.forward * 1.5f;
                spawnRot = Quaternion.LookRotation(player.transform.forward, Vector3.up);
            }

            GameObject obj = PoolManager.Instance.Spawn(summonPrefab, spawnPos, spawnRot);

            if (obj.TryGetComponent(out Ally ally))
            {
                ally.Initialize(summonDuration);
                activeSummons.Add(ally);
            }
            else
            {
                Debug.LogWarning("SummonEmitter: Spawned prefab has no SummonAllyAI component.");
            }
        }
    }

    private void CleanupList()
    {
        for (int i = activeSummons.Count - 1; i >= 0; i--)
        {
            if (activeSummons[i] == null || !activeSummons[i].gameObject.activeInHierarchy)
                activeSummons.RemoveAt(i);
        }
    }

    private bool TryGetSpawnPoint(out Vector3 pos, out Quaternion rot)
    {
        // Desired spawn point: in front of player, on ground
        Camera cam = Camera.main;
        Vector3 forward = cam ? cam.transform.forward : player.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 desired = player.transform.position + forward * spawnDistance;

        // desired on ground
        if (onGround)
        {
            if (Physics.Raycast(desired, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, groundMask))
            {
                desired.y = groundHit.point.y;
            }
        }

        // randomize slightly so it doesn't always stack on same spot
        Vector2 rnd = Random.insideUnitCircle * spawnRadius;
        desired += new Vector3(rnd.x, 0f, rnd.y);

        // snap to navmesh if possible
        if (NavMesh.SamplePosition(desired, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            pos = navHit.position;
            rot = Quaternion.LookRotation(forward, Vector3.up);
            return true;
        }

        // else raycast to ground
        if (Physics.Raycast(desired + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundMask))
        {
            pos = hit.point;
            rot = Quaternion.LookRotation(forward, Vector3.up);
            return true;
        }

        pos = Vector3.zero;
        rot = Quaternion.identity;
        return false;
    }
}
