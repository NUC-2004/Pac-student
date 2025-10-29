using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Teleporter : MonoBehaviour
{
    [Header("Pair & Direction")]
    public Teleporter other;              // 对面那个传送门
    public Vector2Int inwardDir = Vector2Int.left; // 传出后面向“里”的方向（右门=Left，左门=Right）

    [Header("Tuning")]
    public float exitOffset = 1.0f;            // 从出口往里推一点，避免立刻再触发
    public float reenterBlock = 0.2f;          // 传送后短暂屏蔽再进入

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (other == null || !col.CompareTag("Player")) return;

        // 简单的“冷却标记”，避免刚传完又触发
        var mark = col.GetComponent<TeleportMark>();
        if (mark == null) mark = col.gameObject.AddComponent<TeleportMark>();
        if (Time.time < mark.blockUntil) return;

        // 计算出口位置（对面门的位置 + 朝里方向 * 偏移）
        Vector3 exitPos = other.transform.position + (Vector3)(Vector2)other.inwardDir * other.exitOffset;

        // 优先调用玩家控制脚本里的瞬移方法，保证不会被当前 lerp 拉回去
        var ctrl = col.GetComponent<PacStudentController>();
        if (ctrl != null) ctrl.TeleportTo(exitPos, other.inwardDir);
        else col.transform.position = exitPos;

        mark.blockUntil = Time.time + reenterBlock;
    }

    // 内嵌一个简单冷却组件
    class TeleportMark : MonoBehaviour { public float blockUntil; }
}
