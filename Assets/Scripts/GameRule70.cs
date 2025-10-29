using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameRule70 : MonoBehaviour
{
    public static GameRule70 I;

    // -------------------- Refs --------------------
    [Header("Refs")]
    public PacStudentController player;   // 玩家控制脚本
    public LivesHeartsUI heartsUI;        // 左上角心形生命 UI（挂在 LivesPanel 上）
    public HUDController hud;             // 可为空：若用旧的 HUD 计分
    public string startScene = "StartScene";
    public GameObject gameOverGroup;      // GameOver UI 组

    [Header("Audio")]
    public AudioManager audioMgr;         // 统一音频管理（自动查找，亦可手动拖）
    public AudioClip scaredMusic;         // 受惊 BGM
    public AudioClip normalMusic;         // 普通 BGM

    [Header("Score / Life")]
    public int lives = 3;                 // 初始生命（与心形数量一致即可）

    [Header("Spawns")]
    public Transform playerSpawn;         // 可选：玩家复活点（不填则回到关卡开局格）

    // -------------------- Runtime --------------------
    int pelletLeft = 0;                   // 剩余（小豆+大力丸）数量
    float scaredEndTime = -1f;            // 受惊结束时间（<0 表示未受惊）
    GhostController[] ghosts;             // 场景中的幽灵
    int deadCount = 0;                    // Dead 计数（用于“最后一只复活时 BGM 兜底”）

    Vector2Int playerStartCell;           // 记录开局时玩家所在格（作为默认复活点）

    // PlayerPrefs keys
    const string KeyHighScore = "HighScore";
    const string KeyBestTime = "BestTime";

    // =========================================================
    void Awake()
    {
        I = this;
        if (!audioMgr) audioMgr = FindObjectOfType<AudioManager>(true);
        if (!heartsUI) heartsUI = FindObjectOfType<LivesHeartsUI>(true);
        if (!player) player = FindObjectOfType<PacStudentController>(true);
    }

    void Start()
    {
        // 自动再兜一层
        if (!audioMgr) audioMgr = FindObjectOfType<AudioManager>(true);
        if (!heartsUI) heartsUI = FindObjectOfType<LivesHeartsUI>(true);
        if (!player) player = FindObjectOfType<PacStudentController>(true);

        ghosts = FindObjectsOfType<GhostController>(true);

        // 全体幽灵回 Normal、标记从出生区重新出发
        scaredEndTime = -1f;
        if (ghosts != null)
        {
            foreach (var g in ghosts)
            {
                g.EnterNormal();
                g.hasExitedSpawn = false;
            }
        }
        HUDController.I?.StartScared(0f);

        // 统计豆子总数（小豆+大力丸）
        pelletLeft = FindObjectsOfType<PelletPickup>(true).Length
                   + FindObjectsOfType<PowerPelletPickup>(true).Length;

        // UI 初始化
        if (gameOverGroup) gameOverGroup.SetActive(false);
        heartsUI?.SetLives(lives);

        // 记录开局格（作为默认复活点）
        if (player) playerStartCell = player.WorldToCell(player.transform.position);

        // 开场播普通 BGM（兜底）
        if (audioMgr && normalMusic) audioMgr.PlayBgm(normalMusic, true);
    }

    // =========================================================
    //                Pellet / PowerPellet
    // =========================================================
    public void OnPelletEaten()
    {
        pelletLeft = Mathf.Max(0, pelletLeft - 1);
        if (pelletLeft == 0) GameOver();
    }

    public void OnPowerPelletEaten()
    {
        // +50 分（兼容两种计分器）
        if (ScoreKeeper.I) ScoreKeeper.I.AddScore(50);
        else hud?.AddScore(50);

        // SFX + 受惊状态
        audioMgr?.PlayPowerPellet();
        HUDController.I?.StartScared(10f);
        ScareAll(10f);

        OnPelletEaten();
    }

    public void ScareAll(float duration = 10f)
    {
        scaredEndTime = Time.time + duration;

        if (ghosts != null)
            foreach (var g in ghosts) g.EnterScared();

        // 切受惊 BGM
        if (audioMgr && scaredMusic) audioMgr.PlayBgm(scaredMusic, true);

        StopAllCoroutines();
        StartCoroutine(CoScaredTimeline());
    }

    IEnumerator CoScaredTimeline()
    {
        while (Time.time < scaredEndTime)
        {
            float left = scaredEndTime - Time.time;

            // 最后 3 秒进入 Recovering（仅视觉/逻辑提示）
            if (left <= 3f && ghosts != null)
                foreach (var g in ghosts) g.EnterRecovering();

            yield return null;
        }

        // 受惊结束：活着的回 Normal（Dead 保持，等它们回到 Spawn 再复活）
        if (ghosts != null)
            foreach (var g in ghosts)
                if (g.state != GhostState.Dead) g.EnterNormal();

        HUDController.I?.StartScared(0f);
        scaredEndTime = -1f;

        // 切回普通 BGM
        if (audioMgr && normalMusic) audioMgr.PlayBgm(normalMusic, true);
    }

    public float ScaredTimeLeft()
    {
        return Mathf.Max(0f, scaredEndTime < 0 ? 0f : scaredEndTime - Time.time);
    }

    // =========================================================
    //                Player  Ghost 碰撞裁决
    // =========================================================
    public void OnGhostTouchPlayer(GhostController g)
    {
        if (g == null) return;

        switch (g.state)
        {
            case GhostState.Normal:
                StartCoroutine(CoPlayerDie());
                break;

            case GhostState.Scared:
            case GhostState.Recovering:
                // 吃幽灵 +300 分 + SFX
                if (ScoreKeeper.I) ScoreKeeper.I.AddScore(300);
                else hud?.AddScore(300);
                audioMgr?.PlayEatGhost();
                g.EnterDead(); // 进入 Dead，直线回出生区
                break;

            case GhostState.Dead:
                // Dead 忽略
                break;
        }
    }

    IEnumerator CoPlayerDie()
    {
        if (player != null) player.controlsEnabled = false;

        yield return new WaitForSeconds(0.5f);

        // 扣命 & 同步心 UI
        lives = Mathf.Max(0, lives - 1);
        heartsUI?.SetLives(lives);

        // 复活到 PlayerSpawn（若未设则回到开局格）
        if (player != null)
        {
            Vector3 spawnPos = player.CellToWorld(playerStartCell); // 默认开局格
            if (playerSpawn) spawnPos = player.SnapToCell(playerSpawn.position);
            player.TeleportTo(spawnPos, Vector2Int.right);
        }

        // 重置幽灵
        if (ghosts != null)
            foreach (var g in ghosts) g.ResetToStart();

        // 受惊清空，全部回 Normal；切回普通 BGM
        scaredEndTime = -1f;
        StopAllCoroutines();
        if (ghosts != null)
            foreach (var g in ghosts) g.EnterNormal();
        HUDController.I?.StartScared(0f);

        if (!audioMgr) audioMgr = FindObjectOfType<AudioManager>(true);
        if (audioMgr && normalMusic) audioMgr.PlayBgm(normalMusic, true);

        if (lives <= 0) { GameOver(); yield break; }
        if (player != null) player.controlsEnabled = true;
    }

    // =========================================================
    //                        Game Over
    // =========================================================
    void GameOver()
    {
        if (player != null) player.controlsEnabled = false;
        if (ghosts != null) foreach (var g in ghosts) g.enabled = false;

        if (gameOverGroup) gameOverGroup.SetActive(true);

        SaveHighScore();

        // 归零显示（可选）
        heartsUI?.SetLives(0);

        // 切普通 BGM（兜底）
        if (audioMgr && normalMusic) audioMgr.PlayBgm(normalMusic, true);

        StartCoroutine(CoBackToStart());
    }

    IEnumerator CoBackToStart()
    {
        yield return new WaitForSeconds(3f);
        if (!string.IsNullOrEmpty(startScene))
            SceneManager.LoadScene(startScene);
    }

    void SaveHighScore()
    {
        int curScore = ScoreKeeper.I ? ScoreKeeper.I.Score : 0;
        float curTime = ScoreKeeper.I ? ScoreKeeper.I.Elapsed : 0f;

        int oldScore = PlayerPrefs.GetInt(KeyHighScore, 0);
        float oldTime = PlayerPrefs.GetFloat(KeyBestTime, float.MaxValue);

        bool better = (curScore > oldScore) || (curScore == oldScore && curTime < oldTime);
        if (better)
        {
            PlayerPrefs.SetInt(KeyHighScore, curScore);
            PlayerPrefs.SetFloat(KeyBestTime, curTime);
            PlayerPrefs.Save();
        }
    }

    // =========================================================
    //                Dead 计数（用于 BGM 兜底）
    // =========================================================
    public void NotifyGhostBecameDead() { deadCount++; }

    public void NotifyGhostRevived()
    {
        deadCount = Mathf.Max(0, deadCount - 1);
        if (deadCount == 0)
        {
            // 所有幽灵都非 Dead：根据是否仍处于受惊，兜底切回对应 BGM
            if (ScaredTimeLeft() > 0f && audioMgr && scaredMusic)
                audioMgr.PlayBgm(scaredMusic, true);
            else if (audioMgr && normalMusic)
                audioMgr.PlayBgm(normalMusic, true);
        }
    }
}
