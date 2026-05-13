using UnityEngine;
using System.Collections.Generic;

public partial class MapGenerator : MonoBehaviour
{
    public enum MapType
    {
        RandomScatter,  // 0: Your original method
        Maze_ARA,       // 1: High-compute dense maze (Recursive Backtracker)
        Caverns_LPA,    // 2: Organic shifting caves (Cellular Automata)
        Arena_DLite     // 3: Open areas with clustered cover (Perlin Noise)
    }

    [Header("Generation Style")]
    public bool randomlySelectMapType = true; // NEW: Toggle to allow random selection
    public MapType currentMapType = MapType.Maze_ARA;

    [Header("Basic Settings")]
    public GameObject wallPrefab;
    public int mapSize = 100;
    [Range(0, 100)]
    public int obstacleDensity = 10;

    [Header("Start and End Settings")]
    public GameObject player;
    public GameObject destination;
    public float minDistance = 20f;

    [Header("Seed System")]
    public int currentSeed;
    public bool useRandomSeed = true;

    [Header("Puzzle Settings")]
    public GameObject puzzlePrefab;
    public int puzzleCount = 200;

    [Header("Bot Settings")]
    public GameObject botPrefab;
    public int numberOfBots = 15;
    private List<GameObject> spawnedBots = new List<GameObject>();

    private bool[,] grid;

    void Start()
    {
        if (useRandomSeed)
        {
            currentSeed = Random.Range(0, 99999);
        }

        Random.InitState(currentSeed);
        Debug.Log("Current Map Seed: " + currentSeed);

        // --- NEW: Random Map Selection Logic ---
        if (randomlySelectMapType)
        {
            // Random.Range(1, 4) picks an integer: 1, 2, or 3. 
            // 1 = Maze_ARA, 2 = Caverns_LPA, 3 = Arena_DLite
            // It intentionally skips 0 (RandomScatter)
            currentMapType = (MapType)Random.Range(1, 4);
            Debug.Log("Randomly Selected Map Type: " + currentMapType);
        }
        // ---------------------------------------

        GenerateMap();
        SetStartAndEnd();
        SpawnPuzzles();

        PathfindingGrid pg = FindFirstObjectByType<PathfindingGrid>();
        if (pg != null)
        {
            pg.CreateGrid();
            SpawnBots(pg);
        }
        else
        {
            Debug.LogError("MapGenerator: PathfindingGrid script not found! Cannot spawn bots.");
        }
    }

    void GenerateMap()
    {
        grid = new bool[mapSize, mapSize];
        
        // 1. Create the logical grid based on the chosen algorithm
        switch (currentMapType)
        {
            case MapType.RandomScatter:
                GenerateRandomScatter();
                break;
            case MapType.Maze_ARA:
                GenerateMazeARA();
                break;
            case MapType.Caverns_LPA:
                GenerateCavernsLPA();
                break;
            case MapType.Arena_DLite:
                GenerateArenaDLite();
                break;
        }

        // 2. Enforce Map Borders
        for (int i = 0; i < mapSize; i++)
        {
            grid[i, 0] = true;
            grid[i, mapSize - 1] = true;
            grid[0, i] = true;
            grid[mapSize - 1, i] = true;
        }

        // 3. Instantiate physical walls
        float offset = mapSize / 2f;
        for (int x = 0; x < mapSize; x++)
        {
            for (int z = 0; z < mapSize; z++)
            {
                if (grid[x, z])
                {
                    Vector3 spawnPos = new Vector3(x - offset + 0.5f, 1f, z - offset + 0.5f);
                    Instantiate(wallPrefab, spawnPos, Quaternion.identity, transform);
                }
            }
        }
    }

    // ==========================================
    // ALGORITHM SPECIFIC GENERATORS
    // ==========================================

    void GenerateRandomScatter()
    {
        // Your original generation logic
        for (int x = 1; x < mapSize - 1; x++)
        {
            for (int z = 1; z < mapSize - 1; z++)
            {
                if (Random.Range(0, 100) < obstacleDensity)
                    grid[x, z] = true;
            }
        }

        for (int x = 1; x < mapSize - 1; x++)
        {
            for (int z = 1; z < mapSize - 1; z++)
            {
                if (!grid[x, z] && IsTooNarrow(x, z))
                    grid[x, z] = true;
            }
        }
    }

