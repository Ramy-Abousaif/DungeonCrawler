using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomCombatController : MonoBehaviour
{
    [SerializeField] private SpawnCardPool spawnPool;
    private List<Wave> waves;
    private float waveDelay = 2.0f;
    private int currentWaveIndex = 0;
    private int aliveEnemies = 0;

    private DungeonRoom dungeonRoom;
    private SpawnNodeManager nodeManager;

    public void Initialize(DungeonRoom room)
    {
        dungeonRoom = room;
        nodeManager = FindFirstObjectByType<SpawnNodeManager>();
    }

    public void StartCombat()
    {
        if(dungeonRoom.roomType != RoomType.Normal)
            return;

        if (dungeonRoom.IsCleared)
            return;

        GenerateWaves();

        currentWaveIndex = 0;
        aliveEnemies = 0;

        LockDoors();
        StartCoroutine(StartNextWave(waveDelay));
    }

    IEnumerator StartNextWave(float delay)
    {
        yield return new WaitForSeconds(currentWaveIndex == 0 ? 0.5f : delay);

        if (currentWaveIndex >= waves.Count)
        {
            CombatComplete();
            yield break;
        }

        Wave wave = waves[currentWaveIndex];

        foreach (var card in wave.enemies)
        {
            SpawnEnemy(card);
        }

        currentWaveIndex++;
    }

    void SpawnEnemy(SpawnCard card)
    {
        SpawnNode node = nodeManager.GetValidNodeInRoom(card, dungeonRoom);
        if (node == null) return;

        GameObject enemy = Instantiate(card.prefab, node.position, Quaternion.identity);

        aliveEnemies++;

        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        enemyComponent.OnDeath += HandleEnemyDeath;
    }

    void HandleEnemyDeath()
    {
        aliveEnemies--;

        if (aliveEnemies <= 0)
        {
            StartCoroutine(StartNextWave(waveDelay));
        }
    }

    void CombatComplete()
    {
        dungeonRoom.IsCleared = true;

        foreach (var door in dungeonRoom.Doors)
        {
            door.SetCombatLocked(false);
        }
    }

    List<SpawnCard> GenerateEnemiesForWave(float difficulty, int waveIndex)
    {
        List<SpawnCard> result = new List<SpawnCard>();

        float waveScaling = 1f + waveIndex * 0.3f;

        float budget = difficulty * 10f * waveScaling;

        while (budget > 0)
        {
            SpawnCard card = spawnPool.GetAffordable(budget);

            if (card == null)
                break;

            result.Add(card);
            budget -= card.cost;
        }

        return result;
    }

    void GenerateWaves()
    {
        waves = new List<Wave>();

        float difficulty = DifficultyDirector.Instance.GetDifficulty();

        int waveCount = Mathf.Clamp(
            Mathf.FloorToInt(1 + difficulty * 0.5f),
            1,
            8
        );

        for (int i = 0; i < waveCount; i++)
        {
            Wave newWave = new Wave();
            newWave.enemies = GenerateEnemiesForWave(difficulty, i);
            waves.Add(newWave);
        }
    }

    void LockDoors()
    {
        foreach (var door in dungeonRoom.Doors)
        {
            door.SetCombatLocked(true);
            door.PlayDoorAnim(false);
        }
    }
}