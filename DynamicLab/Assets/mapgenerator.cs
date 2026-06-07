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

    [Header("Mode Settings")]
    public bool isMainMenu = false; // Toggle this ON only in the MainMenu scene

    [Header("Generation Style")]
    public bool randomlySelectMapType = true; // Toggle to allow random selection
    public MapType currentMapType = MapType.Maze_ARA;

    // ==========================================
    // --- Biome Settings ---
    // ==========================================
    [Header("Biome Settings")]
    public BiomeData randomScatterBiome;
    public BiomeData mazeARABiome;
    public BiomeData cavernsLPABiome;
    public BiomeData arenaDLiteBiome;
    
    private BiomeData currentBiome;
    // ==========================================

    [Header("Basic Settings")]
    public GameObject wallPrefab; // משמש כגיבוי למקרה שאין Biome
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

    // ==========================================================
    // --- NEW FOR VISUALIZATION: משתנים ופונקציות גישה לגריד ---
    // ==========================================================
    [HideInInspector] public Vector2Int startGridPos;
    [HideInInspector] public Vector2Int endGridPos;

    public bool[,] GetGrid()
    {
        return grid;
    }
    // ==========================================================

    void Start()
    {
        if (useRandomSeed)
        {
            currentSeed = Random.Range(0, 99999);
        }

        Random.InitState(currentSeed);
        Debug.Log("Current Map Seed: " + currentSeed);

        // --- Random Map Selection Logic ---
        if (randomlySelectMapType)
        {
            // Random.Range(1, 4) picks an integer: 1, 2, or 3. 
            // 1 = Maze_ARA, 2 = Caverns_LPA, 3 = Arena_DLite
            // It intentionally skips 0 (RandomScatter)
            currentMapType = (MapType)Random.Range(1, 4);
            Debug.Log("Randomly Selected Map Type: " + currentMapType);
        }

        // ==========================================
        // --- Assign Current Biome ---
        // ==========================================
        switch (currentMapType)
        {
            case MapType.RandomScatter: currentBiome = randomScatterBiome; break;
            case MapType.Maze_ARA: currentBiome = mazeARABiome; break;
            case MapType.Caverns_LPA: currentBiome = cavernsLPABiome; break;
            case MapType.Arena_DLite: currentBiome = arenaDLiteBiome; break;
        }
        // ==========================================

        GenerateMap();
        ApplyAtmosphere(); // קריאה לפונקציית מזג האוויר והתאורה

        // ==========================================
        // --- ONLY SPAWN GAMEPLAY ELEMENTS IF NOT IN MENU ---
        // ==========================================
        if (!isMainMenu)
        {
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

        // ==========================================
        // --- Instantiate Weather System ---
        // ==========================================
        if (currentBiome != null && currentBiome.weatherSystemPrefab != null)
        {
            Instantiate(currentBiome.weatherSystemPrefab, Vector3.zero, Quaternion.identity, transform);
        }

        // 3. Instantiate physical walls AND FLOORS
        float offset = mapSize / 2f;
        for (int x = 0; x < mapSize; x++)
        {
            for (int z = 0; z < mapSize; z++)
            {
                // הגדרת מיקומים לרצפה ומכשול
                Vector3 floorPos = new Vector3(x - offset + 0.5f, 0f, z - offset + 0.5f);
                Vector3 obstaclePos = new Vector3(x - offset + 0.5f, 0f, z - offset + 0.5f);

                // רינדור הרצפה בכל משבצת
                if (currentBiome != null && currentBiome.floorPrefabs != null && currentBiome.floorPrefabs.Length > 0)
                {
                    GameObject floorChoice = currentBiome.floorPrefabs[Random.Range(0, currentBiome.floorPrefabs.Length)];
                    Instantiate(floorChoice, floorPos, Quaternion.identity, transform);
                }

                // רינדור מכשול רק אם במטריצה מוגדר קיר (true)
                if (grid[x, z])
                {
                    if (currentBiome != null && currentBiome.obstaclePrefabs != null && currentBiome.obstaclePrefabs.Length > 0)
                    {
                        GameObject obstacleChoice = currentBiome.obstaclePrefabs[Random.Range(0, currentBiome.obstaclePrefabs.Length)];
                        Instantiate(obstacleChoice, obstaclePos, Quaternion.identity, transform);
                    }
                    else
                    {
                        // חלופה למקרה שהביום ריק
                        Instantiate(wallPrefab, obstaclePos, Quaternion.identity, transform);
                    }
                }
                // --- רינדור קישוטים איפה שאין קיר ---
                else 
                {
                    if (currentBiome != null && currentBiome.decorationPrefabs != null && currentBiome.decorationPrefabs.Length > 0)
                    {
                        if (Random.Range(0, 100) < currentBiome.decorationChance)
                        {
                            GameObject decoChoice = currentBiome.decorationPrefabs[Random.Range(0, currentBiome.decorationPrefabs.Length)];
                            Instantiate(decoChoice, obstaclePos, Quaternion.identity, transform);
                        }
                    }
                }
            }
        }
    }

    // ==========================================
    // ALGORITHM SPECIFIC GENERATORS
    // ==========================================

    void GenerateRandomScatter()
    {
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
        // 1. ניצור מבוך מיניאטורי בחצי מהגודל של המפה המקורית
        int miniSize = mapSize / 2;
        bool[,] miniGrid = new bool[miniSize, miniSize];

        // אתחול המבוך המיניאטורי עם קירות
        for (int x = 0; x < miniSize; x++)
            for (int z = 0; z < miniSize; z++)
                miniGrid[x, z] = true; 

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(1, 1);
        miniGrid[current.x, current.y] = false;
        stack.Push(current);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // 2. יצירת המבוך הבסיסי (Recursive Backtracker) על הגריד המוקטן
        while (stack.Count > 0)
        {
            current = stack.Pop();
            List<Vector2Int> unvisitedNeighbors = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                int nx = current.x + dir.x * 2;
                int nz = current.y + dir.y * 2;

                if (nx > 0 && nx < miniSize - 1 && nz > 0 && nz < miniSize - 1 && miniGrid[nx, nz])
                {
                    unvisitedNeighbors.Add(dir);
                }
            }

            if (unvisitedNeighbors.Count > 0)
            {
                stack.Push(current);
                Vector2Int chosenDir = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                
                miniGrid[current.x + chosenDir.x, current.y + chosenDir.y] = false; 
                miniGrid[current.x + chosenDir.x * 2, current.y + chosenDir.y * 2] = false; 
                
                stack.Push(new Vector2Int(current.x + chosenDir.x * 2, current.y + chosenDir.y * 2));
            }
        }

        // 3. הרחבה: כל משבצת במבוך המיניאטורי הופכת לבלוק של 2x2 במפה האמיתית
        for (int x = 0; x < miniSize; x++)
        {
            for (int z = 0; z < miniSize; z++)
            {
                bool isWall = miniGrid[x, z];
                
                // מיפוי לגריד המקורי (הכפלה ב-2)
                grid[x * 2, z * 2] = isWall;
                grid[x * 2 + 1, z * 2] = isWall;
                grid[x * 2, z * 2 + 1] = isWall;
                grid[x * 2 + 1, z * 2 + 1] = isWall;
            }
        }

        // 4. דילול המבוך ליצירת מסלולים נוספים (Loops) - מותאם למעברים כפולים
        int chunksToBreak = (mapSize * mapSize) / 40; 
        
        for (int i = 0; i < chunksToBreak; i++)
        {
            int randomX = Random.Range(1, miniSize - 1) * 2;
            int randomZ = Random.Range(1, miniSize - 1) * 2;
            
            grid[randomX, randomZ] = false; 
            grid[randomX + 1, randomZ] = false;
            grid[randomX, randomZ + 1] = false;
            grid[randomX + 1, randomZ + 1] = false;
        }
    }

    void GenerateCavernsLPA()
    {
        int fillPercent = 45;

        for (int x = 1; x < mapSize - 1; x++)
        {
            for (int z = 1; z < mapSize - 1; z++)
            {
                grid[x, z] = (Random.Range(0, 100) < fillPercent);
            }
        }

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
        float scale = 0.1f; 
        float threshold = 0.65f; 

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
                    wallCount++; 
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

        // --- NEW FOR VISUALIZATION: שמירת קואורדינטות הגריד האמיתיות של ההתחלה והסיום ---
        startGridPos = new Vector2Int((int)IntStart.x, (int)IntStart.y);
        endGridPos = new Vector2Int((int)IntEnd.x, (int)IntEnd.y);
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

    // ==========================================
    // --- ATMOSPHERE & LIGHTING ---
    // ==========================================
    void ApplyAtmosphere()
    {
        if (currentBiome == null) return;

        // 1. שינוי השמיים (Skybox)
        if (currentBiome.skyboxMaterial != null)
        {
            RenderSettings.skybox = currentBiome.skyboxMaterial;
        }

        // 2. הגדרות ערפל (Fog)
        RenderSettings.fog = true;
        RenderSettings.fogColor = currentBiome.fogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = currentBiome.fogDensity;

        // 3. שינוי אור השמש
        Light sun = RenderSettings.sun;
        if (sun != null)
        {
            sun.color = currentBiome.sunColor;
        }

        // רענון התאורה הגלובלית כדי שהשינויים יחולו מיד
        DynamicGI.UpdateEnvironment();
    }
}