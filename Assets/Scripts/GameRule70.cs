using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 关卡规则（A4 要求）：
/// - 统计场景中所有小豆/大力丸数量
/// - 吃光后触发 Game Over（可显示HUD组），3秒后回到开始场景
/// - 提供统一的“大力丸触发”入口，驱动幽灵受惊/恢复时间线
/// - 记录高分与用时
/// </summary>
public class GameRule70 : MonoBehaviour
{
    public static GameRule70 I;

    [Header("Refs")]
    public PacStudentController player;        // 玩家（用于禁用控制、复位等）
    public HUDController hud;                  // HUD（计分、受惊显示等）
    public string startScene = "StartScene";   // Game Over 后返回的场景名
    public GameObject gameOverGroup;           // HUD 上“Game Over + 遮罩”的父物体（可选）

    [Header("Lives & Score")]
    public int lives = 3;                      // 初始命（如需）

    // —— 清关计数（只统计小豆+大力丸，不含樱桃等其他道具）——
    int pelletLeft = 0;

    [Header("Audio (optional)")]
    public AudioManager audioMgr;              // 旧 AudioManager（可空）
    public AudioClip scaredMusic;              // 受惊BGM（可空）
    public AudioClip normalMusic;              // 普通BGM（可空）

    // 受惊计时（≤3s 进入 Recovering）
    float scaredEndTime = -1f;

    // 幽灵集合（需你的 GhostController 实现具备下列方法/字段）
    GhostController[] ghosts;

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        ghosts = FindObjectsOfType<GhostController>(true);

        // 场景启动：统计豆子总数（小豆 + 大力丸）
        int pellets = FindObjectsOfType<PelletPickup>(true).Length;
        int powers = FindObjectsOfType<PowerPelletPickup>(true).Length;
        pelletLeft = pellets + powers;

