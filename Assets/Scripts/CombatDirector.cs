using UnityEngine;

public class CombatDirector : MonoBehaviour
{
    [Header("Economy")]
    public float credits;
    public float creditGainPerSecond = 1f;

    [Header("Scaling")]
    public float difficultyCoefficient = 1f;

    [Header("References")]
    public SpawnCardPool spawnPool;
    public SpawnNodeManager nodeManager;

    private void Update()
    {
        credits += creditGainPerSecond * difficultyCoefficient * Time.deltaTime;
        TrySpawn();
    }

    private void TrySpawn()
    {
        SpawnCard card = spawnPool.GetAffordable(credits);

        if (card == null)
            return;

        SpawnNode node = nodeManager.GetValidNode(card);
        Debug.Log(node != null ? "Got node" : "No valid node");

        if (node == null)
            return;

        Debug.Log("SPAWNING ENEMY");
        Instantiate(card.prefab, node.position, Quaternion.identity);
        credits -= card.cost;
    }
}