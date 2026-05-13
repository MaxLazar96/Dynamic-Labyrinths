using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("UI Reference")]
    public GameObject levelEndWindow; 
    public TextMeshProUGUI scoreText;

    [Header("Algorithm UI")]
    public GameObject algorithmWindow; 
    public LineRenderer pathRenderer; 

    [Header("Player & Destination References")]
    public GameObject player;      
    public GameObject destination; 

    [Header("Seed Settings")]
    public static int lastUsedSeed;
    public static bool shouldReplaySameSeed = false;

    private int collectedPuzzles = 0;
    private Coroutine pathVisibilityCoroutine;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
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

        collectedPuzzles = 0;
        UpdateScoreUI();
        
        if (levelEndWindow != null) levelEndWindow.SetActive(false);
        if (algorithmWindow != null) algorithmWindow.SetActive(false);
        
        if (pathRenderer != null) pathRenderer.enabled = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

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
        print("SelectARAStar Selected");
        ARAStarPathfinder pathfinder = FindFirstObjectByType<ARAStarPathfinder>();
        
        if (pathfinder != null && player != null && destination != null)
        {
            List<Node> path = pathfinder.FindPath(player.transform.position, destination.transform.position);
            
            if (path != null && path.Count > 0)
            {
                DrawPath(path);
            }
            else
            {
                Debug.LogWarning("ARA* failed to find path. Drawing direct line failsafe.");
                DrawFailsafeLine();
            }
        }
        else
        {
            Debug.LogError("ARAStarPathfinder script is missing from the scene!");
        }
        
        StartPathfinding("ARA*"); 
    }

    public void SelectLPAStar() 
    { 
        print("SelectLPAStar Selected");
        LPAStarPathfinder pathfinder = FindFirstObjectByType<LPAStarPathfinder>();
        
        if (pathfinder != null && player != null && destination != null)
        {
            List<Node> path = pathfinder.FindPath(player.transform.position, destination.transform.position);
            
            if (path != null && path.Count > 0)
            {
                DrawPath(path);
            }
            else
            {
                Debug.LogWarning("LPA* failed to find path. Drawing direct line failsafe.");
                DrawFailsafeLine();
            }
        }
        else
        {
            Debug.LogError("LPAStarPathfinder script is missing from the scene!");
        }
        
        StartPathfinding("LPA*"); 
    }

    public void SelectDLite() 
    { 
        print("SelectDLite Selected");
        DStarLitePathfinder pathfinder = FindFirstObjectByType<DStarLitePathfinder>();
        
        if (pathfinder != null && player != null && destination != null)
        {
            List<Node> path = pathfinder.FindPath(player.transform.position, destination.transform.position);
            
            if (path != null && path.Count > 0)
            {
                DrawPath(path);
            }
            else
            {
                Debug.LogWarning("D* Lite failed to find path. Drawing direct line failsafe.");
                DrawFailsafeLine();
            }
        }
        else
        {
            Debug.LogError("DStarLitePathfinder script is missing from the scene!");
        }
        
        StartPathfinding("D* Lite"); 
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
        
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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

    void EndLevel()
    {
        if (levelEndWindow != null) levelEndWindow.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void ReplayLevel()
    {
        shouldReplaySameSeed = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextMap()
    {
        shouldReplaySameSeed = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}