using UnityEngine;
using System.Collections;

public enum GhostState { Normal, Scared, Recovering, Dead }

public class GhostController : MonoBehaviour
{
    public GhostState state = GhostState.Normal;
    public Vector3 startPos;             // 初始位置（Awake 里记录）
    public float reviveDelay = 3f;       // 被吃后 3 秒复活

    void Awake() { startPos = transform.position; }

    // —— 状态切换（动画/颜色可自行加，这里只管逻辑）——
    public void EnterNormal() { state = GhostState.Normal; }
    public void EnterScared() { if (state != GhostState.Dead) state = GhostState.Scared; }
    public void EnterRecovering() { if (state != GhostState.Dead) state = GhostState.Recovering; }
    public void EnterDead()
    {
        if (state == GhostState.Dead) return;
        state = GhostState.Dead;
        StopAllCoroutines();
        StartCoroutine(CoRevive());
    }

    IEnumerator CoRevive()
    {
        yield return new WaitForSeconds(reviveDelay);
        // 复活时根据全局受惊计时还剩多少决定回哪种状态
        float left = GameRule70.I.ScaredTimeLeft();
        if (left > 3f) EnterScared();
        else if (left > 0f) EnterRecovering();
        else EnterNormal();
        // 回到起始点
        transform.position = startPos;
    }

    // —— 与玩家碰撞 ——（仅负责给 GameRule 报告）
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        GameRule70.I?.OnGhostTouchPlayer(this);
    }

    // 外部重置（玩家死亡后）
    public void ResetToStart()
    {
        transform.position = startPos;
        EnterNormal();
    }
}

