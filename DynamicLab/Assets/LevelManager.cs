using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("In-Game UI")]
    public TextMeshProUGUI scoreText;
    public GameObject algorithmWindow; 
    public GameObject pauseMenuWindow; // <-- NEW

    [Header("End Game UI")]
    public GameObject levelEndWindow; 
    public TextMeshProUGUI runStatsText; // <-- NEW: Replaces basic text

    [Header("Pathfinding Visuals")]
    public LineRenderer pathRenderer; 

    [Header("Player & Destination")]
    public GameObject player;      
    public GameObject destination; 

    [Header("Seed Settings")]
    public static int lastUsedSeed;
    public static bool shouldReplaySameSeed = false;

    private int collectedPuzzles = 0;
    private Coroutine pathVisibilityCoroutine;
    
    private bool isPaused = false; // <-- NEW
    private string usedAlgorithm = "None"; // <-- NEW: Tracks what they chose

    void Awake()
    {
        if (instance == null) instance = this;

        // Seed logic moved to Awake so MapGenerator gets it in time!
        MapGenerator generator = FindFirstObjectByType<MapGenerator>();
        if (generator != null)
        {
            if (shouldReplaySameSeed)
            {
                generator.useRandomSeed = false;
                generator.currentSeed = lastUsedSeed;
            }
            else
            {
                generator.useRandomSeed = true;
            }
        }
    }

    void Start()
    {
        collectedPuzzles = 0;
        UpdateScoreUI();
        
        // Hide all overlay screens
        if (levelEndWindow != null) levelEndWindow.SetActive(false);
        if (algorithmWindow != null) algorithmWindow.SetActive(false);
        if (pauseMenuWindow != null) pauseMenuWindow.SetActive(false);
        
        if (pathRenderer != null) pathRenderer.enabled = false;

        ResumeGame(); // Ensures time scale and cursor are correct
    }

    // ==========================================
    // --- NEW: PAUSE MENU LOGIC ---
    // ==========================================
    void Update()
    {
        // Toggle pause menu with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Don't pause if the player is already in the end-game or algorithm menus
            if (levelEndWindow.activeSelf || algorithmWindow.activeSelf) return;

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuWindow != null) pauseMenuWindow.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuWindow != null) pauseMenuWindow.SetActive(false);
        Time.timeScale = 1f;
        
        // --- CHANGED: Now the mouse stays visible and unlocked during gameplay! ---
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true; 
        // --------------------------------------------------------------------------
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // Loads the Main Menu
    }
    // ==========================================

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MapGenerator generator = FindFirstObjectByType<MapGenerator>();
            if (generator != null) lastUsedSeed = generator.currentSeed;
            EndLevel();
        }
    }

    public void CollectPuzzle()
    {
        collectedPuzzles++;
        UpdateScoreUI();

        if (collectedPuzzles == 5)
        {
            OpenAlgorithmMenu();
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = "Puzzles: " + collectedPuzzles;
    }

    void OpenAlgorithmMenu()
    {
        if (algorithmWindow != null) algorithmWindow.SetActive(true);
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ==========================================
    // ALGORITHM SELECTION BUTTONS
    // ==========================================

    public void SelectARAStar() 
    { 
        usedAlgorithm = "ARA*"; // Track selection
        ARAStarPathfinder pathfinder = FindFirstObjectByType<ARAStarPathfinder>();
        
        if (pathfinder != null && player != null && destination != null)
        {
            List<Node> path = pathfinder.FindPath(player.transform.position, destination.transform.position);
            if (path != null && path.Count > 0) DrawPath(path);
            else DrawFailsafeLine();
        }
        StartPathfinding(usedAlgorithm); 
    }

    public void SelectLPAStar() 
    { 
        usedAlgorithm = "LPA*";
        LPAStarPathfinder pathfinder = FindFirstObjectByType<LPAStarPathfinder>();
        
        if (pathfinder != null && player != null && destination != null)
        {
            List<Node> path = pathfinder.FindPath(player.transform.position, destination.transform.position);
            if (path != null && path.Count > 0) DrawPath(path);
            else DrawFailsafeLine();
        }
        StartPathfinding(usedAlgorithm); 
    }

    public void SelectDLite() 
    { 
        usedAlgorithm = "D* Lite";
        DStarLitePathfinder pathfinder = FindFirstObjectByType<DStarLitePathfinder>();
        
        if (pathfinder != null && player != null && destination != null)
        {
            List<Node> path = pathfinder.FindPath(player.transform.position, destination.transform.position);
            if (path != null && path.Count > 0) DrawPath(path);
            else DrawFailsafeLine();
        }
        StartPathfinding(usedAlgorithm); 
    }

    // ==========================================
    // PATH DRAWING & VISUALS
    // ==========================================

    void DrawFailsafeLine()
    {
        if (pathRenderer == null) return;
        pathRenderer.positionCount = 2;
        pathRenderer.SetPosition(0, player.transform.position + Vector3.up * 0.5f);
        pathRenderer.SetPosition(1, destination.transform.position + Vector3.up * 0.5f);
    }

    void DrawPath(List<Node> path)
    {
        if (path == null || pathRenderer == null) return;
        
        pathRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            pathRenderer.SetPosition(i, path[i].worldPosition + Vector3.up * 0.5f);
        }
    }

    void StartPathfinding(string algoName)
    {
        if (algorithmWindow != null) algorithmWindow.SetActive(false);
        ResumeGame(); // Unpauses and locks cursor

        Debug.Log("Starting 60-second visualization for: " + algoName);
        if (pathVisibilityCoroutine != null) StopCoroutine(pathVisibilityCoroutine);
        pathVisibilityCoroutine = StartCoroutine(ShowPathForDuration(60f));
    }

    IEnumerator ShowPathForDuration(float duration)
    {
        if (pathRenderer != null)
        {
            pathRenderer.enabled = true; 
            yield return new WaitForSecondsRealtime(duration); 
            pathRenderer.enabled = false;
        }
    }

    // ==========================================
    // --- NEW: END GAME & SCENE MANAGEMENT ---
    // ==========================================
    void EndLevel()
    {
        if (levelEndWindow != null) levelEndWindow.SetActive(true);
        
        // Populate the dynamic stats!
        if (runStatsText != null)
        {
            runStatsText.text = $"Labyrinth Conquered!\n\nPuzzles Collected: {collectedPuzzles}/5\nAssisted By: {usedAlgorithm}";
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void ReplayLevel()
    {
        shouldReplaySameSeed = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(1); // Explicitly load Gameplay scene
    }

    public void NextMap()
    {
        shouldReplaySameSeed = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(1); // Explicitly load Gameplay scene
    }
}