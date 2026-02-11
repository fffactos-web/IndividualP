using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ProceduralFloorGenerator : MonoBehaviour
{
    [Header("Grid")]
    public int width = 40;
    public int depth = 40;
    public int cellSize = 100;

    [Header("Height map")]
    public int levelsCount = 6;
    public int heightStep = 50;
    public float noiseScale = 0.06f;
    public int seed = 0;
    [Min(1)] public int octaves = 4;
    [Range(0.1f, 1f)] public float persistence = 0.5f;
    [Min(1f)] public float lacunarity = 2f;
    public Vector2 noiseOffset;

    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject rampPrefab;

    public float rampModelLength = 1f;
    public float rampModelHeight = 1f;
    public float rampModelWidth = 1f;

    [Header("Placement controls")]
    public float[] lateralOffsets = new float[] { 0f, 0.25f, -0.25f, 0.5f, -0.5f };
    public int maxHeightLevelsPerSegment = 2;
    public int maxPlacementAttempts = 5;
    [Tooltip("Do not remove base tile by default. Enable only if you need a clean ramp opening.")]
    public bool replaceTileWithRamp = false;

    [Header("Options")]
    public bool generateOnStart = true;

    // internal
    private int[,] levelMap;
    private bool[,] highMap;
    private bool[,] visited;
    private HashSet<Vector2Int> tilePositions = new HashSet<Vector2Int>();
    private HashSet<string> edgeSet = new HashSet<string>();
    private List<GameObject> spawned = new List<GameObject>();
    private List<Bounds> rampBounds = new List<Bounds>();

    [ContextMenu("Generate")]
    public void Generate()
    {
        ClearAll();

        if (seed == 0) seed = Random.Range(-1000000, 1000000);
        Random.InitState(seed);

        levelMap = new int[width, depth];
        highMap = new bool[width, depth];

        float ox = Random.Range(0f, 9999f) + noiseOffset.x;
        float oz = Random.Range(0f, 9999f) + noiseOffset.y;

        // 1) Generate discrete height levels from layered Perlin noise.
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                float n = SampleFractalPerlin(x + ox, z + oz);
                int lvl = Mathf.FloorToInt(Mathf.Clamp01(n) * levelsCount);
                if (lvl >= levelsCount) lvl = levelsCount - 1;
                levelMap[x, z] = lvl;
                highMap[x, z] = lvl > 0;
            }

        // 2) Place tiles.
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                Vector3 w = GridToWorld(x, z, levelMap[x, z]);
                if (tilePrefab != null)
                {
                    GameObject t = Instantiate(tilePrefab, w, Quaternion.identity, transform);
                    t.name = $"Tile [{x},{z}] L{levelMap[x,z]}";
                    spawned.Add(t);
                    tilePositions.Add(new Vector2Int(x, z));
                }
            }

        // 3) Find elevated islands and add ramps around their borders.
        visited = new bool[width, depth];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                if (highMap[x, z] && !visited[x, z])
                {
                    var island = FloodFill(x, z);
                    CreateRampsAroundIsland(island);
                }
            }
    }

    List<Vector2Int> FloodFill(int sx, int sz)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(sx, sz));
        visited[sx, sz] = true;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            list.Add(cur);
            foreach (var d in Neighbors4())
            {
                int nx = cur.x + d.x, nz = cur.y + d.y;
                if (InBounds(nx, nz) && highMap[nx, nz] && !visited[nx, nz])
                {
                    visited[nx, nz] = true;
                    q.Enqueue(new Vector2Int(nx, nz));
                }
            }
        }
        return list;
    }

    void CreateRampsAroundIsland(List<Vector2Int> island)
    {
        foreach (var cell in island)
        {
            int cx = cell.x, cz = cell.y;
            int cl = levelMap[cx, cz];

            foreach (var d in Neighbors4())
            {
                int nx = cx + d.x, nz = cz + d.y;
                if (!InBounds(nx, nz)) continue;

                int nl = levelMap[nx, nz];
                if (nl >= cl) continue;
                if (nl != 0) continue;  // only connect island borders to ground level

                int lowX = nx, lowZ = nz, lowL = nl;
                int highX = cx, highZ = cz, highL = cl;
                string key = $"{lowX},{lowZ}->{highX},{highZ}";
                if (edgeSet.Contains(key)) continue;

                BuildRampPathFromLowToHigh(lowX, lowZ, lowL, highX, highZ, highL);

                edgeSet.Add(key);
            }
        }
    }

    /// <summary>
    /// Builds one or more ramp segments from a low tile to an adjacent higher tile.
    /// </summary>
    void BuildRampPathFromLowToHigh(int lowX, int lowZ, int lowL, int highX, int highZ, int highL)
    {
        int levelDiff = Mathf.Abs(highL - lowL);
        if (levelDiff <= 0)
            return;

        int segments = Mathf.CeilToInt((float)levelDiff / maxHeightLevelsPerSegment);
        segments = Mathf.Max(1, segments);

        int remaining = levelDiff;
        int currentLowLevel = lowL;

        Vector3 lowCenter = GridToWorld(lowX, lowZ, lowL);
        Vector3 highCenter = GridToWorld(highX, highZ, highL);
        Vector3 dirWorld = (highCenter - lowCenter);
        dirWorld.y = 0f;
        dirWorld.Normalize();
        Vector3 perp = Vector3.Cross(Vector3.up, dirWorld).normalized;

        for (int s = 0; s < segments; s++)
        {
            int partLevels = Mathf.Min(remaining, Mathf.CeilToInt((float)levelDiff / segments));
            int segLowLevel = currentLowLevel;
            int segHighLevel = currentLowLevel + partLevels;

            float targetVertical = partLevels * heightStep;
            float targetHorizontal = cellSize;

            bool placed = false;

            for (int attempt = 0; attempt < Mathf.Min(maxPlacementAttempts, lateralOffsets.Length); attempt++)
            {
                float lateral = lateralOffsets[attempt];
                Vector3 basePos = GridToWorld(lowX, lowZ, segLowLevel) + perp * (lateral * cellSize);
                basePos.y = segLowLevel * heightStep;

                Quaternion rot = Quaternion.LookRotation(dirWorld, Vector3.up);

                float scaleX = targetHorizontal / Mathf.Max(1e-5f, rampModelLength);
                float scaleY = targetVertical / Mathf.Max(1e-5f, rampModelHeight);
                float scaleZ = cellSize / Mathf.Max(1e-5f, rampModelWidth);

                Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);

                if (!HasSupportUnder(basePos, segLowLevel))
                {
                    continue;
                }

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
                    rampBounds.Add(b);
                    spawned.Add(r);

                    if (replaceTileWithRamp)
                        RemoveTileAt(lowX, lowZ);

                    placed = true;
                    break;
                }
                else
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(r);
                    else Destroy(r);
#else
                Destroy(r);
#endif
                }
            }

            if (!placed)
            {
                // no valid placement for this segment
                break;
            }

            remaining -= partLevels;
            currentLowLevel = segHighLevel;

        }
    }

    float SampleFractalPerlin(float x, float z)
    {
        float scale = Mathf.Max(1e-4f, noiseScale);
        int octaveCount = Mathf.Max(1, octaves);

        float amplitude = 1f;
        float frequency = 1f;
        float total = 0f;
        float normalization = 0f;

        for (int i = 0; i < octaveCount; i++)
        {
            float sampleX = x * scale * frequency;
            float sampleZ = z * scale * frequency;

            total += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
            normalization += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return normalization > 0f ? total / normalization : 0f;
    }

    bool HasSupportUnder(Vector3 worldPos, int level)
    {
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.z / cellSize)
        );

        if (tilePositions.Contains(gridPos))
            return true;

        foreach (var b in rampBounds)
        {
            if (b.min.x <= worldPos.x && b.max.x >= worldPos.x &&
                b.min.z <= worldPos.z && b.max.z >= worldPos.z &&
                b.max.y <= (level + 0.01f) * heightStep)
            {
                return true;
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

    bool InBounds(int x, int z) => x >= 0 && x < width && z >= 0 && z < depth;

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
        edgeSet.Clear();
        spawned.Clear();
        rampBounds.Clear();
    }

    private void Start()
    {
        if (generateOnStart) Generate();
    }
}

