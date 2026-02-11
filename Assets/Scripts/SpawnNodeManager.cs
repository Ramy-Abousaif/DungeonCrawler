using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnNode
{
    public Vector3 position;
    public HullSize hullSize;
}

public class SpawnNodeManager : MonoBehaviour
{
    public int nodeCount = 300;
    public float sampleRadius = 100f;
    [SerializeField] private LayerMask worldLayer;
    [SerializeField] private LayerMask obstacleLayer;

    private List<SpawnNode> nodes = new List<SpawnNode>();
    private Transform player;
    private Vector3 lastValidNavPos;

    private void Awake()
    {
        player = GameObject.FindWithTag("Player").transform;
        if (NavMesh.SamplePosition(player.position, out var hit, 100f, NavMesh.AllAreas))
            lastValidNavPos = hit.position;
        BuildNodes();
    }

    private void Update()
    {
        if (NavMesh.SamplePosition(player.position, out var hit, 100f, NavMesh.AllAreas))
            lastValidNavPos = hit.position;
    }

    private void BuildNodes()
    {
        nodes.Clear();
        int attempts = 0;
        int maxNodes = nodeCount;

        while (nodes.Count < maxNodes && attempts < maxNodes * 10)
        {
            Vector3 random = Random.insideUnitSphere * sampleRadius;
            random.y = 0f;

            Vector3 candidate = lastValidNavPos + random;

            // Increase sample radius to be safe
            if (NavMesh.SamplePosition(candidate, out var hit, sampleRadius, NavMesh.AllAreas))
            {
                nodes.Add(new SpawnNode { position = hit.position });
            }

            attempts++;
        }
    }

    public SpawnNode GetValidNode(SpawnCard card)
    {
        List<SpawnNode> validNodes = new List<SpawnNode>();

        foreach (var node in nodes)
        {
            float radius = GetHullRadius(card.hullSize);

            if (Physics.CheckSphere(node.position, radius, obstacleLayer))
                continue;

            float dist = Vector2.Distance(
                new Vector2(node.position.x, node.position.z),
                new Vector2(lastValidNavPos.x, lastValidNavPos.z)
            );

            if (dist < card.minDistance || dist > card.maxDistance)
                continue;

            if (HasLineOfSight(node.position))
                continue;

            validNodes.Add(node);
        }

        if (validNodes.Count == 0)
            return null;

        return validNodes[Random.Range(0, validNodes.Count)];
    }

    private bool HasLineOfSight(Vector3 pos)
    {
        Camera cam = Camera.main;
        if (!cam)
            return false;

        Vector3 eye = cam.transform.position;
        Vector3 dir = pos - eye;
        float dist = dir.magnitude;

        // World geometry blocks fairness
        if (Physics.Raycast(eye, dir.normalized, dist, worldLayer))
            return false;

        return true;
    }

    private SpawnNode GetFlyingNode(SpawnCard card)
    {
        Vector3 pos = lastValidNavPos;

        Vector2 circle = Random.insideUnitCircle.normalized *
                        Random.Range(card.minDistance, card.maxDistance);

        pos += new Vector3(circle.x, 0f, circle.y);
        pos.y += card.airHeight;

        return new SpawnNode { position = pos };
    }

    private float GetHullRadius(HullSize size)
    {
        switch (size)
        {
            case HullSize.Small: return 0.5f;
            case HullSize.Medium: return 1.0f;
            case HullSize.Large: return 2.0f;
        }
        return 1f;
    }

    private void OnDrawGizmosSelected()
    {
        if (nodes == null) return;
        Gizmos.color = Color.yellow;
        foreach (var node in nodes)
            Gizmos.DrawSphere(node.position, 1f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(lastValidNavPos, 1f);
    }
}