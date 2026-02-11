using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ProceduralFloorGenerator : MonoBehaviour
{
    [Header("Grid")]
    [Min(1)] public int width = 40;
    [Min(1)] public int depth = 40;
    [Min(1)] public int cellSize = 100;

    [Header("Map size")]
    [Tooltip("If enabled, map dimensions are computed from world size and cell size.")]
    public bool useWorldSize = false;
    [Min(1)] public int mapSizeX = 4000;
    [Min(1)] public int mapSizeZ = 4000;

    [Header("Height")]
    [Min(1)] public int levelsCount = 6;
    public int heightStep = 50;
    [Range(0f, 1f)]
    [Tooltip("If random value is greater than this threshold, next step raises height by one level.")]
    public float rampRaiseThreshold = 0.65f;
    public int seed = 0;

    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject rampPrefab;

    [Header("Ramp model")]
    public float rampModelLength = 1f;
    public float rampModelHeight = 1f;
    public float rampModelWidth = 1f;

    [Header("Ramp placement")]
    public float[] lateralOffsets = { 0f, 0.25f, -0.25f, 0.5f, -0.5f };
    [Min(1)] public int maxHeightLevelsPerSegment = 2;
    [Min(1)] public int maxPlacementAttempts = 5;
    public bool replaceTileWithRamp = false;

    [Header("Options")]
    public bool generateOnStart = true;

    private struct RampRequest
    {
        public readonly Vector2Int low;
        public readonly Vector2Int high;
        public readonly int lowLevel;
        public readonly int highLevel;

        public RampRequest(Vector2Int low, Vector2Int high, int lowLevel, int highLevel)
        {
            this.low = low;
            this.high = high;
            this.lowLevel = lowLevel;
            this.highLevel = highLevel;
        }
    }

    private int[,] levelMap;
    private int activeWidth;
    private int activeDepth;

    private readonly HashSet<Vector2Int> tilePositions = new HashSet<Vector2Int>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<Bounds> rampBounds = new List<Bounds>();

    private static readonly Vector2Int[] NeighborDirs =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    [ContextMenu("Generate")]
    public void Generate()
    {
        ClearAll();
        SanitizeSettings();
        ResolveMapDimensions();

        int randomSeed = seed == 0 ? Random.Range(-1000000, 1000000) : seed;
        Random.InitState(randomSeed);

        levelMap = new int[activeWidth, activeDepth];
        bool[,] visited = new bool[activeWidth, activeDepth];

        for (int x = 0; x < activeWidth; x++)
            for (int z = 0; z < activeDepth; z++)
                levelMap[x, z] = -1;

        int targetCellCount = activeWidth * activeDepth;
        int maxLevel = Mathf.Max(1, levelsCount) - 1;

        Vector2Int startC = new Vector2Int(Random.Range(0, activeWidth), Random.Range(0, activeDepth));

        List<Vector2Int> progressionOrder = new List<Vector2Int>(targetCellCount);
        List<RampRequest> rampRequests = new List<RampRequest>();

        progressionOrder.Add(startC);
        visited[startC.x, startC.y] = true;
        levelMap[startC.x, startC.y] = 0;

        int visitedCount = 1;
        Vector2Int current = startC;

        while (visitedCount < targetCellCount)
        {
            Vector2Int next;
            if (TryGetRandomUnvisitedNeighbor(current, visited, out next))
            {
                int currentLevel = levelMap[current.x, current.y];
                int nextLevel = currentLevel;

                float roll = Random.value;
                if (roll > rampRaiseThreshold && currentLevel < maxLevel)
                    nextLevel = currentLevel + 1;

                visited[next.x, next.y] = true;
                levelMap[next.x, next.y] = nextLevel;
                progressionOrder.Add(next);
                visitedCount++;

                if (nextLevel > currentLevel)
                    rampRequests.Add(new RampRequest(current, next, currentLevel, nextLevel));

                current = next;
                continue;
            }

            if (!TryFindContinuationFromC(progressionOrder, visited, out current))
                break;
        }

        SpawnTiles();
        SpawnRamps(rampRequests);
    }

    private bool TryGetRandomUnvisitedNeighbor(Vector2Int origin, bool[,] visited, out Vector2Int neighbor)
    {
        Vector2Int[] candidates = new Vector2Int[4];
        int count = 0;

        for (int i = 0; i < NeighborDirs.Length; i++)
        {
            Vector2Int n = origin + NeighborDirs[i];
            if (!InBounds(n.x, n.y) || visited[n.x, n.y])
                continue;

            candidates[count] = n;
            count++;
        }

        if (count == 0)
        {
            neighbor = default;
            return false;
        }

        neighbor = candidates[Random.Range(0, count)];
        return true;
    }

    private bool TryFindContinuationFromC(List<Vector2Int> progressionOrder, bool[,] visited, out Vector2Int continuation)
    {
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            Vector2Int candidate = progressionOrder[i];
            if (TryGetRandomUnvisitedNeighbor(candidate, visited, out _))
            {
                continuation = candidate;
                return true;
            }
        }

        continuation = default;
        return false;
    }

    private void SpawnTiles()
    {
        if (tilePrefab == null)
            return;

        for (int x = 0; x < activeWidth; x++)
        {
            for (int z = 0; z < activeDepth; z++)
            {
                int level = Mathf.Max(0, levelMap[x, z]);
                levelMap[x, z] = level;

                Vector3 worldPos = GridToWorld(x, z, level);
                GameObject tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                tile.name = $"Tile [{x},{z}] L{level}";

                spawned.Add(tile);
                tilePositions.Add(new Vector2Int(x, z));
            }
        }
    }

    private void SpawnRamps(List<RampRequest> rampRequests)
    {
        if (rampPrefab == null)
            return;

        for (int i = 0; i < rampRequests.Count; i++)
        {
            RampRequest request = rampRequests[i];
            BuildRampPathFromLowToHigh(
                request.low.x,
                request.low.y,
                request.lowLevel,
                request.high.x,
                request.high.y,
                request.highLevel);
        }
    }

    private void BuildRampPathFromLowToHigh(int lowX, int lowZ, int lowL, int highX, int highZ, int highL)
    {
        int levelDiff = Mathf.Abs(highL - lowL);
        if (levelDiff <= 0)
            return;

        int maxLevelsPerSegment = Mathf.Max(1, maxHeightLevelsPerSegment);
        int segmentCount = Mathf.Max(1, Mathf.CeilToInt((float)levelDiff / maxLevelsPerSegment));

        int remainingLevels = levelDiff;
        int currentLowLevel = lowL;

        Vector3 lowCenter = GridToWorld(lowX, lowZ, lowL);
        Vector3 highCenter = GridToWorld(highX, highZ, highL);
        Vector3 direction = highCenter - lowCenter;
        direction.y = 0f;
        direction.Normalize();

        Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized;

        List<GameObject> tempRamps = new List<GameObject>();
        List<Bounds> tempBounds = new List<Bounds>();

        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            int partLevels = Mathf.Min(remainingLevels, Mathf.CeilToInt((float)levelDiff / segmentCount));
            int segmentLowLevel = currentLowLevel;
            int segmentHighLevel = currentLowLevel + partLevels;

            float horizontalSpan = (float)cellSize / segmentCount;
            float verticalSpan = partLevels * heightStep;

            bool placed = false;
            int attemptCount = Mathf.Min(maxPlacementAttempts, lateralOffsets.Length);

            for (int attempt = 0; attempt < attemptCount; attempt++)
            {
                float lateralOffset = lateralOffsets[attempt];
                Vector3 basePosition = GridToWorld(lowX, lowZ, segmentLowLevel)
                    + direction * (segmentIndex * horizontalSpan)
                    + perpendicular * (lateralOffset * cellSize);
                basePosition.y = segmentLowLevel * heightStep;

                Vector3 startAnchor = basePosition;
                Vector3 endAnchor = basePosition + direction * horizontalSpan;
                endAnchor.y = segmentHighLevel * heightStep;

                if (!HasAnchorAt(startAnchor, segmentLowLevel, tempBounds))
                    continue;

                bool isLast = segmentIndex == segmentCount - 1;
                if (isLast && !HasAnchorAt(endAnchor, segmentHighLevel, tempBounds))
                    continue;

                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                Vector3 scale = new Vector3(
                    horizontalSpan / Mathf.Max(1e-5f, rampModelLength),
                    verticalSpan / Mathf.Max(1e-5f, rampModelHeight),
                    cellSize / Mathf.Max(1e-5f, rampModelWidth));

                GameObject ramp = Instantiate(rampPrefab, basePosition, rotation, transform);
                ramp.transform.localScale = scale;
                ramp.name = $"Ramp [{lowX},{lowZ}]->[{highX},{highZ}] seg{segmentIndex} L{segmentLowLevel}->{segmentHighLevel}";

                Bounds bounds = CalcBoundsRecursive(ramp);
                bounds.Expand(0.01f);

                if (IntersectsAny(bounds, rampBounds) || IntersectsAny(bounds, tempBounds))
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(ramp);
                    else Destroy(ramp);
#else
                    Destroy(ramp);
#endif
                    continue;
                }

                tempRamps.Add(ramp);
                tempBounds.Add(bounds);
                placed = true;
                break;
            }

            if (!placed)
            {
                CleanupObjects(tempRamps);
                return;
            }

            remainingLevels -= partLevels;
            currentLowLevel = segmentHighLevel;
        }

        if (remainingLevels > 0)
        {
            CleanupObjects(tempRamps);
            return;
        }

        rampBounds.AddRange(tempBounds);
        spawned.AddRange(tempRamps);

        if (replaceTileWithRamp)
            RemoveTileAt(lowX, lowZ);
    }

    private static bool IntersectsAny(Bounds bounds, List<Bounds> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Intersects(bounds))
                return true;
        }

        return false;
    }

    private bool HasAnchorAt(Vector3 worldPos, int expectedLevel, List<Bounds> extraBounds)
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
        const float toleranceY = 0.1f;

        if (ContainsYAnchor(rampBounds, worldPos, expectedY, toleranceY))
            return true;

        if (extraBounds != null && ContainsYAnchor(extraBounds, worldPos, expectedY, toleranceY))
            return true;

        return false;
    }

    private static bool ContainsYAnchor(List<Bounds> boundsList, Vector3 worldPos, float expectedY, float toleranceY)
    {
        for (int i = 0; i < boundsList.Count; i++)
        {
            Bounds b = boundsList[i];
            bool containsXZ = b.min.x <= worldPos.x && b.max.x >= worldPos.x
                && b.min.z <= worldPos.z && b.max.z >= worldPos.z;
            bool containsY = b.min.y - toleranceY <= expectedY && b.max.y + toleranceY >= expectedY;

            if (containsXZ && containsY)
                return true;
        }

        return false;
    }

    private void RemoveTileAt(int tx, int tz)
    {
        Vector3 targetPos = GridToWorld(tx, tz, levelMap[tx, tz]);

        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawned[i];
            if (obj == null || !obj.name.StartsWith("Tile"))
                continue;

            if (Vector3.Distance(obj.transform.position, targetPos) > 0.1f)
                continue;

            spawned.RemoveAt(i);
            tilePositions.Remove(new Vector2Int(tx, tz));

#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(obj);
            else Destroy(obj);
#else
            Destroy(obj);
#endif
            break;
        }
    }

    private static void CleanupObjects(List<GameObject> objects)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(objects[i]);
            else Destroy(objects[i]);
