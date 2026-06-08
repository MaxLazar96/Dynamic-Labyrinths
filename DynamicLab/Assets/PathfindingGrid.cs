using UnityEngine;
using System.Collections.Generic;

public class PathfindingGrid : MonoBehaviour 
{
    public Vector2 gridWorldSize = new Vector2(100, 100);
    public float nodeRadius = 0.5f;
    Node[,] grid;

    public void CreateGrid() 
    {
        int gridSizeX = Mathf.RoundToInt(gridWorldSize.x / (nodeRadius * 2));
        int gridSizeY = Mathf.RoundToInt(gridWorldSize.y / (nodeRadius * 2));
        grid = new Node[gridSizeX, gridSizeY];

        MapGenerator mapGen = FindFirstObjectByType<MapGenerator>();
        bool[,] logicalGrid = mapGen != null ? mapGen.GetGrid() : null;

        for (int x = 0; x < gridSizeX; x++) 
        {
            for (int y = 0; y < gridSizeY; y++) 
            {
                Vector3 worldPoint = transform.position + Vector3.right * (x * 1f - 50f + 0.5f) + Vector3.forward * (y * 1f - 50f + 0.5f);
                
                bool walkable = true;
                if (logicalGrid != null && x < logicalGrid.GetLength(0) && y < logicalGrid.GetLength(1)) 
                {
                    walkable = !logicalGrid[x, y];
                }
                
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public List<Node> GetNeighbors(Node node) 
    {
        List<Node> neighbors = new List<Node>();
        for (int x = -1; x <= 1; x++) 
        {
            for (int y = -1; y <= 1; y++) 
            {
                if (x == 0 && y == 0) continue;
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                if (checkX >= 0 && checkX < 100 && checkY >= 0 && checkY < 100) 
                    neighbors.Add(grid[checkX, checkY]);
            }
        }
        return neighbors;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition) 
    {
        // THE FIX: Perfect World-to-Grid mathematical mapping. No more 1% coordinate drift!
        int x = Mathf.FloorToInt(worldPosition.x + 50f);
        int y = Mathf.FloorToInt(worldPosition.z + 50f);
        
        // Clamp to prevent out-of-bounds array errors
        x = Mathf.Clamp(x, 0, Mathf.RoundToInt(gridWorldSize.x) - 1);
        y = Mathf.Clamp(y, 0, Mathf.RoundToInt(gridWorldSize.y) - 1);
        
        return grid[x, y];
    }

    public Node[,] GetGrid() { return grid; }
}