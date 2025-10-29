using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 负责：回合开始倒计时、记分/计时、吃到物品的分数与结束判定、简单“幽灵受惊”计时显示、Game Over 保存分数。
/// 需要在 Inspector 里拖拽：
/// - player：场上 PacStudent（含 PacStudentController）
/// - txtScore / txtTimer / txtScared / txtLevelName：HUD 的 TextMeshPro 文本
/// - livesPanel：HUD 的 Life 容器（下面放了 3 个心心）
/// - countdownPanel：倒计时整块（包含遮罩和大字），txtCountdown 是其中的大字
/// </summary>
public class GameRule : MonoBehaviour
{
    public static GameRule I;

    [Header("Scene")]
    public string startSceneName = "StartScene";

    [Header("Refs")]
    public PacStudentController player;
    public TMP_Text txtScore;
    public TMP_Text txtTimer;
    public TMP_Text txtScared;      // 幽灵受惊剩余秒数（整数），默认隐藏
    public TMP_Text txtLevelName;   // “Level 1 / Level 2”
    public Transform livesPanel;    // 里面已有 3 个心心
    [Space]
    public GameObject countdownPanel;   // 遮罩 + 大字
    public TMP_Text txtCountdown;       // “3 / 2 / 1 / GO!”

    [Header("Round")]
    public int startLives = 3;
    public string levelName = "Level 1";
    public float goFreezeSeconds = 1f;  // “GO!” 显示时长
    public float backToStartDelay = 3f; // Game Over 后返回 StartScene 的等待

    // 分数/时间
    int score;
    int lives;
    float timeElapsed;     // 计时（秒）
    bool running;          // 是否在计时（GO 之后）

    // 关卡剩余可吃的物件
    int pelletsRemaining;

    // 受惊计时
    bool scaredActive;
    float scaredLeft;      // 剩余秒数（从 10 倒数到 0，再隐藏）

    // PlayerPrefs Keys（StartScene 也会用）
    const string KeyHighScore = "HighScore";
    const string KeyBestTime = "BestTime";

    void Awake()
    {
        I = this;
        if (!player) player = FindObjectOfType<PacStudentController>();
    }

    void Start()
    {
        // 统计可吃目标（小豆 + 大力丸）
        pelletsRemaining = FindObjectsOfType<PelletPickup>(true).Length
                         + FindObjectsOfType<PowerPelletPickup>(true).Length;

        // HUD 初始化
        lives = startLives;
        score = 0;
        timeElapsed = 0;
        running = false;

        if (txtLevelName) txtLevelName.text = levelName;
        UpdateScoreUI();
        UpdateTimerUI(0);
        UpdateLivesUI();
        SetScaredUI(visible: false, seconds: 0);

        // 开场倒计时
        if (countdownPanel) countdownPanel.SetActive(true);
        if (player) player.controlsEnabled = false;
        StartCoroutine(RoundStartRoutine());
    }

    void Update()
    {
        // 游戏计时
        if (running)
        {
            timeElapsed += Time.deltaTime;
            UpdateTimerUI(timeElapsed);
        }

        // 受惊计时
        if (scaredActive)
        {
            scaredLeft -= Time.deltaTime;
            float show = Mathf.Max(0f, scaredLeft);
            if (txtScared)
            {
                txtScared.gameObject.SetActive(show > 0f);
                txtScared.text = Mathf.CeilToInt(show).ToString();
            }

            if (scaredLeft <= 0f)
            {
                scaredActive = false;
                // 恢复普通音乐
                GameAudioManager.I?.PlayLevelMusic();
                // TODO：把幽灵从 Recovering/Scared 切回 Normal（如果你有幽灵状态机）
            }
            else if (scaredLeft <= 3f)
            {
                // TODO：3 秒时切到幽灵“Recovering”状态（若你有）
            }
        }
    }

    #region —— 回合开始 / 倒计时 ——
    System.Collections.IEnumerator RoundStartRoutine()
    {
        // “3 2 1” 倒计时
        if (txtCountdown) txtCountdown.text = "3";
        yield return new WaitForSeconds(1f);
        if (txtCountdown) txtCountdown.text = "2";
        yield return new WaitForSeconds(1f);
        if (txtCountdown) txtCountdown.text = "1";
        yield return new WaitForSeconds(1f);
        if (txtCountdown) txtCountdown.text = "GO!";
        yield return new WaitForSeconds(goFreezeSeconds);

        // 开始游戏
        if (countdownPanel) countdownPanel.SetActive(false);
        running = true;
        GameAudioManager.I?.PlayLevelMusic();
        if (player) player.controlsEnabled = true;
    }
    #endregion

