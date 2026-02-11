using UnityEngine;

public enum HullSize
{
    Small,
    Medium,
    Large
}

[CreateAssetMenu(menuName = "Spawning/Spawn Card")]
public class SpawnCard : ScriptableObject
{
    public GameObject prefab;
    public float cost = 10f;

    public HullSize hullSize;
    public bool isFlying;
    public float airHeight = 20f;
    public float minDistance = 15f;
    public float maxDistance = 60f;
}