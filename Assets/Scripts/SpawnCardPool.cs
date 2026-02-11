using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spawning/Spawn Pool")]
public class SpawnCardPool : ScriptableObject
{
    public List<SpawnCard> cards;

    public SpawnCard GetAffordable(float credits)
    {
        List<SpawnCard> valid = cards.FindAll(c => c.cost <= credits);
        if (valid.Count == 0)
            return null;

        return valid[Random.Range(0, valid.Count)];
    }
}