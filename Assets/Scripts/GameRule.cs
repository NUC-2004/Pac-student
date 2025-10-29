using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameRule : MonoBehaviour
{
    public static GameRule I;

    [Header("Scene")]
    public string startSceneName = "StartScene";

    [Header("Refs")]
    public PacStudentController player;
    public TMP_Text txtScore;
    public TMP_Text txtTimer;
    public TMP_Text txtScared;
    public TMP_Text txtLevelName;
    public Transform livesPanel;            // 下面放 Life_1 / Life_2 / Life_3
    [Space]
    public GameObject countdownPanel;
    public TMP_Text txtCountdown;

    [Header("Round")]
    public int startLives = 3;
    public string levelName = "Level 1";
    public float goFreezeSeconds = 1f;
    public float backToStartDelay = 3f;

    [Header("Audio")]
    public AudioManager audioMgr;           // 自动寻找，或手动拖
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip cherrySfx;             // 可选：吃樱桃

    // 分数/时间/生命
    int score;
    int lives;
    float timeElapsed;
    bool running;

    // 目标计数
    int pelletsRemaining;

    // 受惊计时
    bool scaredActive;
    float scaredLeft;

    // PlayerPrefs Keys
    const string KeyHighScore = "HighScore";
    const string KeyBestTime = "BestTime";

    void Awake()
    {
        I = this;
        if (!player) player = FindObjectOfType<PacStudentController>(true);
        if (!audioMgr) audioMgr = FindObjectOfType<AudioManager>(true); // 免拖拽
    }

    void Start()
    {
        if (!audioMgr) audioMgr = FindObjectOfType<AudioManager>(true);

        // 统计（小豆 + 大力丸）
        pelletsRemaining = FindObjectsOfType<PelletPickup>(true).Length
                         + FindObjectsOfType<PowerPelletPickup>(true).Length;

        // HUD 初始化
        lives = startLives;
        score = 0;
        timeElapsed = 0f;
        running = false;

        if (txtLevelName) txtLevelName.text = levelName;
        UpdateScoreUI();
        UpdateTimerUI(0);
        UpdateLivesUI();
        SetScaredUI(false, 0);

        // 倒计时
        if (countdownPanel) countdownPanel.SetActive(true);
        if (player) player.controlsEnabled = false;

        // 开场 BGM
        if (audioMgr && normalMusic) audioMgr.PlayBgm(normalMusic, true);

        StartCoroutine(RoundStartRoutine());
    }

    void Update()
    {
        if (running)
        {
            timeElapsed += Time.deltaTime;
            UpdateTimerUI(timeElapsed);
        }

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
                // 受惊结束 → 切回普通 BGM
                if (audioMgr && normalMusic) audioMgr.PlayBgm(normalMusic, true);
            }
            else if (scaredLeft <= 3f)
            {
                // 这里可切 Recovering（如需）
            }
        }
    }

    #region 回合开始/倒计时
    System.Collections.IEnumerator RoundStartRoutine()
    {
        if (txtCountdown) txtCountdown.text = "3"; yield return new WaitForSeconds(1f);
        if (txtCountdown) txtCountdown.text = "2"; yield return new WaitForSeconds(1f);
        if (txtCountdown) txtCountdown.text = "1"; yield return new WaitForSeconds(1f);
        if (txtCountdown) txtCountdown.text = "GO!"; yield return new WaitForSeconds(goFreezeSeconds);

        if (countdownPanel) countdownPanel.SetActive(false);
        running = true;
        if (player) player.controlsEnabled = true;

        // 开局就用普通 BGM（已经在 Start 播了，这里不重复）
    }
    #endregion

    #region Pellet / PowerPellet / Cherry （给拾取脚本调用）
    public void OnPelletEaten()
    {
        pelletsRemaining--;
        AddScore(10);
        // 吃豆 SFX
        audioMgr?.PlayPellet();

        CheckRoundFinish();
    }

    public void OnPowerPelletEaten()
    {
        pelletsRemaining--;
        AddScore(50);

        // 大力丸 SFX + 受惊 BGM
        audioMgr?.PlayPowerPellet();
        if (audioMgr && scaredMusic) audioMgr.PlayBgm(scaredMusic, true);

        scaredActive = true;
        scaredLeft = 10f;
        SetScaredUI(true, scaredLeft);

        CheckRoundFinish();
    }

    public void OnCherryEaten()
    {
        AddScore(100);
        if (audioMgr && cherrySfx) audioMgr.PlaySfx(cherrySfx);
    }
    #endregion

    #region 玩家死亡/复活/结束
    public void KillPlayer()
    {
        if (!running) return;

        lives = Mathf.Max(0, lives - 1);
        UpdateLivesUI();

        running = false;
        if (player) player.controlsEnabled = false;

        if (lives > 0)
            Invoke(nameof(RespawnPlayer), 1.0f);
        else
            Invoke(nameof(GameOver), 1.0f);
    }

    void RespawnPlayer()
    {
        // 这里按你项目需要复位玩家；先简单恢复控制
        if (player) player.controlsEnabled = true;

        // 切回普通 BGM
        if (audioMgr && normalMusic) audioMgr.PlayBgm(normalMusic, true);

        running = true;
    }

    void CheckRoundFinish()
    {
        if (pelletsRemaining <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        running = false;
        if (player) player.controlsEnabled = false;

        if (countdownPanel) countdownPanel.SetActive(true);
        if (txtCountdown) txtCountdown.text = "GAME OVER";

        TrySaveHighScore();

        // 切普通 BGM
        if (audioMgr && normalMusic) audioMgr.PlayBgm(normalMusic, true);

        Invoke(nameof(ReturnToStart), backToStartDelay);
    }
    #endregion

    #region UI & 存档
    void AddScore(int add)
    {
        score += add;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (txtScore) txtScore.text = score.ToString("D6");
    }

    void UpdateTimerUI(float seconds)
    {
        if (!txtTimer) return;
        int mm = Mathf.FloorToInt(seconds / 60f);
        int ss = Mathf.FloorToInt(seconds % 60f);
        int cs = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);
        txtTimer.text = $"{mm:00}:{ss:00}:{cs:00}";
    }

    void UpdateLivesUI()
    {
        if (!livesPanel) return;
        for (int i = 0; i < livesPanel.childCount; i++)
            livesPanel.GetChild(i).gameObject.SetActive(i < lives);
    }

    void SetScaredUI(bool visible, float seconds)
    {
        if (!txtScared) return;
        txtScared.gameObject.SetActive(visible);
        if (visible) txtScared.text = Mathf.CeilToInt(seconds).ToString();
    }

    void TrySaveHighScore()
    {
        int prevScore = PlayerPrefs.GetInt(KeyHighScore, 0);
        float prevBestTime = PlayerPrefs.GetFloat(KeyBestTime, float.MaxValue);

        bool better = (score > prevScore) || (score == prevScore && timeElapsed < prevBestTime);
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
