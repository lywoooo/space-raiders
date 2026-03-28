using System.Collections;
using System.Collections.Generic;
=======
>>>>>>> ddbb4cdad82a772c4c6fdbf4e98932ebaa5ab42e
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public ProceduralBlockGenerator blockGenerator;
    public GameObject[] enemyPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float spawnOffsetHeight = 5f;  // Height above/below block to spawn enemy
    [SerializeField] private float spawnRandomRadius = 3f;  // Random radius around spawn point

    private float timeSinceLastSpawn = 0f;

    private void Start()
    {
        if (blockGenerator == null)
            blockGenerator = FindObjectOfType<ProceduralBlockGenerator>();

        if (enemyPrefab == null || enemyPrefab.Length == 0)
            Debug.LogError("EnemySpawner: Enemy prefab array is empty.", this);

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
        if (enemyPrefab == null || enemyPrefab.Length == 0 || blockGenerator == null)
            return;

        // Try to find a valid spawn position on a loaded block multiple times
        for (int attempts = 0; attempts < 5; attempts++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            
            if (spawnPos != Vector3.zero)
            {
                GameObject prefabToUse = enemyPrefab[Random.Range(0, enemyPrefab.Length)];
                Instantiate(prefabToUse, spawnPos, Quaternion.identity);
                return;
            }

            GameObject prefabToSpawn = enemyPrefab[Random.Range(0, enemyPrefab.Length)];
            if (prefabToSpawn == null)
                continue;

            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            return;
        }

        Debug.LogWarning("EnemySpawner: Could not find valid spawn position after 5 attempts");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Get a random position from a loaded block
        if (!blockGenerator.HasSpawnedBlocks())
        {
            // No blocks spawned yet, spawn near player
            Vector3 playerPos = blockGenerator.GetPlayerPosition();
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
        if (blockGenerator != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(blockGenerator.GetPlayerPosition() + Vector3.up * spawnOffsetHeight, spawnRandomRadius);
        }
    }
}
