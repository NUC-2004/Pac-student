using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Grid")]
    public float cellSize = 1f;
    public Vector2 gridOrigin = new Vector2(0.5f, 0.5f);

    [Header("Movement")]
    public float speed = 4f;
    public LayerMask wallMask;

    // 允许/禁止玩家控制（外部可改）
    public bool controlsEnabled = true;

    public Vector2Int lastInput = Vector2Int.zero;   // 最近一次按键方向（WASD 更新）
    public Vector2Int currentInput = Vector2Int.zero; // 当前前进方向

    bool isMoving = false;
    Vector3 fromPos, toPos;
    float t = 0f;

    Animator animator;
    readonly int ID_IsMoving = Animator.StringToHash("IsMoving");
    readonly int ID_FaceX = Animator.StringToHash("FaceX");
    readonly int ID_FaceY = Animator.StringToHash("FaceY");

    // ===== 撞墙反馈（强化版防误触） =====
    [Header("Wall Bump Feedback")]
    public AudioSource sfx;            // 拖到 Player 上的 SFX AudioSource
    public AudioClip wallBumpClip;     // 撞墙音效
    public GameObject pfxWallHit;      // 撞墙粒子 prefab（Pfx_WallHit，Simulation Space = World）
    [Tooltip("避免连续触发的冷却时间（秒）")]
    public float bumpCooldown = 0.08f;

    // “同一格+方向”只触发一次的去抖
    Vector2Int lastBumpCell = new Vector2Int(99999, 99999);
    Vector2Int lastBumpDir = Vector2Int.zero;
    float lastBumpTime = -999f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // 对齐最近格中心
        var cell = WorldToCell(transform.position);
        transform.position = CellToWorld(cell);
        SetFace(Vector2Int.right);
        SetMoving(false);
    }

    void Update()
    {
        if (!controlsEnabled) { SetMoving(false); return; }

        // 只在按键按下时更新 lastInput
        if (Input.GetKeyDown(KeyCode.W)) lastInput = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S)) lastInput = Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A)) lastInput = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D)) lastInput = Vector2Int.right;

        if (!isMoving)
        {
            if (!TryStartMove(lastInput))
                TryStartMove(currentInput);
        }
        else
        {
            float dur = 1f / Mathf.Max(0.0001f, speed);
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(fromPos, toPos, Mathf.Clamp01(t));
            if (t >= 1f)
            {
                transform.position = toPos;
                isMoving = false;
                SetMoving(false);
            }
        }
    }

    bool TryStartMove(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return false;

        // 吸附到格中心，避免累计误差
        Vector3 center = SnapToCell(transform.position);
        transform.position = center;

        // 判定是否被墙阻挡
        bool blocked = IsBlocked(center, dir, out RaycastHit2D hit);

        if (blocked)
        {
            TryPlayBump(center, dir, hit);
            return false;
        }

        // 可走 → 启动一次格移动
        fromPos = center;
        toPos = center + (Vector3)(Vector2)dir * cellSize;
        t = 0f;
        isMoving = true;

        // 进入新格后，重置“同一格+方向”的去抖
        lastBumpCell = new Vector2Int(99999, 99999);
        lastBumpDir = Vector2Int.zero;

        currentInput = dir;
        SetFace(dir);
        SetMoving(true);
        return true;
    }

    /// <summary>
    /// 使用 BoxCast 判定“从当前格中心沿 d 迈向下一格中心”是否被墙阻挡。
    /// 命中信息通过 hit 返回（若未命中则 hit.collider = null）。
    /// </summary>
    bool IsBlocked(Vector3 fromCenter, Vector2Int d, out RaycastHit2D hit)
    {
        Vector2 origin = fromCenter;
        Vector2 dir = d;
        float dist = cellSize * 0.95f;

        // 盒体略小于一格，避免墙角误判；注意只打 wallMask
        Vector2 box = new Vector2(cellSize * 0.6f, cellSize * 0.6f);
        hit = Physics2D.BoxCast(origin, box, 0f, dir, dist, wallMask);
        return hit.collider != null;
    }

    void TryPlayBump(Vector3 center, Vector2Int dir, RaycastHit2D hit)
    {
        // 1) 命中校验：必须真打到墙
        if (!hit.collider) return;

        // 2) 法线校验：需要“正面撞墙”，而不是擦边/背面
        // 与移动方向夹角接近 180° → dot(normal, dir) < -0.5 更保险
        float oppose = Vector2.Dot(hit.normal.normalized, ((Vector2)dir).normalized);
        if (oppose > -0.5f) return;

        // 3) 冷却时间
        if (Time.time - lastBumpTime <= bumpCooldown) return;

        // 4) 同一“格 + 方向”只触发一次
        Vector2Int curCell = WorldToCell(center);
        if (curCell == lastBumpCell && dir == lastBumpDir) return;

        lastBumpCell = curCell;
        lastBumpDir = dir;
        lastBumpTime = Time.time;

        // 5) 实例化粒子（世界坐标，Z=0；Prefab 的 Simulation Space 请设为 World）
        Vector3 hitPos = new Vector3(hit.point.x, hit.point.y, 0f);
        if (pfxWallHit) Instantiate(pfxWallHit, hitPos, Quaternion.identity);

        // 6) 音效
        if (sfx && wallBumpClip) sfx.PlayOneShot(wallBumpClip);
    }

    // ======= 网格工具（public 便于外部调用） =======
    public Vector3 SnapToCell(Vector3 p)
    {
        float rx = Mathf.Round((p.x - gridOrigin.x) / cellSize) * cellSize + gridOrigin.x;
        float ry = Mathf.Round((p.y - gridOrigin.y) / cellSize) * cellSize + gridOrigin.y;
        return new Vector3(rx, ry, p.z);
    }

    public Vector2Int WorldToCell(Vector3 p)
    {
        int cx = Mathf.RoundToInt((p.x - gridOrigin.x) / cellSize);
        int cy = Mathf.RoundToInt((p.y - gridOrigin.y) / cellSize);
        return new Vector2Int(cx, cy);
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        return new Vector3(c.x * cellSize + gridOrigin.x, c.y * cellSize + gridOrigin.y, 0f);
    }

    void SetFace(Vector2Int d)
    {
        int x = Mathf.Clamp(d.x, -1, 1);
        int y = Mathf.Clamp(d.y, -1, 1);
        if (y != 0) x = 0;
        if (x != 0) y = 0;
        if (animator)
        {
            animator.SetFloat(ID_FaceX, x);
            animator.SetFloat(ID_FaceY, y);
        }
    }

    void SetMoving(bool on)
    {
        if (!animator) return;
        animator.SetBool(ID_IsMoving, on);
    }

    // ====== 传送门接口 ======
    public void TeleportTo(Vector3 worldPos, Vector2Int outDir)
    {
        transform.position = SnapToCell(worldPos); // 对齐网格
        lastInput = outDir;
        currentInput = outDir;

        isMoving = false;
        t = 0f;

        SetFace(outDir);
        SetMoving(false);
    }

}
