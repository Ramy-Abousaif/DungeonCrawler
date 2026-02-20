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

    public void Initialize()
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

            if (NavMesh.SamplePosition(candidate, out var hit, sampleRadius, NavMesh.AllAreas))
            {
                nodes.Add(new SpawnNode { position = hit.position });
            }

            attempts++;
        }
    }

    // public SpawnNode GetValidNodeInRoom(SpawnCard card, RoomNode room)
    // {
    //     List<SpawnNode> validNodes = new List<SpawnNode>();

    //     foreach (var node in nodes)
    //     {
    //         if (!IsNodeInsideRoom(node.position, room))
    //             continue;

    //         float radius = GetHullRadius(card.hullSize);

    //         if (Physics.CheckSphere(node.position, radius, obstacleLayer))
    //             continue;

    //         validNodes.Add(node);
    //     }

    //     if (validNodes.Count == 0)
    //         return null;

    //     return validNodes[Random.Range(0, validNodes.Count)];
    // }

    // private bool IsNodeInsideRoom(Vector3 position, RoomNode room)
    // {
    //     Vector3 center = room.RoomObject.transform.position;

    //     Debug.Log(room.WorldSize);
    //     float halfX = room.WorldSize.x * 0.5f;
    //     float halfZ = room.WorldSize.y * 0.5f;

    //     return Mathf.Abs(position.x - center.x) <= halfX &&
    //         Mathf.Abs(position.z - center.z) <= halfZ;
    // }

    public SpawnNode GetValidNode(SpawnCard card)
    {
        List<SpawnNode> validNodes = new List<SpawnNode>();

        foreach (var node in nodes)
        {
            float radius = GetHullRadius(card.hullSize);

            if (Physics.CheckSphere(node.position, radius, obstacleLayer))
                continue;

            validNodes.Add(node);
        }

        if (validNodes.Count == 0)
            return null;

        return validNodes[Random.Range(0, validNodes.Count)];
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