using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RoomCombatController : MonoBehaviour
{
    public SpawnCardPool spawnPool;
    private List<Wave> waves;
    private float waveDelay = 2.0f;
    private int currentWaveIndex = 0;
    private int aliveEnemies = 0;

    private DungeonRoom dungeonRoom;
    private float tileSize;

    public void Initialize(DungeonRoom room)
    {
        dungeonRoom = room;
        tileSize = FindFirstObjectByType<DungeonGenerator>().tileSize;
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
        Vector3 spawnPos = FindValidSpawnPosition(card);
        if (spawnPos == Vector3.zero) 
        {
            Debug.LogWarning($"Could not find valid spawn position for {card.prefab.name}");
            return;
        }

        GameObject enemy = Instantiate(card.prefab, spawnPos, Quaternion.identity);

        aliveEnemies++;

        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        enemyComponent.OnDeath += HandleEnemyDeath;
    }

    Vector3 FindValidSpawnPosition(SpawnCard card, int maxAttempts = 20)
    {
        // Calculate actual room center (pivot is at edge, not center)
        Vector3 roomPivot = dungeonRoom.spawnedObject.transform.position;
        float roomWidth = dungeonRoom.width * tileSize;
        float roomLength = dungeonRoom.length * tileSize;
        Vector3 roomCenter = roomPivot + new Vector3(roomWidth * 0.5f, 0, roomLength * 0.5f);
        
        float halfWidth = (roomWidth * 0.5f) - 2f;  // Shrink bounds slightly for safety
        float halfLength = (roomLength * 0.5f) - 2f;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Random position within room bounds
            float randomX = Random.Range(-halfWidth, halfWidth);
            float randomZ = Random.Range(-halfLength, halfLength);
            Vector3 candidate = roomCenter + new Vector3(randomX, 0, randomZ);

            // Try to find valid NavMesh position near candidate
            if (NavMesh.SamplePosition(candidate, out var hit, 5f, NavMesh.AllAreas))
            {
                // Verify the NavMesh position is still within room bounds
                if (IsPositionInRoom(hit.position, roomCenter, halfWidth, halfLength))
                {
                    return hit.position;
                }
            }
        }

        return Vector3.zero;
    }

    bool IsPositionInRoom(Vector3 position, Vector3 roomCenter, float halfWidth, float halfLength)
    {
        return Mathf.Abs(position.x - roomCenter.x) <= halfWidth &&
               Mathf.Abs(position.z - roomCenter.z) <= halfLength;
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