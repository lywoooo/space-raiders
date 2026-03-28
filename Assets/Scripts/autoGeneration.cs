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
    }

    private struct SpawnedBlockData
    {
        public GameObject instance;
        public Vector3 position;
        public Vector3 scale;
    }

    [Header("References")]
    public Transform player;
    //public GameObject greenCubePrefab;     // Origin marker
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

    private readonly List<SpawnedBlockData> spawnedBlocks = new List<SpawnedBlockData>();
    private readonly Dictionary<Vector3Int, int> chunkBlockCount = new Dictionary<Vector3Int, int>();
    private readonly List<DestroyedZone> destroyedZones = new List<DestroyedZone>();
    private readonly Dictionary<Vector3Int, List<int>> spacingGrid = new Dictionary<Vector3Int, List<int>>();
    private MaterialPropertyBlock materialPropertyBlock;
    private Vector3 originPosition;
    private Vector3 lastGenerationPosition;

    private void Awake()
    {
        materialPropertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("ProceduralBlockGenerator: Player reference is missing.", this);
            enabled = false;
            return;
        }

        originPosition = player.position;
        //GenerateOriginMarker();
        GenerateBlocks();
        lastGenerationPosition = player.position;
    }

    private void Update()
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

    // private void GenerateOriginMarker()
    // {
    //     // if (greenCubePrefab != null)
    //     // {
    //     //     Instantiate(greenCubePrefab, player.position, Quaternion.identity);
    //     // }
    //     // else
    //     // {
    //         GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //         marker.transform.position = player.position;
    //         marker.transform.localScale = Vector3.one;

    //         Renderer r = marker.GetComponent<Renderer>();
    //         r.material.color = Color.green;
    //     //}
    // }

    private void GenerateBlocks()
    {
        if (blockPrefab == null || blockPrefab.Length == 0)
            return;

        CleanupDestroyedBlocks();

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(blockCount * 30, 1);

        while (spawned < blockCount && attempts < maxAttempts)
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

            if (IsTooCloseToExistingBlock(spawnPos))
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
                renderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor("_Color", rarityColor);
                renderer.SetPropertyBlock(materialPropertyBlock);
            }

            // Update chunk block count
            chunkBlockCount[chunkCoord]++;

            RegisterSpawnedBlock(block, spawnPos, block.transform.localScale);
            spawned++;
        }
    }

    private Vector3Int GetChunkCoordinate(Vector3 position)
    {
        // Convert world position to chunk coordinate
        float safeChunkSize = Mathf.Max(chunkSize, 0.01f);
        int chunkX = Mathf.FloorToInt(position.x / safeChunkSize);
        int chunkY = Mathf.FloorToInt(position.y / safeChunkSize);
        int chunkZ = Mathf.FloorToInt(position.z / safeChunkSize);
        
        return new Vector3Int(chunkX, chunkY, chunkZ);
    }

    private Vector3Int GetSpacingCell(Vector3 position)
    {
        float safeSpacing = Mathf.Max(minSpacing, 0.01f);
        return new Vector3Int(
            Mathf.FloorToInt(position.x / safeSpacing),
            Mathf.FloorToInt(position.y / safeSpacing),
            Mathf.FloorToInt(position.z / safeSpacing));
    }

    private bool IsTooCloseToExistingBlock(Vector3 spawnPos)
    {
        if (spawnedBlocks.Count == 0)
            return false;

        float minSpacingSqr = minSpacing * minSpacing;
        Vector3Int cell = GetSpacingCell(spawnPos);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3Int neighborCell = new Vector3Int(cell.x + x, cell.y + y, cell.z + z);
                    if (!spacingGrid.TryGetValue(neighborCell, out List<int> indices))
                        continue;

                    for (int i = 0; i < indices.Count; i++)
                    {
                        int blockIndex = indices[i];
                        if (blockIndex < 0 || blockIndex >= spawnedBlocks.Count)
                            continue;

                        Vector3 delta = spawnedBlocks[blockIndex].position - spawnPos;
                        if (delta.sqrMagnitude < minSpacingSqr)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    private void RegisterSpawnedBlock(GameObject block, Vector3 position, Vector3 scale)
    {
        int index = spawnedBlocks.Count;
        spawnedBlocks.Add(new SpawnedBlockData
        {
            instance = block,
            position = position,
            scale = scale
        });

        Vector3Int cell = GetSpacingCell(position);
        if (!spacingGrid.TryGetValue(cell, out List<int> indices))
        {
            indices = new List<int>();
            spacingGrid[cell] = indices;
        }

        indices.Add(index);
    }

    private void RebuildSpatialIndex()
    {
        spacingGrid.Clear();

        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            Vector3Int cell = GetSpacingCell(spawnedBlocks[i].position);
            if (!spacingGrid.TryGetValue(cell, out List<int> indices))
            {
                indices = new List<int>();
                spacingGrid[cell] = indices;
            }

            indices.Add(i);
        }
    }

    private void CleanupDistantBlocks()
    {
        CleanupDestroyedBlocks();
    }

    private void CleanupDestroyedBlocks()
    {
        bool removedAny = false;

        for (int i = spawnedBlocks.Count - 1; i >= 0; i--)
        {
            SpawnedBlockData blockData = spawnedBlocks[i];
            if (blockData.instance == null)
            {
                // Block was destroyed - update chunk count and register destroyed zone
                Vector3Int chunkCoord = GetChunkCoordinate(blockData.position);
                if (chunkBlockCount.ContainsKey(chunkCoord))
                {
                    chunkBlockCount[chunkCoord] = Mathf.Max(0, chunkBlockCount[chunkCoord] - 1);
                }

                // Register this position as a destroyed zone so no new blocks spawn here
                RegisterDestroyedZone(blockData.position, blockData.scale);

                spawnedBlocks.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny)
            RebuildSpatialIndex();
    }

    /// <summary>
    /// Registers a position as a destroyed zone (invisible chunk block) where new blocks should not spawn
    /// </summary>
    public void RegisterDestroyedZone(Vector3 position, Vector3 blockSize)
    {
        DestroyedZone zone = new DestroyedZone
        {
            position = position,
            size = blockSize + Vector3.one * zoneBufferRadius * 2f  // Add buffer radius
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
        CleanupDestroyedBlocks();

        if (spawnedBlocks.Count == 0)
            return player != null ? player.position : transform.position;

        int randomIndex = Random.Range(0, spawnedBlocks.Count);
        return spawnedBlocks[randomIndex].position;
    }

    public bool HasSpawnedBlocks()
    {
        CleanupDestroyedBlocks();
        return spawnedBlocks.Count > 0;
    }

    public Vector3 GetPlayerPosition()
    {
        return player != null ? player.position : transform.position;
    }

    /// <summary>
    /// Gets the count of currently spawned blocks
    /// </summary>
    public int GetSpawnedBlockCount()
    {
        CleanupDestroyedBlocks();
        return spawnedBlocks.Count;
    }
}
