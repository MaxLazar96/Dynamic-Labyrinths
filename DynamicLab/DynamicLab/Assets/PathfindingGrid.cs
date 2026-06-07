using UnityEngine;
using System.Collections.Generic;

public class PathfindingGrid : MonoBehaviour {
    public LayerMask obstacleMask;
    public Vector2 gridWorldSize = new Vector2(100, 100);
    public float nodeRadius = 0.5f;
    Node[,] grid;

    void Awake() {
        CreateGrid();
    }

    public void CreateGrid() {
        int gridSizeX = Mathf.RoundToInt(gridWorldSize.x / (nodeRadius * 2));
        int gridSizeY = Mathf.RoundToInt(gridWorldSize.y / (nodeRadius * 2));
        grid = new Node[gridSizeX, gridSizeY];

        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                // Centering the grid on (0,0,0) for your 100x100 map
                Vector3 worldPoint = transform.position + Vector3.right * (x * 1f - 50f + 0.5f) + Vector3.forward * (y * 1f - 50f + 0.5f);
                // Change the radius slightly to make detection more reliable
bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius * 0.9f, obstacleMask));
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    // This logic allows the algorithm to move in 8 directions (including diagonals)
    public List<Node> GetNeighbors(Node node) {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) continue; // Skip the node itself

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                // Ensure the neighbor is within the 100x100 map bounds
                if (checkX >= 0 && checkX < 100 && checkY >= 0 && checkY < 100) {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition) {
        float percentX = (worldPosition.x + 50f) / 100f;
        float percentY = (worldPosition.z + 50f) / 100f;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        int x = Mathf.RoundToInt((gridWorldSize.x / 1f - 1) * percentX);
        int y = Mathf.RoundToInt((gridWorldSize.y / 1f - 1) * percentY);
        return grid[x, y];
    }

    void OnDrawGizmos() {
    if (grid != null) {
        foreach (Node n in grid) {
            Gizmos.color = (n.walkable) ? Color.white : Color.red;
            Gizmos.DrawCube(n.worldPosition, Vector3.one * 0.3f);
        }
    }
}
public Node[,] GetGrid() {
    return grid;
}
}