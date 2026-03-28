using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProceduralBlockGenerator blockGenerator;
    [SerializeField] private GameObject[] enemyPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float spawnOffsetHeight = 5f;  // Height above/below block to spawn enemy
    [SerializeField] private float spawnRandomRadius = 3f;  // Random radius around spawn point

    private float timeSinceLastSpawn = 0f;
    private List<GameObject> spawnedBlocks = new List<GameObject>();

    private void Start()
    {
        if (blockGenerator == null)
            blockGenerator = FindObjectOfType<ProceduralBlockGenerator>();

        if (enemyPrefab == null)
            Debug.LogError("EnemySpawner: Enemy prefab not assigned!");

        timeSinceLastSpawn = spawnInterval;  // Spawn first enemy immediately
    }

    private void Update()
    {
        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= spawnInterval)
        {
            SpawnEnemy();
            timeSinceLastSpawn = 0f;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || blockGenerator == null)
            return;

        // Try to find a valid spawn position on a loaded block multiple times
        for (int attempts = 0; attempts < 5; attempts++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            
            if (spawnPos != Vector3.zero)
            {
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                return;
            }
        }

        Debug.LogWarning("EnemySpawner: Could not find valid spawn position after 5 attempts");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Get a random position from a loaded block
        if (blockGenerator.GetSpawnedBlockCount() == 0)
        {
            // No blocks spawned yet, spawn near player
            Vector3 playerPos = blockGenerator.player.position;
            Vector3 randomOffset = Random.insideUnitSphere * blockGenerator.generationRadiusX * 0.5f;
            return playerPos + randomOffset + Vector3.up * spawnOffsetHeight;
        }

        Vector3 blockPosition = blockGenerator.GetRandomBlockPosition();
        
        // Add random offset around the block
        Vector3 randomOffset2 = Random.insideUnitSphere * spawnRandomRadius;
        return blockPosition + randomOffset2 + Vector3.up * spawnOffsetHeight;
    }

    private void OnDrawGizmos()
    {
        // Visualization for spawn radius
        if (blockGenerator != null && blockGenerator.player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(blockGenerator.player.position + Vector3.up * spawnOffsetHeight, spawnRandomRadius);
        }
    }
}
