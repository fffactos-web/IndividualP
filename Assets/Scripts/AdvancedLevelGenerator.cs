using UnityEngine;
using System.Collections.Generic;

using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

public enum TileType
{
    Empty,
    Path,
    Ramp
}

public enum Direction
{
    Up,
    Bottom,
    Left,
    Right
}

public class TileData
{
    public Vector3Int position;
    public bool visited;
    public float height;
    public TileType type;
    public Direction moveDirection;

    public TileData(Vector3Int pos)
    {
        position = pos;
        visited = false;
        height = 0f;
        type = TileType.Empty;
        moveDirection = Direction.Up;
    }
}

public class AdvancedLevelGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject pathCell;
    [SerializeField] GameObject rampCell;
    [SerializeField] Transform parentOfPath;
    [SerializeField] GameObject[] utilityPoints;

    [Header("Grid")]
    [SerializeField] int maxX = 20;
    [SerializeField] int maxY = 20;
    [SerializeField] float cellOffset = 2f;

    [Header("Height Settings")]
    [SerializeField] float chanceToGetUp = 0.25f;
    [SerializeField] float heightStep = 1f;

    TileData[,] tiles;

    void Start()
    {
        Generate();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            Generate();
    }

    void Generate()
    {
        Clear();

        InitializeGrid();
        GeneratePath();
        Spawn();
    }

    void Clear()
    {
        for (int i = parentOfPath.childCount - 1; i >= 0; i--)
            Destroy(parentOfPath.GetChild(i).gameObject);
    }

    void InitializeGrid()
    {
        tiles = new TileData[maxX, maxY];

        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                tiles[x, y] = new TileData(new Vector3Int(x, 0, y));
            }
        }
    }

    void InitializeUtilities()
    {

    }

    void GeneratePath()
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();

        Vector2Int start = new Vector2Int(Random.Range(0, maxX), Random.Range(0, maxY));

        tiles[start.x, start.y].visited = true;
        tiles[start.x, start.y].type = TileType.Path;
        tiles[start.x, start.y].height = 0f;

        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current);

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            Shuffle(neighbors);

            bool moved = false;

            foreach (var next in neighbors)
            {
                bool tryRamp = Random.value <= chanceToGetUp;

                if (tryRamp && CanPlaceRamp(current, next))
                {
                    tiles[next.x, next.y].height =
                        tiles[current.x, current.y].height + heightStep;

                    tiles[next.x, next.y].type = TileType.Ramp;
                    SetDirection(current, next);

                    tiles[next.x, next.y].visited = true;
                    stack.Push(next);

                    moved = true;
                    break;
                }
                else
                {
                    tiles[next.x, next.y].height =
                        tiles[current.x, current.y].height;

                    tiles[next.x, next.y].type = TileType.Path;
                    tiles[next.x, next.y].visited = true;
                    stack.Push(next);

                    moved = true;
                    break;
                }
            }

            if (!moved)
                stack.Pop();
        }
    }

    List<Vector2Int> GetUnvisitedNeighbors(Vector2Int pos)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        Vector2Int[] dirs =
        {
            new Vector2Int(0,1),
            new Vector2Int(0,-1),
            new Vector2Int(-1,0),
            new Vector2Int(1,0)
        };

        foreach (var d in dirs)
        {
            Vector2Int check = pos + d;

            if (IsInside(check) && !tiles[check.x, check.y].visited)
                result.Add(check);
        }

        return result;
    }

    bool CanPlaceRamp(Vector2Int current, Vector2Int next)
    {
        Vector2Int delta = next - current;
        Vector2Int forward = next + delta;

        if (!IsInside(forward))
            return false;

        if (tiles[forward.x, forward.y].visited)
            return false;

        return true;
    }

    void SetDirection(Vector2Int current, Vector2Int next)
    {
        Vector2Int delta = next - current;

        if (delta.y > 0)
            tiles[next.x, next.y].moveDirection = Direction.Up;
        else if (delta.y < 0)
            tiles[next.x, next.y].moveDirection = Direction.Bottom;
        else if (delta.x < 0)
            tiles[next.x, next.y].moveDirection = Direction.Left;
        else if (delta.x > 0)
            tiles[next.x, next.y].moveDirection = Direction.Right;
    }

    void Spawn()
    {
        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                if (tiles[x, y].type == TileType.Empty)
                    continue;

                GameObject prefab =
                    tiles[x, y].type == TileType.Ramp ? rampCell : pathCell;

                GameObject obj = Instantiate(prefab, parentOfPath);
                obj.layer = 9;
                obj.GetComponentInChildren<Transform>().gameObject.layer = 9;

                obj.transform.position = new Vector3(
                    x * cellOffset,
                    tiles[x, y].height,
                    y * cellOffset
                );

                if (tiles[x, y].type == TileType.Ramp)
                {
                    switch (tiles[x, y].moveDirection)
                    {
                        case Direction.Up:
                            obj.transform.rotation = Quaternion.Euler(0, 0, 0);
                            break;
                        case Direction.Bottom:
                            obj.transform.rotation = Quaternion.Euler(0, 180, 0);
                            break;
                        case Direction.Left:
                            obj.transform.rotation = Quaternion.Euler(0, -90, 0);
                            break;
                        case Direction.Right:
                            obj.transform.rotation = Quaternion.Euler(0, 90, 0);
                            break;
                    }
                }
            }
        }
    }

    bool IsInside(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < maxX &&
               pos.y >= 0 && pos.y < maxY;
    }

    void Shuffle(List<Vector2Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
