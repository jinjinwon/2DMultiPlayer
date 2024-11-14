using System.Collections.Generic;
using UnityEngine;

public class Pathfind : MonoBehaviour
{
    public Vector2Int startNodePosition;
    public Vector2Int targetNodePosition;

    private List<Node> openList;
    private HashSet<Node> closedList;

    public List<Node> FindPath(Vector2Int start, Vector2Int target)
    {
        Node startNode = GridManager.grid[start.x, start.y];
        Node targetNode = GridManager.grid[target.x, target.y];

        openList = new List<Node> { startNode };
        closedList = new HashSet<Node>();

        while (openList.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openList);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedList.Contains(neighbor))
                {
                    continue;
                }

                int newGCost = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newGCost < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        return null; // 경로를 찾지 못한 경우
    }

    private List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        // 상하좌우 이웃 노드
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighborPos = node.position + direction;
            if (neighborPos.x >= 0 && neighborPos.x < GridManager.grid.GetLength(0) && neighborPos.y >= 0 && neighborPos.y < GridManager.grid.GetLength(1))
            {
                neighbors.Add(GridManager.grid[neighborPos.x, neighborPos.y]);
            }
        }
        return neighbors;
    }

    private int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.position.x - b.position.x);
        int dstY = Mathf.Abs(a.position.y - b.position.y);
        return dstX + dstY; // 맨해튼 거리 계산
    }

    private Node GetLowestFCostNode(List<Node> nodeList)
    {
        Node lowestFCostNode = nodeList[0];
        foreach (Node node in nodeList)
        {
            if (node.FCost < lowestFCostNode.FCost || (node.FCost == lowestFCostNode.FCost && node.hCost < lowestFCostNode.hCost))
            {
                lowestFCostNode = node;
            }
        }
        return lowestFCostNode;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse(); // 시작 지점에서 목표 지점으로 가도록 반전

        return path;
    }
}
