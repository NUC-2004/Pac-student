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

    public Vector2Int lastInput = Vector2Int.zero;
    public Vector2Int currentInput = Vector2Int.zero;

    bool isMoving = false;
    Vector3 fromPos, toPos;
    float t = 0f;

    Animator animator;
    readonly int ID_IsMoving = Animator.StringToHash("IsMoving");
    readonly int ID_FaceX = Animator.StringToHash("FaceX");
    readonly int ID_FaceY = Animator.StringToHash("FaceY");

    // ===== 撞墙反馈（新增） =====
    [Header("Wall Bump Feedback")]
    public AudioSource sfx;           // 拖到 Player 上的 SFX AudioSource
    public AudioClip wallBumpClip;    // 撞墙音效
    public GameObject pfxWallHit;     // 撞墙粒子 prefab（如 Pfx_WallHit）
    public float bumpCooldown = 0.08f;// 防抖时间，避免连续触发
    float lastBumpTime = -999f;       // 上次触发时间

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        Vector2Int cell = WorldToCell(transform.position);
        transform.position = CellToWorld(cell);
        SetFace(Vector2Int.right);
        SetMoving(false);
    }

    void Update()
    {
        // 禁用控制时，不读输入也不移动
        if (!controlsEnabled) { SetMoving(false); return; }

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

        Vector3 center = SnapToCell(transform.position);
        transform.position = center;

        // 如果这一格朝该方向不可走，触发一次“撞墙反馈”
        if (!CanWalk(center, dir))
        {
            if (Time.time - lastBumpTime > bumpCooldown)
            {
                lastBumpTime = Time.time;

                // 撞击点 = 当前格中心 + 朝向 * 半格（靠近墙）
                Vector3 hitPos = center + (Vector3)(Vector2)dir * (cellSize * 0.5f);

                if (pfxWallHit) Instantiate(pfxWallHit, hitPos, Quaternion.identity);
                if (sfx && wallBumpClip) sfx.PlayOneShot(wallBumpClip);
            }
            return false;
        }

        fromPos = center;
        toPos = center + (Vector3)(Vector2)dir * cellSize;
        t = 0f;
        isMoving = true;

        currentInput = dir;
        SetFace(dir);
        SetMoving(true);
        return true;
    }

    bool CanWalk(Vector3 fromCenter, Vector2Int d)
    {
        Vector3 toCenter = fromCenter + (Vector3)(Vector2)d * cellSize;
        float box = cellSize * 0.6f;
        return !Physics2D.OverlapBox(toCenter, new Vector2(box, box), 0f, wallMask);
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
    // 瞬移到 worldPos，并让角色朝 outDir 继续移动；清空当前 lerp。
    public void TeleportTo(Vector3 worldPos, Vector2Int outDir)
    {
        transform.position = SnapToCell(worldPos); // 对齐网格
        lastInput = outDir;
        currentInput = outDir;

        // 取消当前插值，准备从新格中心开始下一步
        isMoving = false;
        t = 0f;

        SetFace(outDir);
        SetMoving(false);
    }
}
