using System.Collections.Generic;
using UnityEngine;

public enum GhostState { Normal, Scared, Recovering, Dead }

[RequireComponent(typeof(Collider2D))]
public class GhostController : MonoBehaviour
{
    [Header("Identity & Refs")]
    [Range(1, 4)] public int ghostId = 1;
    public PacStudentController player;
    public GameRule70 gameRule;

    [Header("Grid")]
    public float cellSize = 1f;
    public Vector2 gridOrigin = new Vector2(0.5f, 0.5f);

    [Header("Movement")]
    public LayerMask wallMask;
    public LayerMask teleporterMask;
    public Vector2 checkHalfExtents = new Vector2(0.40f, 0.40f);
    public float fallbackNormalSpeed = 3.2f;

    [Header("Spawn Area")]
    public Transform spawnCenter;
    public float spawnArriveRadius = 0.3f;
    public Transform topGap;
    public Transform bottomGap;
    public BoxCollider2D spawnBounds;        // 非 Dead 禁止重返
    public Transform respawnPoint;           // 可选：各自复活点

    [Header("Start")]
    public Vector2Int startCell;
    public Vector2Int startFacing = Vector2Int.left;

    // 运行期
    [HideInInspector] public GhostState state = GhostState.Normal;
    [HideInInspector] public bool hasExitedSpawn = false;

    bool isMoving = false;
    Vector3 fromPos, toPos;
    float t = 0f;
    Vector2Int curCell;
    Vector2Int dir;
    Vector2Int prevDir;

    float normalSpeed;
    float scaredSpeed;
    float deadSpeed;

    static readonly Vector2Int[] DIRS = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    void Awake()
    {
        if (!gameRule) gameRule = FindObjectOfType<GameRule70>();
        if (!player) player = FindObjectOfType<PacStudentController>();

        normalSpeed = player ? player.speed * 0.9f : fallbackNormalSpeed;
        scaredSpeed = normalSpeed * 0.5f;
        deadSpeed = scaredSpeed;

        curCell = startCell;
        dir = ClampDir(startFacing);
        prevDir = Vector2Int.zero;
        state = GhostState.Normal;           // 关键：默认 Normal
        transform.position = CellToWorld(curCell);

        // 确保触发器
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (state == GhostState.Dead) { MoveDeadStraight(); return; }

        if (!hasExitedSpawn && state != GhostState.Dead) { MoveOutOfSpawn(); return; }

        if (!isMoving) ChooseNextAndBeginStep();
        else
        {
            float spd = GetCurrentSpeed();
            t += (spd / cellSize) * Time.deltaTime;
            if (t >= 1f) CompleteStep();
            else transform.position = Vector3.Lerp(fromPos, toPos, t);
        }
    }

    // ===== API =====
    public void EnterScared() { if (state != GhostState.Dead) state = GhostState.Scared; }
    public void EnterRecovering() { if (state != GhostState.Dead) state = GhostState.Recovering; }
    public void EnterNormal() { state = GhostState.Normal; }

    public void EnterDead()
    {
        if (state == GhostState.Dead) return;
        state = GhostState.Dead;
        isMoving = false;
        gameRule?.NotifyGhostBecameDead();
    }

    public void ResetToStart()
    {
        state = GhostState.Normal;
        hasExitedSpawn = false;
        curCell = startCell;
        dir = ClampDir(startFacing);
        prevDir = Vector2Int.zero;
        isMoving = false;
        transform.position = CellToWorld(curCell);
    }

    // ===== Movement Core =====
    void ChooseNextAndBeginStep()
    {
        Vector2Int chosen = DecideDirection();
        prevDir = dir;
        dir = chosen;
        var nextCell = curCell + dir;
        fromPos = CellToWorld(curCell);
        toPos = CellToWorld(nextCell);
        t = 0f;
        isMoving = true;
    }

    void CompleteStep()
    {
        transform.position = toPos;
        curCell += dir;
        isMoving = false;
    }

    Vector2Int DecideDirection()
    {
        List<Vector2Int> options = ValidDirsFrom(curCell);

        // 不回头
        Vector2Int back = -prevDir;
        if (prevDir != Vector2Int.zero) options.RemoveAll(d => d == back);
        if (options.Count == 0) return prevDir != Vector2Int.zero ? back : options.Count > 0 ? options[0] : Vector2Int.right;

        if (state == GhostState.Scared || state == GhostState.Recovering)
            return Ghost1(options);

        switch (Mathf.Clamp(ghostId, 1, 4))
        {
            case 1: return Ghost1(options);
            case 2: return Ghost2(options);
            case 3: return Ghost3(options);
            case 4: return Ghost4(options);
        }
        return Ghost3(options);
    }

    Vector2Int Ghost1(List<Vector2Int> options)
    {
        float curDist = DistToPlayer(transform.position);
        List<Vector2Int> good = new();
        foreach (var d in options)
        {
            Vector3 np = CellToWorld(curCell + d);
            if (DistToPlayer(np) >= curDist - 1e-4f) good.Add(d);
        }
        if (good.Count == 0) good = options;
        return good[Random.Range(0, good.Count)];
    }

    Vector2Int Ghost2(List<Vector2Int> options)
    {
        float curDist = DistToPlayer(transform.position);
        List<Vector2Int> good = new();
        foreach (var d in options)
        {
            Vector3 np = CellToWorld(curCell + d);
            if (DistToPlayer(np) <= curDist + 1e-4f) good.Add(d);
        }
        if (good.Count == 0) good = options;
        return good[Random.Range(0, good.Count)];
    }