    void GenerateMazeARA()
    {
        // Fills map with walls, then carves a perfect maze using a simple drunkard's walk / recursive backtracker
        for (int x = 0; x < mapSize; x++)
            for (int z = 0; z < mapSize; z++)
                grid[x, z] = true; 

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(1, 1);
        grid[current.x, current.y] = false;
        stack.Push(current);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (stack.Count > 0)
        {
            current = stack.Pop();
            List<Vector2Int> unvisitedNeighbors = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                int nx = current.x + dir.x * 2;
                int nz = current.y + dir.y * 2;

                if (nx > 0 && nx < mapSize - 1 && nz > 0 && nz < mapSize - 1 && grid[nx, nz])
                {
                    unvisitedNeighbors.Add(dir);
                }
            }

            if (unvisitedNeighbors.Count > 0)
            {
                stack.Push(current);
                Vector2Int chosenDir = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                
                // Carve path
                grid[current.x + chosenDir.x, current.y + chosenDir.y] = false; // Carve through wall
                grid[current.x + chosenDir.x * 2, current.y + chosenDir.y * 2] = false; // Carve into next cell
                
                stack.Push(new Vector2Int(current.x + chosenDir.x * 2, current.y + chosenDir.y * 2));
            }
        }
    }

    void GenerateCavernsLPA()
    {
        // Cellular Automata: Organic caves ideal for dynamic obstacle injection later
        int fillPercent = 45;

        // Initial noise
        for (int x = 1; x < mapSize - 1; x++)
        {
            for (int z = 1; z < mapSize - 1; z++)
            {
                grid[x, z] = (Random.Range(0, 100) < fillPercent);
            }
        }

        // Smoothing passes
        for (int i = 0; i < 5; i++)
        {
            bool[,] newGrid = (bool[,])grid.Clone();
            for (int x = 1; x < mapSize - 1; x++)
            {
                for (int z = 1; z < mapSize - 1; z++)
                {
                    int neighborWallTiles = GetSurroundingWallCount(x, z);
                    if (neighborWallTiles > 4) newGrid[x, z] = true;
                    else if (neighborWallTiles < 4) newGrid[x, z] = false;
                }
            }
            grid = newGrid;
        }
    }

    void GenerateArenaDLite()
    {
        // Perlin Noise: Open arena with sparse cover, great for Builder dropping new walls
        float scale = 0.1f; // Higher scale = smaller noise clusters
        float threshold = 0.65f; // Higher threshold = fewer walls

        // Shift the Perlin sample area so random seeds actually change the layout
        float offsetX = Random.Range(0f, 10000f);
        float offsetZ = Random.Range(0f, 10000f);

        for (int x = 1; x < mapSize - 1; x++)
        {
            for (int z = 1; z < mapSize - 1; z++)
            {
                float noiseValue = Mathf.PerlinNoise((x + offsetX) * scale, (z + offsetZ) * scale);
                grid[x, z] = (noiseValue > threshold);
            }
        }
    }

    // ==========================================
    // UTILITY METHODS
    // ==========================================

    int GetSurroundingWallCount(int gridX, int gridZ)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourZ = gridZ - 1; neighbourZ <= gridZ + 1; neighbourZ++)
            {
                if (neighbourX >= 0 && neighbourX < mapSize && neighbourZ >= 0 && neighbourZ < mapSize)
                {
                    if (neighbourX != gridX || neighbourZ != gridZ)
                    {
                        if (grid[neighbourX, neighbourZ]) wallCount++;
                    }
                }
                else
                {
                    wallCount++; // Encourage walls on the very edge
                }
            }
        }
        return wallCount;
    }

    bool IsTooNarrow(int x, int z)
    {
        int horizontalWalls = (grid[x - 1, z] ? 1 : 0) + (grid[x + 1, z] ? 1 : 0);
        int verticalWalls = (grid[x, z - 1] ? 1 : 0) + (grid[x, z + 1] ? 1 : 0);
        return (horizontalWalls > 1 || verticalWalls > 1);
    }

    void SetStartAndEnd()
    {
        Vector2 IntStart = Vector2.zero;
        Vector2 IntEnd = Vector2.zero;
        bool pointsFound = false;
        int safetyNet = 0;

        while (!pointsFound && safetyNet < 1000)
        {
            safetyNet++;
            IntStart = new Vector2(Random.Range(1, mapSize - 1), Random.Range(1, mapSize - 1));
            IntEnd = new Vector2(Random.Range(1, mapSize - 1), Random.Range(1, mapSize - 1));

            if (!grid[(int)IntStart.x, (int)IntStart.y] && !grid[(int)IntEnd.x, (int)IntEnd.y])
            {
                if (Vector2.Distance(IntStart, IntEnd) > minDistance)
                {
                    pointsFound = true;
                }
            }
        }

        float offset = mapSize / 2f;
        player.transform.position = new Vector3(IntStart.x - offset + 0.5f, 1f, IntStart.y - offset + 0.5f);
        destination.transform.position = new Vector3(IntEnd.x - offset + 0.5f, 1f, IntEnd.y - offset + 0.5f);
    }

    void SpawnPuzzles()
    {
        int placedPuzzles = 0;
        int safetyNet = 1000;
        float offset = mapSize / 2f;

        while (placedPuzzles < puzzleCount && safetyNet < 5000)
        {
            safetyNet++;
            int x = Random.Range(1, mapSize - 1);
            int z = Random.Range(1, mapSize - 1);

            if (!grid[x, z])
            {
                Vector3 spawnPos = new Vector3(x - offset + 0.5f, 1f, z - offset + 0.5f);
                
                if (Vector3.Distance(spawnPos, player.transform.position) > 2f &&
                    Vector3.Distance(spawnPos, destination.transform.position) > 2f)
                {
                    Instantiate(puzzlePrefab, spawnPos, Quaternion.identity, transform);
                    placedPuzzles++;
                }
            }
        }
    }

    void SpawnBots(PathfindingGrid gridScript)
    {
        if (botPrefab == null) { Debug.LogError("HunterBot Prefab is null!"); return; }

        foreach (GameObject bot in spawnedBots) { if(bot != null) Destroy(bot); }
        spawnedBots.Clear();

        Node[,] gridNodes = gridScript.GetGrid();
        if (gridNodes == null) { Debug.LogError("Grid Nodes not initialized!"); return; }

        int spawnedCount = 0;
        int attempts = 0; 

        while (spawnedCount < numberOfBots && attempts < 1000)
        {
            attempts++;
            int x = Random.Range(0, mapSize);
            int z = Random.Range(0, mapSize);

            if (x >= gridNodes.GetLength(0) || z >= gridNodes.GetLength(1)) continue;

            if (gridNodes[x, z].walkable)
            {
                Vector3 spawnPos = gridNodes[x, z].worldPosition + Vector3.up * 1f;
                GameObject newBot = Instantiate(botPrefab, spawnPos, Quaternion.identity);
                
                HunterAgent agent = newBot.GetComponent<HunterAgent>();
                if (agent != null) 
                {
                    agent.playerTransform = player.transform;
                }

                spawnedBots.Add(newBot);
                spawnedCount++;
            }
        }
    }
}