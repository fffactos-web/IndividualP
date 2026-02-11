using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ProceduralFloorGenerator : MonoBehaviour
{
    [Header("Grid")]
    public int width = 40;
    public int depth = 40;
    public int cellSize = 100;

    [Header("Map size")]
    [Tooltip("If enabled, map grid dimensions are derived from world size and cell size.")]
    public bool useWorldSize = false;
    [Min(1)] public int mapSizeX = 4000;
    [Min(1)] public int mapSizeZ = 4000;

    [Header("Height")]
    [Min(1)] public int levelsCount = 6;
    public int heightStep = 50;
    [Range(0f, 1f)] public float rampRaiseChance = 0.35f;
    public int seed = 0;

    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject rampPrefab;

    public float rampModelLength = 1f;
    public float rampModelHeight = 1f;
    public float rampModelWidth = 1f;

    [Header("Placement controls")]
    public float[] lateralOffsets = new float[] { 0f, 0.25f, -0.25f, 0.5f, -0.5f };
    [Min(1)] public int maxHeightLevelsPerSegment = 2;
    public int maxPlacementAttempts = 5;
    [Tooltip("Do not remove base tile by default. Enable only if you need a clean ramp opening.")]
    public bool replaceTileWithRamp = false;

    [Header("Options")]
    public bool generateOnStart = true;

    private struct RampRequest
    {
        public int lowX;
        public int lowZ;
        public int lowLevel;
        public int highX;
        public int highZ;
        public int highLevel;

        public RampRequest(int lowX, int lowZ, int lowLevel, int highX, int highZ, int highLevel)
        {
            this.lowX = lowX;
            this.lowZ = lowZ;
            this.lowLevel = lowLevel;
            this.highX = highX;
            this.highZ = highZ;
            this.highLevel = highLevel;
        }
    }

    // internal
    private int[,] levelMap;
    private HashSet<Vector2Int> tilePositions = new HashSet<Vector2Int>();
    private List<GameObject> spawned = new List<GameObject>();
    private List<Bounds> rampBounds = new List<Bounds>();
    private int activeWidth;
    private int activeDepth;

    [ContextMenu("Generate")]
    public void Generate()
    {
        ClearAll();
        ApplyMapSizeSettings();

        if (seed == 0)
            seed = Random.Range(-1000000, 1000000);

        Random.InitState(seed);
        ResolveMapDimensions();

        int safeLevelsCount = Mathf.Max(1, levelsCount);
        int totalCells = activeWidth * activeDepth;

        levelMap = new int[activeWidth, activeDepth];
        bool[,] visited = new bool[activeWidth, activeDepth];
        for (int x = 0; x < activeWidth; x++)
            for (int z = 0; z < activeDepth; z++)
                levelMap[x, z] = -1;

        // 1) Pick random point C.
        Vector2Int c = new Vector2Int(Random.Range(0, activeWidth), Random.Range(0, activeDepth));

        // Search order starts from C as requested.
        List<Vector2Int> visitedOrder = new List<Vector2Int>(totalCells) { c };
        List<RampRequest> rampRequests = new List<RampRequest>();

        visited[c.x, c.y] = true;
        levelMap[c.x, c.y] = 0;

        int visitedCount = 1;
        Vector2Int current = c;

        // 2..5) Random walk with restarts from cells reachable from C order.
        while (visitedCount < totalCells)
        {
            var neighbors = GetUnvisitedNeighbors(current, visited);
            if (neighbors.Count > 0)
            {
                Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                int currentLevel = levelMap[current.x, current.y];

                int nextLevel = currentLevel;
                if (Random.value > rampRaiseChance && currentLevel < safeLevelsCount - 1)
                    nextLevel = currentLevel + 1;

                visited[next.x, next.y] = true;
                levelMap[next.x, next.y] = nextLevel;
                visitedOrder.Add(next);
                visitedCount++;

                if (nextLevel > currentLevel)
                {
                    rampRequests.Add(new RampRequest(
                        current.x,
                        current.y,
                        currentLevel,
                        next.x,
                        next.y,
                        nextLevel));
                }

                current = next;
                continue;
            }

            bool foundContinuation = false;
            for (int i = 0; i < visitedOrder.Count; i++)
            {
                Vector2Int candidate = visitedOrder[i];
                if (GetUnvisitedNeighbors(candidate, visited).Count == 0)
                    continue;

                current = candidate;
                foundContinuation = true;
                break;
            }

            if (!foundContinuation)
                break;
        }

        // Spawn tiles for every filled map cell.
        for (int x = 0; x < activeWidth; x++)
        {
            for (int z = 0; z < activeDepth; z++)
            {
                int lvl = Mathf.Max(0, levelMap[x, z]);
                levelMap[x, z] = lvl;

                if (tilePrefab == null)
                    continue;

                Vector3 worldPos = GridToWorld(x, z, lvl);
                GameObject t = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                t.name = $"Tile [{x},{z}] L{lvl}";
                spawned.Add(t);
                tilePositions.Add(new Vector2Int(x, z));
            }
        }

        if (rampPrefab == null)
            return;

        foreach (var request in rampRequests)
        {
            BuildRampPathFromLowToHigh(
                request.lowX,
                request.lowZ,
                request.lowLevel,
                request.highX,
                request.highZ,
                request.highLevel);
        }
    }

    List<Vector2Int> GetUnvisitedNeighbors(Vector2Int pos, bool[,] visited)
    {
        List<Vector2Int> result = new List<Vector2Int>(4);
        foreach (var d in Neighbors4())
        {
            int nx = pos.x + d.x;
            int nz = pos.y + d.y;
            if (!InBounds(nx, nz) || visited[nx, nz])
                continue;

            result.Add(new Vector2Int(nx, nz));
        }

        return result;
    }

    /// <summary>
    /// Builds one or more ramp segments from a low tile to an adjacent higher tile.
    /// Path is committed only when all segments are valid, preventing dangling ramps.
    /// </summary>
    void BuildRampPathFromLowToHigh(int lowX, int lowZ, int lowL, int highX, int highZ, int highL)
    {
        int levelDiff = Mathf.Abs(highL - lowL);
        if (levelDiff <= 0)
            return;

        int maxLevelsPerSegment = Mathf.Max(1, maxHeightLevelsPerSegment);
        int segments = Mathf.CeilToInt((float)levelDiff / maxLevelsPerSegment);
        segments = Mathf.Max(1, segments);

        int remaining = levelDiff;
        int currentLowLevel = lowL;

        Vector3 lowCenter = GridToWorld(lowX, lowZ, lowL);
        Vector3 highCenter = GridToWorld(highX, highZ, highL);
        Vector3 dirWorld = (highCenter - lowCenter);
        dirWorld.y = 0f;
        dirWorld.Normalize();
        Vector3 perp = Vector3.Cross(Vector3.up, dirWorld).normalized;

        List<GameObject> pathRamps = new List<GameObject>();
        List<Bounds> pathBounds = new List<Bounds>();

        bool failed = false;

        for (int s = 0; s < segments; s++)
        {
            int partLevels = Mathf.Min(remaining, Mathf.CeilToInt((float)levelDiff / segments));
            int segLowLevel = currentLowLevel;
            int segHighLevel = currentLowLevel + partLevels;

            float targetVertical = partLevels * heightStep;
            float targetHorizontal = (float)cellSize / segments;

            bool placed = false;

            for (int attempt = 0; attempt < Mathf.Min(maxPlacementAttempts, lateralOffsets.Length); attempt++)
            {
                float lateral = lateralOffsets[attempt];
                Vector3 basePos = GridToWorld(lowX, lowZ, segLowLevel) + dirWorld * (s * targetHorizontal) + perp * (lateral * cellSize);
                basePos.y = segLowLevel * heightStep;

                Vector3 startAnchor = basePos;
                Vector3 endAnchor = basePos + dirWorld * targetHorizontal;
                endAnchor.y = segHighLevel * heightStep;

                if (!HasAnchorAt(startAnchor, segLowLevel, pathBounds))
                    continue;

                bool isFinalSegment = s == segments - 1;
                if (isFinalSegment && !HasAnchorAt(endAnchor, segHighLevel, pathBounds))
                    continue;

                Quaternion rot = Quaternion.LookRotation(dirWorld, Vector3.up);

                float scaleX = targetHorizontal / Mathf.Max(1e-5f, rampModelLength);
                float scaleY = targetVertical / Mathf.Max(1e-5f, rampModelHeight);
                float scaleZ = cellSize / Mathf.Max(1e-5f, rampModelWidth);

                Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);

                var r = Instantiate(rampPrefab, basePos, rot, transform);
                r.transform.localScale = scale;
                r.name = $"Ramp [{lowX},{lowZ}]->[{highX},{highZ}] seg{s} L{segLowLevel}->{segHighLevel}";

                Bounds b = CalcBoundsRecursive(r);
                b.Expand(0.01f);

                bool intersects = false;
                foreach (var pb in rampBounds)
                {
                    if (pb.Intersects(b))
                    {
                        intersects = true;
                        break;
                    }
                }

                if (!intersects)
                {
                    foreach (var pb in pathBounds)
                    {
                        if (pb.Intersects(b))
                        {
                            intersects = true;
                            break;
                        }
                    }
                }

                if (!intersects)
                {
                    pathBounds.Add(b);
                    pathRamps.Add(r);
                    placed = true;
                    break;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(r);
                else Destroy(r);
#else
                Destroy(r);
#endif
            }

            if (!placed)
            {
                failed = true;
                break;
            }

            remaining -= partLevels;
            currentLowLevel = segHighLevel;
        }

        if (failed || remaining > 0)
        {
            foreach (var r in pathRamps)
            {
                if (r == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(r);
                else Destroy(r);
#else
                Destroy(r);
#endif
            }
            return;
        }

        rampBounds.AddRange(pathBounds);
        spawned.AddRange(pathRamps);

        if (replaceTileWithRamp)
            RemoveTileAt(lowX, lowZ);
    }

    bool HasAnchorAt(Vector3 worldPos, int expectedLevel)
    {
        return HasAnchorAt(worldPos, expectedLevel, null);
    }

    bool HasAnchorAt(Vector3 worldPos, int expectedLevel, List<Bounds> extraBounds)
    {
        int safeCellSize = Mathf.Max(1, cellSize);
        int tileX = Mathf.RoundToInt(worldPos.x / safeCellSize);
        int tileZ = Mathf.RoundToInt(worldPos.z / safeCellSize);
        Vector2Int gridPos = new Vector2Int(tileX, tileZ);

        if (!InBounds(tileX, tileZ) || levelMap[tileX, tileZ] != expectedLevel)
            return false;

        if (tilePositions.Contains(gridPos))
            return true;

        float expectedY = expectedLevel * heightStep;
        const float anchorToleranceY = 0.1f;

        foreach (var b in rampBounds)
        {
            if (b.min.x <= worldPos.x && b.max.x >= worldPos.x &&
                b.min.z <= worldPos.z && b.max.z >= worldPos.z &&
                b.min.y - anchorToleranceY <= expectedY && b.max.y + anchorToleranceY >= expectedY)
            {
                return true;
            }
        }

        if (extraBounds != null)
        {
            foreach (var b in extraBounds)
            {
                if (b.min.x <= worldPos.x && b.max.x >= worldPos.x &&
                    b.min.z <= worldPos.z && b.max.z >= worldPos.z &&
                    b.min.y - anchorToleranceY <= expectedY && b.max.y + anchorToleranceY >= expectedY)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void RemoveTileAt(int tx, int tz)
    {
        Vector3 pos = GridToWorld(tx, tz, levelMap[tx, tz]);
        foreach (var go in new List<GameObject>(spawned))
        {
            if (go == null) continue;
            if (!go.name.StartsWith("Tile")) continue;
            if (Vector3.Distance(go.transform.position, pos) < 0.1f)
            {
                spawned.Remove(go);
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(go);
                else Destroy(go);
#else
                Destroy(go);
#endif
                tilePositions.Remove(new Vector2Int(tx, tz));
                break;
            }
        }
    }

    static Bounds CalcBoundsRecursive(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.001f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    Vector3 GridToWorld(int gx, int gz, int level)
    {
        float wx = gx * cellSize;
        float wz = gz * cellSize;
        float wy = level * heightStep;
        return new Vector3(wx, wy, wz);
    }

    static Vector2Int[] Neighbors4()
    {
        return new Vector2Int[]
        {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1)
        };
    }

    void ApplyMapSizeSettings()
    {
        width = Mathf.Max(1, width);
        depth = Mathf.Max(1, depth);
        cellSize = Mathf.Max(1, cellSize);
        mapSizeX = Mathf.Max(1, mapSizeX);
        mapSizeZ = Mathf.Max(1, mapSizeZ);
        levelsCount = Mathf.Max(1, levelsCount);
    }

    void ResolveMapDimensions()
    {
        int safeCellSize = Mathf.Max(1, cellSize);

        if (useWorldSize)
        {
            activeWidth = Mathf.Max(1, Mathf.CeilToInt((float)mapSizeX / safeCellSize));
            activeDepth = Mathf.Max(1, Mathf.CeilToInt((float)mapSizeZ / safeCellSize));
        }
        else
        {
            activeWidth = Mathf.Max(1, width);
            activeDepth = Mathf.Max(1, depth);
        }
    }

    bool InBounds(int x, int z) => x >= 0 && x < activeWidth && z >= 0 && z < activeDepth;

    [ContextMenu("ClearAll")]
    public void ClearAll()
    {
        var cur = new List<Transform>();
        foreach (Transform t in transform) cur.Add(t);
        foreach (var c in cur)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(c.gameObject);
            else Destroy(c.gameObject);
#else
            Destroy(c.gameObject);
#endif
        }
        tilePositions.Clear();
        spawned.Clear();
        rampBounds.Clear();
    }

    private void OnValidate()
    {
        ApplyMapSizeSettings();
    }

    private void Start()
    {
        if (generateOnStart) Generate();
    }
}
