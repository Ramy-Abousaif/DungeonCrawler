using UnityEngine;

public class AITargetable : MonoBehaviour
{
    [Header("Target Identity")]
    public Faction faction = Faction.Enemy;

    [Header("Priority / Aggro")]
    [Tooltip("Higher = more attractive target")]
    public int priority = 0;

    [Tooltip("0 = infinite")]
    public float maxAttractRange = 0f;

    [Tooltip("If true, AI will prefer this target heavily (decoy / taunt)")]
    public bool isLure = false;

    private void OnEnable()
    {
        AITargetManager.Register(this);
    }

    private void OnDisable()
    {
        AITargetManager.Unregister(this);
    }
}