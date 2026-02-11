using System.Collections.Generic;
using UnityEngine;

public static class AITargetManager
{
    private static readonly List<AITargetable> targets = new();

    public static void Register(AITargetable t)
    {
        if (t == null) return;
        if (!targets.Contains(t)) targets.Add(t);
    }

    public static void Unregister(AITargetable t)
    {
        if (t == null) return;
        targets.Remove(t);
    }

    public static AITargetable GetBestTarget(Vector3 from, Faction seekerFaction, float maxRange = Mathf.Infinity)
    {
        Cleanup();

        AITargetable best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null) continue;

            // don't target self
            if ((t.transform.position - from).sqrMagnitude < 0.01f)
                continue;

            // check faction hostility
            if (!FactionUtil.AreHostile(seekerFaction, t.faction))
                continue;

            float dist = Vector3.Distance(from, t.transform.position);

            // maxRange passed in from AIBase
            if (dist > maxRange)
                continue;

            // target-specific range (like lures only attract within X)
            if (t.maxAttractRange > 0f && dist > t.maxAttractRange)
                continue;

            // --- scoring ---
            float score = 0f;

            // base priority
            score += t.priority * 100f;

            // lure boost
            if (t.isLure)
                score += 10000f;

            // prefer closer targets
            score += -dist * 5f;

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best;
    }

    private static void Cleanup()
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] == null || !targets[i].gameObject.activeInHierarchy)
                targets.RemoveAt(i);
        }
    }
}