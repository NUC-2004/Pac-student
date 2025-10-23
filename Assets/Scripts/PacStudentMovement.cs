using UnityEngine;
using System.Collections;

public class PacStudentMovement : MonoBehaviour
{
    [Header("Grid")]
    public float cellSize = 1f;                 // 一格大小（你的格距）
    public Vector2 gridOrigin = new Vector2(0.5f, 0.5f); // 网格原点偏移（豆子在 *.5 就用 0.5,0.5；在整数格用 0,0）
    public float speed = 4f;                    // 每秒走几格
    public LayerMask wallMask;                  // 只勾 Walls 层

    // 方向
    private Vector2Int dir = Vector2Int.right;      // 当前方向
    private Vector2Int lastDir = Vector2Int.right;  // 缓存方向（输入）
    private bool isMoving = false;

    void Update()
    {
        ReadInput();                 // 只记录输入
        if (!isMoving) TryStep();    // 仅在格中心决定下一步
    }

    void ReadInput()
    {
        Vector2 raw = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
            lastDir = new Vector2Int(raw.x > 0 ? 1 : (raw.x < 0 ? -1 : 0), 0);
        else if (Mathf.Abs(raw.y) > 0)
            lastDir = new Vector2Int(0, raw.y > 0 ? 1 : -1);
    }

    void TryStep()
    {
        // 吸附到格中心
        Vector3 center = RoundToCell(transform.position);
        transform.position = center;

        // 优先尝试缓存方向，其次保持原方向
        Vector2Int next = CanWalk(center, lastDir) ? lastDir :
                          (CanWalk(center, dir) ? dir : Vector2Int.zero);

        if (next == Vector2Int.zero) return;

        if (next != dir) dir = next;
        Vector3 target = center + (Vector3)(Vector2)dir * cellSize;
        StartCoroutine(MoveTo(target));
    }

    IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        Vector3 start = transform.position;
        float dur = cellSize / Mathf.Max(0.0001f, speed);  // 秒/格
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.position = target;
        isMoving = false;
    }

    bool CanWalk(Vector3 fromCenter, Vector2Int d)
    {
        if (d == Vector2Int.zero) return false;
        Vector3 toCenter = fromCenter + (Vector3)(Vector2)d * cellSize;

        // 用比一格略小的盒子探测目标格是否被墙占用（避免把走廊误判成墙）
        float box = cellSize * 0.6f; // 如仍顶墙，可把 0.6 改小到 0.5
        return !Physics2D.OverlapBox(toCenter, new Vector2(box, box), 0f, wallMask);
    }

    Vector3 RoundToCell(Vector3 p)
    {
        float rx = Mathf.Round((p.x - gridOrigin.x) / cellSize) * cellSize + gridOrigin.x;
        float ry = Mathf.Round((p.y - gridOrigin.y) / cellSize) * cellSize + gridOrigin.y;
        return new Vector3(rx, ry, p.z);
    }
}