    #region —— Pellet / PowerPellet / Cherry ——（供拾取脚本调用）
    public void OnPelletEaten()
    {
        pelletsRemaining--;
        AddScore(10);
        CheckRoundFinish();
    }

    public void OnPowerPelletEaten()
    {
        pelletsRemaining--;
        AddScore(50);

        // 音乐 & 受惊 10s
        GameAudioManager.I?.PlayPowerPellet();
        GameAudioManager.I?.PlayScaredMusic();

        scaredActive = true;
        scaredLeft = 10f;
        SetScaredUI(true, scaredLeft);

        CheckRoundFinish();
    }

    public void OnCherryEaten()
    {
        AddScore(100);
        GameAudioManager.I?.PlayCherry();
    }
    #endregion

    #region —— 玩家死亡 / 复活 / 结束判定 ——
    public void KillPlayer()
    {
        if (running == false) return;

        lives = Mathf.Max(0, lives - 1);
        UpdateLivesUI();

        // 停止计时，关操作
        running = false;
        if (player) player.controlsEnabled = false;

        // TODO：播放死亡动画/粒子、停止幽灵移动等
        // 这里简单延迟复活或结算
        if (lives > 0)
            Invoke(nameof(RespawnPlayer), 1.0f);
        else
            Invoke(nameof(GameOver), 1.0f);
    }

    void RespawnPlayer()
    {
        // 复位到起始点（你项目里是左上角，可以根据需要改）
        if (player)
        {
            // 让玩家回到初始格并等待输入
            player.transform.position = player.transform.position; // 这里只是占位；若你有固定出生点，可替换
            player.controlsEnabled = false;
        }

        // 小 “3,2,1,GO!”（简化：直接继续）
        if (player) player.controlsEnabled = true;
        running = true;
        GameAudioManager.I?.PlayLevelMusic();
    }

    void CheckRoundFinish()
    {
        if (pelletsRemaining <= 0)
        {
            // 清屏 -> Game Over（胜利）
            GameOver();
        }
    }

    void GameOver()
    {
        running = false;
        if (player) player.controlsEnabled = false;

        // 显示遮罩+“Game Over”（这里复用 countdown 面板，写字可用 txtCountdown）
        if (countdownPanel) countdownPanel.SetActive(true);
        if (txtCountdown) txtCountdown.text = "GAME OVER";

        // 保存高分
        TrySaveHighScore();

        // 返回 StartScene
        Invoke(nameof(ReturnToStart), backToStartDelay);
    }
    #endregion

    #region —— UI 更新 / 分数与时间格式化 ——
    void AddScore(int add)
    {
        score += add;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (!txtScore) return;
        txtScore.text = score.ToString("D6"); // 6位补零
    }

    void UpdateTimerUI(float seconds)
    {
        if (!txtTimer) return;
        int mm = Mathf.FloorToInt(seconds / 60f);
        int ss = Mathf.FloorToInt(seconds % 60f);
        int cs = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f); // centiseconds
        txtTimer.text = $"{mm:00}:{ss:00}:{cs:00}";
    }

    void UpdateLivesUI()
    {
        if (!livesPanel) return;
        // 简化：livesPanel 下前 lives 个启用，其余禁用
        for (int i = 0; i < livesPanel.childCount; i++)
            livesPanel.GetChild(i).gameObject.SetActive(i < lives);
    }

    void SetScaredUI(bool visible, float seconds)
    {
        if (!txtScared) return;
        txtScared.gameObject.SetActive(visible);
        if (visible) txtScared.text = Mathf.CeilToInt(seconds).ToString();
    }
    #endregion

    #region —— 高分存取 & 返回开始场景 ——
    void TrySaveHighScore()
    {
        int prevScore = PlayerPrefs.GetInt(KeyHighScore, 0);
        float prevBestTime = PlayerPrefs.GetFloat(KeyBestTime, float.MaxValue);

        bool better =
            (score > prevScore) ||
            (score == prevScore && timeElapsed < prevBestTime);

        if (better)
        {
            PlayerPrefs.SetInt(KeyHighScore, score);
            PlayerPrefs.SetFloat(KeyBestTime, timeElapsed);
            PlayerPrefs.Save();
        }
    }

    void ReturnToStart()
    {
        if (!string.IsNullOrEmpty(startSceneName))
            SceneManager.LoadScene(startSceneName);
    }
    #endregion
}
