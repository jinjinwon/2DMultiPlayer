using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoSingleton<GridManager>
{
    public static Node[,] grid; // ���� �׸���
    public int gridWidth = 10; // �׸��� �ʺ�
    public int gridHeight = 10; // �׸��� ����
    public float cellSize = 2.56f; // �� ���� ũ��
    public Vector3 gridOffset; // �׸����� ������

    public Tilemap BlockTileMap;

    private void Awake()
    {
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        grid = new Node[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Vector3 cellWorldPosition = new Vector3(x, y , 0);

                bool isWalkable = CheckTileWalkable(cellWorldPosition);

                if(isWalkable == false)
                {
                    Debug.Log($"x : {x} y : {y} IsWalkable : false");
                }    
                grid[x, y] = new Node(gridPosition, isWalkable);
            }
        }
    }

    private bool CheckTileWalkable(Vector3 position)
    {
        TileBase tile = BlockTileMap.GetTile(BlockTileMap.WorldToCell(position));
        return !(tile != null);
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.green;

        //for (int x = 0; x < gridWidth; x++)
        //{
        //    for (int y = 0; y < gridHeight; y++)
        //    {
        //        Vector3 cellPosition = new Vector3(x ,y);
        //        Gizmos.DrawWireCube(cellPosition, new Vector3(cellSize, cellSize, 0)); 
        //    }
        //}
    }

    // �̵� ������ ������ ��ġ ��ȯ
    public Vector2Int GetRandomWalkablePosition()
    {
        Vector2Int randomPosition;
        do
        {
            randomPosition = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
        }
        while (!GridCheck(randomPosition));

        return randomPosition;
    }

    public bool GridCheck(Vector2Int vector)
    {
        foreach(Node vec in grid)
        {
            if(vec.isWalkable == true)
            {
                return true;
            }
        }
        return false;
    }
}