    Vector2Int Ghost3(List<Vector2Int> options) => options[Random.Range(0, options.Count)];

    Vector2Int Ghost4(List<Vector2Int> options)
    {
        Vector2Int fwd = dir == Vector2Int.zero ? Vector2Int.right : dir;
        Vector2Int right = TurnRight(fwd);
        Vector2Int left = TurnLeft(fwd);
        Vector2Int back = -fwd;

        if (options.Contains(right)) return right;
        if (options.Contains(fwd)) return fwd;
        if (options.Contains(left)) return left;
        return back;
    }

    void MoveOutOfSpawn()
    {
        Transform gap = (ghostId == 1 || ghostId == 3) ? topGap : bottomGap;
        if (!gap) { hasExitedSpawn = true; return; }

        float spd = GetCurrentSpeed();
        Vector3 dirWorld = (gap.position - transform.position).normalized;
        transform.position += dirWorld * spd * Time.deltaTime;

        if (Vector3.Distance(transform.position, gap.position) <= 0.05f)
        {
            hasExitedSpawn = true;
            curCell = WorldToCell(transform.position);
            dir = ClosestDir(dirWorld);
            prevDir = Vector2Int.zero;
            isMoving = false;
            transform.position = CellToWorld(curCell);
        }
    }

    void MoveDeadStraight()
    {
        Vector3 target = respawnPoint ? respawnPoint.position :
                         (spawnCenter ? spawnCenter.position : transform.position);
        Vector3 d = target - transform.position;
        float dist = d.magnitude;

        if (dist <= spawnArriveRadius)
        {
            float left = gameRule ? gameRule.ScaredTimeLeft() : 0f;
            if (left > 3f) EnterScared();
            else if (left > 0f) EnterRecovering();
            else EnterNormal();

            gameRule?.NotifyGhostRevived();
            hasExitedSpawn = false;
            isMoving = false;
            return;
        }

        Vector3 dirWorld = d / Mathf.Max(0.0001f, dist);
        transform.position += dirWorld * deadSpeed * Time.deltaTime;
    }

    float GetCurrentSpeed()
    {
        return state switch
        {
            GhostState.Normal => normalSpeed,
            GhostState.Scared => scaredSpeed,
            GhostState.Recovering => scaredSpeed,
            GhostState.Dead => deadSpeed,
            _ => normalSpeed
        };
    }

    List<Vector2Int> ValidDirsFrom(Vector2Int cell)
    {
        var list = new List<Vector2Int>(4);
        foreach (var d in DIRS)
        {
            Vector2Int nc = cell + d;
            Vector3 wp = CellToWorld(nc);

            // 非 Dead 且已出房间，禁止回到出生区
            if (state != GhostState.Dead && hasExitedSpawn && spawnBounds && spawnBounds.OverlapPoint(wp))
                continue;

            bool hitWall = Physics2D.OverlapBox(wp, checkHalfExtents * 2f, 0f, wallMask);
            if (hitWall) continue;

            if (teleporterMask.value != 0)
            {
                bool isTp = Physics2D.OverlapBox(wp, checkHalfExtents * 2f, 0f, teleporterMask);
                if (isTp) continue;
            }
            list.Add(d);
        }
        return list;
    }

    float DistToPlayer(Vector3 worldPos) => player ? Vector3.Distance(worldPos, player.transform.position) : 9999f;

    static Vector2Int TurnRight(Vector2Int d) =>
        (d == Vector2Int.up) ? Vector2Int.right :
        (d == Vector2Int.right) ? Vector2Int.down :
        (d == Vector2Int.down) ? Vector2Int.left : Vector2Int.up;

    static Vector2Int TurnLeft(Vector2Int d) =>
        (d == Vector2Int.up) ? Vector2Int.left :
        (d == Vector2Int.left) ? Vector2Int.down :
        (d == Vector2Int.down) ? Vector2Int.right : Vector2Int.up;

    Vector2Int ClosestDir(Vector3 worldDir)
    {
        Vector2 v = new Vector2(worldDir.x, worldDir.y).normalized;
        float best = -1f; Vector2Int bestD = Vector2Int.right;
        foreach (var d in DIRS)
        {
            float dot = Vector2.Dot(v, d);
            if (dot > best) { best = dot; bestD = d; }
        }
        return bestD;
    }

    Vector2Int ClampDir(Vector2Int d)
    {
        if (d == Vector2Int.zero) return Vector2Int.right;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
            return d.x >= 0 ? Vector2Int.right : Vector2Int.left;
        else
            return d.y >= 0 ? Vector2Int.up : Vector2Int.down;
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        return new Vector3(gridOrigin.x + c.x * cellSize, gridOrigin.y + c.y * cellSize, 0f);
    }

    public Vector2Int WorldToCell(Vector3 w)
    {
        int cx = Mathf.RoundToInt((w.x - gridOrigin.x) / cellSize);
        int cy = Mathf.RoundToInt((w.y - gridOrigin.y) / cellSize);
        return new Vector2Int(cx, cy);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (state == GhostState.Dead) return; // Dead 忽略玩家

        gameRule?.OnGhostTouchPlayer(this);
    }
}
