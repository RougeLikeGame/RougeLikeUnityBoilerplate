using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PathGrid : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(20f, 12f);
    [SerializeField] private float cellSize = 0.5f;
    [Tooltip("Layers that block walking/the obstleves and wall layers")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("rough raduis of the enemies that uses this grid and the paths keep this far from obstcles")]
    [SerializeField] private float agentRadius = 0.35f;
    [SerializeField] private int maxSearchNodes = 4000;
    [SerializeField] private bool drawGizmos = true;
    public LayerMask ObstacleMask => obstacleMask;
    public float AgentRadius => agentRadius;
    private bool[] walkable;
    private int width, height;
    private Vector2 origin; //bottom-left corner
    //the A* working arrays 
    private float[] gCost;
    private int[] parent;
    private bool[] closed;
    private readonly List<int> heap = new List<int>();
    private float[] fCost;
    private readonly List<Vector2> rawPath = new List<Vector2>();
    private static readonly int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

    private void Awake() => Rebuild();

    //it is the re-scan the obstacles and call this if obstacles are moved or destroyed 
    public void Rebuild()
    {
        width = Mathf.Max(1, Mathf.CeilToInt(size.x / cellSize));
        height = Mathf.Max(1, Mathf.CeilToInt(size.y / cellSize));
        origin = (Vector2)transform.position - size * 0.5f;

        int n = width * height;
        walkable = new bool[n];
        gCost = new float[n];
        fCost = new float[n];
        parent = new int[n];
        closed = new bool[n];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                walkable[y * width + x] = Physics2D.OverlapCircle(CellCenter(x, y), agentRadius, obstacleMask) == null;
    }

    //True if a body of this radius can move straight from a to b without touching an obstacle
    public bool IsClear(Vector2 a, Vector2 b, float radius)
    {
        Vector2 d = b - a;
        float dist = d.magnitude;
        if (dist < 0.001f) return true;
        return Physics2D.CircleCast(a, radius, d / dist, dist, obstacleMask).collider == null;
    }

    //finds a path from start to goal. Fills "path" with world points (not including the start) Returns false if there's no path (path is left empty).
    public bool FindPath(Vector2 start, Vector2 goal, List<Vector2> path)
    {
        path.Clear();
        if (walkable == null) Rebuild();

        int s = NearestWalkable(WorldToCell(start));
        int g = NearestWalkable(WorldToCell(goal));
        if (s < 0 || g < 0) return false;

        for (int i = 0; i < gCost.Length; i++)
        {
            gCost[i] = float.MaxValue;
            closed[i] = false;
            parent[i] = -1;
        }
        heap.Clear();

        gCost[s] = 0f;
        fCost[s] = Heuristic(s, g);
        Push(s);

        int expanded = 0;
        bool found = false;
        while (heap.Count > 0 && expanded < maxSearchNodes)
        {
            int cur = Pop();
            if (closed[cur]) continue;
            closed[cur] = true;
            expanded++;
            if (cur == g) { found = true; break; }

            int cx = cur % width, cy = cur / width;
            for (int k = 0; k < 8; k++)
            {
                int nx = cx + dx[k], ny = cy + dy[k];
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                int ni = ny * width + nx;
                if (!walkable[ni] || closed[ni]) continue;

                bool diagonal = k >= 4;
                //both side cells must be open for a diagonal step.
                if (diagonal && (!walkable[cy * width + nx] || !walkable[ny * width + cx])) continue;

                float step = diagonal ? 1.41421f : 1f;
                float ng = gCost[cur] + step;
                if (ng < gCost[ni])
                {
                    gCost[ni] = ng;
                    parent[ni] = cur;
                    fCost[ni] = ng + Heuristic(ni, g);
                    Push(ni);
                }
            }
        }

        if (!found) return false;

        //Walk back from the goal to build the raw path.
        rawPath.Clear();
        for (int c = g; c != -1 && c != s; c = parent[c])
            rawPath.Add(CellCenter(c % width, c / width));
        rawPath.Reverse();
        if (rawPath.Count > 0 && walkable[WorldToCellClamped(goal)]) rawPath[rawPath.Count - 1] = goal;

        Smooth(start, path);
        return path.Count > 0;
    }

    //String pulling: keep only the waypoints we can't skip with a clear straight line
    private void Smooth(Vector2 start, List<Vector2> path)
    {
        Vector2 from = start;
        int i = 0;
        while (i < rawPath.Count)
        {
            int furthest = i;
            for (int j = rawPath.Count - 1; j > i; j--)
            {
                if (IsClear(from, rawPath[j], agentRadius * 0.9f)) { furthest = j; break; }
            }
            path.Add(rawPath[furthest]);
            from = rawPath[furthest];
            i = furthest + 1;
        }
    }

    private float Heuristic(int a, int b)
    {
        int ax = a % width, ay = a / width, bx = b % width, by = b / width;
        int ddx = Mathf.Abs(ax - bx), ddy = Mathf.Abs(ay - by);
        return (ddx + ddy) + (1.41421f - 2f) * Mathf.Min(ddx, ddy); //octile distance
    }

    private Vector2 CellCenter(int x, int y) => origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

    private int WorldToCell(Vector2 p)
    {
        int x = Mathf.FloorToInt((p.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((p.y - origin.y) / cellSize);
        if (x < 0 || y < 0 || x >= width || y >= height) return WorldToCellClamped(p);
        return y * width + x;
    }

    private int WorldToCellClamped(Vector2 p)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / cellSize), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((p.y - origin.y) / cellSize), 0, height - 1);
        return y * width + x;
    }

    //If a cell is blocked (e.g. the target is hugging a rock), use the closest open cell
    private int NearestWalkable(int cell)
    {
        if (walkable[cell]) return cell;
        int cx = cell % width, cy = cell / width;
        for (int r = 1; r <= 4; r++)
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    int i = y * width + x;
                    if (walkable[i]) return i;
                }
        return -1;
    }

    //Small binary min-heap ordered by fCost.
    private void Push(int cell)
    {
        heap.Add(cell);
        int i = heap.Count - 1;
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (fCost[heap[p]] <= fCost[heap[i]]) break;
            int t = heap[p]; heap[p] = heap[i]; heap[i] = t;
            i = p;
        }
    }

    private int Pop()
    {
        int top = heap[0];
        int last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        if (heap.Count > 0)
        {
            heap[0] = last;
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < heap.Count && fCost[heap[l]] < fCost[heap[m]]) m = l;
                if (r < heap.Count && fCost[heap[r]] < fCost[heap[m]]) m = r;
                if (m == i) break;
                int t = heap[m]; heap[m] = heap[i]; heap[i] = t;
                i = m;
            }
        }
        return top;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));

        if (walkable == null) return;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!walkable[y * width + x])
                    Gizmos.DrawCube(CellCenter(x, y), new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0f));
    }
}