        if (gameOverGroup) gameOverGroup.SetActive(false);
        // 计时器（若你的 ScoreKeeper 里在别处启动，这里无需处理）
    }

    // ===================== 大力丸（统一入口） =====================

    /// <summary>
    /// 玩家吃到大力丸时从 PowerPelletPickup 调用
    /// 负责：+50 分、HUD受惊显示、驱动受惊状态、并计入清关-1
    /// </summary>
    public void OnPowerPelletEaten()
    {
        // 1) +50 分
        if (ScoreKeeper.I) ScoreKeeper.I.AddScore(50);
        else hud?.AddScore(50); // 兜底

        // 2) HUD 显示受惊倒计时（例如 10 秒）
        HUDController.I?.StartScared(10f);

        // 3) 所有幽灵进入 "Scared → (≤3s) Recovering → Normal" 时间线
        ScareAll(10f);

        // 4) 作为清关项：-1
        OnPelletEaten();
    }

    /// <summary>
    /// 触发全体幽灵受惊，带音乐切换与时间线驱动
    /// </summary>
    public void ScareAll(float duration = 10f)
    {
        scaredEndTime = Time.time + duration;

        if (ghosts != null)
        {
            foreach (var g in ghosts) g.EnterScared();
        }

        // 切受惊 BGM（可选）
        if (audioMgr && scaredMusic)
        {
            audioMgr.audioSource.loop = true;
            audioMgr.audioSource.clip = scaredMusic;
            audioMgr.audioSource.Play();
        }

        // 协程驱动倒计时和状态回收
        StopAllCoroutines();
        StartCoroutine(CoScaredTimeline());
    }

    IEnumerator CoScaredTimeline()
    {
        while (Time.time < scaredEndTime)
        {
            float left = scaredEndTime - Time.time;

            // ≤3 秒进入 Recovering
            if (left <= 3f && ghosts != null)
            {
                foreach (var g in ghosts) g.EnterRecovering();
            }

            // HUD 的受惊剩余时间可在 HUD 自己的 Update 刷，这里不用重复
            yield return null;
        }

        // 时间到：活着的幽灵回 Normal（Dead 保持）
        if (ghosts != null)
        {
            foreach (var g in ghosts)
                if (g.state != GhostState.Dead) g.EnterNormal();
        }

        // 切回普通 BGM（可选）
        if (audioMgr && normalMusic)
        {
            audioMgr.audioSource.loop = true;
            audioMgr.audioSource.clip = normalMusic;
            audioMgr.audioSource.Play();
        }

        // 清除 HUD 受惊显示
        HUDController.I?.StartScared(0f);
        scaredEndTime = -1f;
    }

    /// <summary>
    /// 获取受惊剩余时间（HUD 可读）
    /// </summary>
    public float ScaredTimeLeft() =>
        Mathf.Max(0f, scaredEndTime < 0 ? 0f : (scaredEndTime - Time.time));

    // ===================== 幽灵碰到玩家 =====================

    /// <summary>
    /// 幽灵与玩家相撞（由幽灵或玩家在碰撞时调用）
    /// </summary>
    public void OnGhostTouchPlayer(GhostController g)
    {
        if (g == null) return;

        if (g.state == GhostState.Normal)
        {
            // 玩家死亡一条命
            StartCoroutine(CoPlayerDie());
        }
        else if (g.state == GhostState.Scared || g.state == GhostState.Recovering)
        {
            // 玩家吃幽灵 +300
            if (ScoreKeeper.I) ScoreKeeper.I.AddScore(300);
            else HUDController.I?.AddScore(300);

            g.EnterDead();
        }
        // Dead 不处理
    }

    IEnumerator CoPlayerDie()
    {
        if (player != null) player.controlsEnabled = false;

        // 可在此播放玩家死亡动画/粒子
        yield return new WaitForSeconds(0.5f);

        lives = Mathf.Max(0, lives - 1);

        // 重置玩家与幽灵到出生点/初始状态（注意替换为你的实际出生点）
        if (player != null)
        {
            player.TeleportTo(player.CellToWorld(new Vector2Int(-12, 13)), Vector2Int.right);
        }
        if (ghosts != null)
        {
            foreach (var g in ghosts) g.ResetToStart();
        }

        // 结束受惊状态 & 还原音乐
        scaredEndTime = -1f;
        StopAllCoroutines();
        if (ghosts != null)
        {
            foreach (var g in ghosts) g.EnterNormal();
        }
        if (audioMgr && normalMusic)
        {
            audioMgr.audioSource.loop = true;
            audioMgr.audioSource.clip = normalMusic;
            audioMgr.audioSource.Play();
        }
        HUDController.I?.StartScared(0f);

        // 没命 => 直接 Game Over
        if (lives <= 0)
        {
            GameOver();
            yield break;
        }

        // 否则允许玩家继续（最简：立即解锁控制）
        if (player != null) player.controlsEnabled = true;
    }

    // ===================== 清关计数（小豆/大力丸各-1） =====================

    /// <summary>
    /// 小豆或大力丸被吃掉时调用（由 Pellet 脚本触发）
    /// </summary>
    public void OnPelletEaten()
    {
        pelletLeft = Mathf.Max(0, pelletLeft - 1);

        // 当剩余为 0 => Game Over（清关）
        if (pelletLeft == 0) GameOver();
    }

    // ===================== Game Over 处理 =====================

    void GameOver()
    {
        // 禁止玩家输入
        if (player != null) player.controlsEnabled = false;

        // 停止幽灵逻辑
        if (ghosts != null)
            foreach (var g in ghosts) g.enabled = false;

        // 显示 HUD 的 Game Over 组（可选）
        if (gameOverGroup) gameOverGroup.SetActive(true);

        // 保存高分/最佳时间
        SaveHighScore();

        // 3 秒后回到开始场景
        StartCoroutine(CoBackToStart());
    }

    void SaveHighScore()
    {
        int curScore = ScoreKeeper.I ? ScoreKeeper.I.Score : 0;
        float curTime = ScoreKeeper.I ? ScoreKeeper.I.Elapsed : 0f;

        int oldScore = PlayerPrefs.GetInt("HighScore", 0);
        float oldTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);

        bool better = (curScore > oldScore) || (curScore == oldScore && curTime < oldTime);
        if (better)
        {
            PlayerPrefs.SetInt("HighScore", curScore);
            PlayerPrefs.SetFloat("BestTime", curTime);
            PlayerPrefs.Save();
        }
    }

    IEnumerator CoBackToStart()
    {
        yield return new WaitForSeconds(3f);
        if (!string.IsNullOrEmpty(startScene))
        {
            SceneManager.LoadScene(startScene);
        }
    }
}