#else
            Destroy(objects[i]);
#endif
        }
    }

    private static Bounds CalcBoundsRecursive(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one * 0.001f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private Vector3 GridToWorld(int gx, int gz, int level)
    {
        return new Vector3(gx * cellSize, level * heightStep, gz * cellSize);
    }

    private void SanitizeSettings()
    {
        width = Mathf.Max(1, width);
        depth = Mathf.Max(1, depth);
        cellSize = Mathf.Max(1, cellSize);
        mapSizeX = Mathf.Max(1, mapSizeX);
        mapSizeZ = Mathf.Max(1, mapSizeZ);
        levelsCount = Mathf.Max(1, levelsCount);
        maxHeightLevelsPerSegment = Mathf.Max(1, maxHeightLevelsPerSegment);
        maxPlacementAttempts = Mathf.Max(1, maxPlacementAttempts);

        if (lateralOffsets == null || lateralOffsets.Length == 0)
            lateralOffsets = new float[] { 0f };
    }

    private void ResolveMapDimensions()
    {
        int safeCellSize = Mathf.Max(1, cellSize);

        if (useWorldSize)
        {
            activeWidth = Mathf.Max(1, Mathf.CeilToInt((float)mapSizeX / safeCellSize));
            activeDepth = Mathf.Max(1, Mathf.CeilToInt((float)mapSizeZ / safeCellSize));
            return;
        }

        activeWidth = Mathf.Max(1, width);
        activeDepth = Mathf.Max(1, depth);
    }

    private bool InBounds(int x, int z)
    {
        return x >= 0 && x < activeWidth && z >= 0 && z < activeDepth;
    }

    [ContextMenu("ClearAll")]
    public void ClearAll()
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform)
            children.Add(child);

        for (int i = 0; i < children.Count; i++)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(children[i].gameObject);
            else Destroy(children[i].gameObject);
#else
            Destroy(children[i].gameObject);
#endif
        }

        tilePositions.Clear();
        spawned.Clear();
        rampBounds.Clear();
    }

    private void OnValidate()
    {
        SanitizeSettings();
    }

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }
}
