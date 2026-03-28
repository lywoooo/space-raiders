using System.Collections.Generic;
using UnityEngine;

public class ProceduralBlockGenerator : MonoBehaviour
{
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

    private List<Vector3> usedPositions = new List<Vector3>();
    private List<GameObject> spawnedBlocks = new List<GameObject>();
    private Dictionary<Vector3Int, int> chunkBlockCount = new Dictionary<Vector3Int, int>();
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
                // Block was destroyed - update chunk count
                Vector3Int chunkCoord = GetChunkCoordinate(usedPositions[i]);
                if (chunkBlockCount.ContainsKey(chunkCoord))
                {
                    chunkBlockCount[chunkCoord]--;
                }

                usedPositions.RemoveAt(i);
                spawnedBlocks.RemoveAt(i);
            }
        }
    }
}
