using System.Collections.Generic;
using UnityEngine;

public class ProceduralBlockGenerator : MonoBehaviour
{
    /// <summary>
    /// Represents an invisible zone where blocks were destroyed and should not be regenerated
    /// </summary>
    private struct DestroyedZone
    {
        public Vector3 position;
        public Vector3 size;
        public float creationTime;
    }

    [Header("References")]
    public Transform player;
    public GameObject greenCubePrefab;     // Origin marker
    public GameObject[] blockPrefab;         // Block to spawn around the player

    [Header("Generation Area")]
    public float generationRadiusX = 60f;
    public float generationRadiusZ = 60f;
    public int blockCount = 25;
    public float minSpacing = 4.5f;

    [Header("Block Size")]
    public Vector3 minBlockSize = new Vector3(1f, 1f, 1f);
    public Vector3 maxBlockSize = new Vector3(4f, 4f, 4f);

    [Header("Height Variation")]
    public float minBlockHeight = 0f;
    public float maxBlockHeight = 30f;

    [Header("Spatial Chunking - Density Control")]
    public float chunkSize = 40f;
    public int blocksPerChunk = 3;

    [Header("Continuous Generation")]
    public float regenerationDistance = 20f;

    [Header("Rarity System")]
    public float distanceFromOriginForRarity = 50f;
    public float rarityMultiplierPerUnit = 0.02f;

    [Header("Destroyed Zone Settings")]
    [SerializeField] private float zoneBufferRadius = 5f;  // Extra radius around destroyed blocks
    [SerializeField] private bool debugShowZones = false;  // Draw destroyed zones in Scene view

    private List<Vector3> usedPositions = new List<Vector3>();
    private List<GameObject> spawnedBlocks = new List<GameObject>();
    private Dictionary<Vector3Int, int> chunkBlockCount = new Dictionary<Vector3Int, int>();
    private List<DestroyedZone> destroyedZones = new List<DestroyedZone>();
    private Vector3 originPosition;
    private Vector3 lastGenerationPosition;

    void Start()
    {
        originPosition = player.position;
        GenerateOriginMarker();
        GenerateBlocks();
        lastGenerationPosition = player.position;
    }

    void Update()
    {
        // Check if player has moved far enough to trigger new generation
        float distanceFromLastGen = Vector3.Distance(player.position, lastGenerationPosition);
        
        if (distanceFromLastGen >= regenerationDistance)
        {
            GenerateBlocks();
            lastGenerationPosition = player.position;
            CleanupDistantBlocks();
        }
    }

