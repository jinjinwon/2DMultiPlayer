using UnityEngine;

[System.Serializable]
public class Node
{
    public Vector2Int position;
    public bool isWalkable;
    public int gCost;
    public int hCost;
    public Node parent;

    public Node(Vector2Int position, bool isWalkable)
    {
        this.position = position;
        this.isWalkable = isWalkable;
    }

    public int FCost => gCost + hCost; // F 비용 계산
}