    void GenerateOriginMarker()
    {
        if (greenCubePrefab != null)
        {
            Instantiate(greenCubePrefab, player.position, Quaternion.identity);
        }
        else
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = player.position;
            marker.transform.localScale = Vector3.one;

            Renderer r = marker.GetComponent<Renderer>();
            r.material.color = Color.green;
        }
    }

    void GenerateBlocks()
    {
        if (blockPrefab == null)
            return;

        int spawned = 0;
        int attempts = 0;

        while (spawned < blockCount && attempts < blockCount * 30)
        {
            attempts++;

            // Generate position in elliptical area with different X and Z radii and height variation
            Vector2 randomCircle = Random.insideUnitCircle;
            Vector3 spawnPos = new Vector3(
                player.position.x + randomCircle.x * generationRadiusX,
                Random.Range(minBlockHeight, maxBlockHeight),
                player.position.z + randomCircle.y * generationRadiusZ
            );

            // Check if position is inside a destroyed zone
            if (IsInDestroyedZone(spawnPos))
                continue;

            // Get the chunk this position belongs to
            Vector3Int chunkCoord = GetChunkCoordinate(spawnPos);

            // Check if this chunk has reached its block limit
            if (!chunkBlockCount.ContainsKey(chunkCoord))
            {
                chunkBlockCount[chunkCoord] = 0;
            }

            if (chunkBlockCount[chunkCoord] >= blocksPerChunk)
                continue;

            bool tooClose = false;

            // Check against all used positions (considers all previously generated blocks)
            foreach (Vector3 pos in usedPositions)
            {
                if (Vector3.Distance(pos, spawnPos) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            GameObject prefabToUse = blockPrefab[Random.Range(0, blockPrefab.Length)];
            GameObject block = Instantiate(prefabToUse, spawnPos, Quaternion.identity);

            float sizeX = Random.Range(minBlockSize.x, maxBlockSize.x);
            float sizeY = Random.Range(minBlockSize.y, maxBlockSize.y);
            float sizeZ = Random.Range(minBlockSize.z, maxBlockSize.z);

            block.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);

            // Calculate rarity based on distance from origin
            float distanceFromOrigin = Vector3.Distance(spawnPos, originPosition);
            float rarityMultiplier = 1f + (distanceFromOrigin * rarityMultiplierPerUnit);
            
            // Apply rarity color (white to yellow gradient)
            Color rarityColor = Color.Lerp(Color.white, new Color(1f, 1f, 0f, 1f), Mathf.Min(rarityMultiplier - 1f, 1f));
            
            // Apply the rarity color to the block
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(renderer.material);
                mat.color = rarityColor;
                renderer.material = mat;
            }

            // Update chunk block count
            chunkBlockCount[chunkCoord]++;

            usedPositions.Add(spawnPos);
            spawnedBlocks.Add(block);
            spawned++;
        }
    }

    Vector3Int GetChunkCoordinate(Vector3 position)
    {
        // Convert world position to chunk coordinate
        int chunkX = Mathf.FloorToInt(position.x / chunkSize);
        int chunkY = Mathf.FloorToInt(position.y / chunkSize);
        int chunkZ = Mathf.FloorToInt(position.z / chunkSize);
        
        return new Vector3Int(chunkX, chunkY, chunkZ);
    }

    void CleanupDistantBlocks()
    {
        // Only clean up null blocks (destroyed manually or by other means)
        // Keep all generated blocks in the world
        for (int i = spawnedBlocks.Count - 1; i >= 0; i--)
        {
            if (spawnedBlocks[i] == null)
            {
                // Block was destroyed - update chunk count and register destroyed zone
                Vector3Int chunkCoord = GetChunkCoordinate(usedPositions[i]);
                if (chunkBlockCount.ContainsKey(chunkCoord))
                {
                    chunkBlockCount[chunkCoord]--;
                }

                // Register this position as a destroyed zone so no new blocks spawn here
                RegisterDestroyedZone(usedPositions[i], Vector3.one);

                usedPositions.RemoveAt(i);
                spawnedBlocks.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Registers a position as a destroyed zone (invisible chunk block) where new blocks should not spawn
    /// </summary>
    public void RegisterDestroyedZone(Vector3 position, Vector3 blockSize)
    {
        DestroyedZone zone = new DestroyedZone
        {
            position = position,
            size = blockSize + Vector3.one * zoneBufferRadius * 2f,  // Add buffer radius
            creationTime = Time.time
        };
        
        destroyedZones.Add(zone);
    }

    /// <summary>
    /// Checks if a position is inside any destroyed zone
    /// </summary>
    private bool IsInDestroyedZone(Vector3 position)
    {
        foreach (DestroyedZone zone in destroyedZones)
        {
            Vector3 diff = position - zone.position;
            
            // Check if position is within the zone bounds
            if (Mathf.Abs(diff.x) < zone.size.x / 2f &&
                Mathf.Abs(diff.y) < zone.size.y / 2f &&
                Mathf.Abs(diff.z) < zone.size.z / 2f)
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Visual debugging - draws destroyed zones in the Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!debugShowZones)
            return;

        if (destroyedZones == null)
            return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);  // Red with transparency
        
        foreach (DestroyedZone zone in destroyedZones)
        {
            Gizmos.DrawCube(zone.position, zone.size);
        }
    }

    /// <summary>
    /// Gets a random position from a loaded block, useful for spawning enemies
    /// </summary>
    public Vector3 GetRandomBlockPosition()
    {
        if (spawnedBlocks.Count == 0)
            return player.position;

        // Find a non-null block
        GameObject randomBlock = null;
        int attempts = 0;
        
        while (randomBlock == null && attempts < 10)
        {
            int randomIndex = Random.Range(0, spawnedBlocks.Count);
            randomBlock = spawnedBlocks[randomIndex];
            attempts++;
        }

        if (randomBlock != null)
        {
            return randomBlock.transform.position;
        }

        return player.position;
    }

    /// <summary>
    /// Gets the count of currently spawned blocks
    /// </summary>
    public int GetSpawnedBlockCount()
    {
        return spawnedBlocks.Count;
    }
}
