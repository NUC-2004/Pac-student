using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController I;

    [Header("Refs")]
    public TMP_Text txtTimer;    // HUD/Txt_Timer
    public TMP_Text txtScore;    // HUD/Txt_Score
    public TMP_Text txtScared;   // HUD/Txt_Scared

    // timer
    float timeSec;
    bool running = true;

    // score
    int score = 0;

    // scared countdown
    float scaredLeft = 0f;

    void Awake()
    {
        if (I == null) I = this; else Destroy(gameObject);
        if (txtScore) txtScore.text = "000000";
        if (txtScared) txtScared.text = "";
    }

    void Update()
    {
        // 计时显示
        if (running && txtTimer)
        {
            timeSec += Time.deltaTime;
            int minutes = (int)(timeSec / 60f);
            int seconds = (int)(timeSec % 60f);
            int ms2 = (int)((timeSec - Mathf.Floor(timeSec)) * 100f);
            txtTimer.text = $"{minutes:00}:{seconds:00}:{ms2:00}";
        }

        // 受惊倒计时（按秒显示，帧间平滑递减）
        if (txtScared)
        {
            if (scaredLeft > 0f)
            {
                scaredLeft -= Time.deltaTime;
                int leftInt = Mathf.Max(0, Mathf.CeilToInt(scaredLeft));
                txtScared.text = leftInt.ToString();
            }
            else
            {
                txtScared.text = "";
            }
        }
    }

    // === 计时 API ===
    public void ResetTimer(float start = 0f) { timeSec = start; }
    public void PauseTimer() { running = false; }
    public void ResumeTimer() { running = true; }

    // === 计分 API ===
    public void SetScore(int newScore)
    {
        score = Mathf.Max(0, newScore);
        if (txtScore) txtScore.text = score.ToString("D6");
    }
    public void AddScore(int delta) => SetScore(score + delta);

    // === 受惊计时 API（内部按秒递减显示） ===
    public void StartScared(float durationSec) { scaredLeft = Mathf.Max(0f, durationSec); }

    // ===== 兼容 ScoreKeeper 的最小适配补充 =====

    // 计时清零并开始
    public void ResetTimerAndStart()
    {
        ResetTimer(0f);
        ResumeTimer();
    }

    // 兼容布尔暂停：true=暂停，false=继续
    public void PauseTimer(bool pause)
    {
        running = !pause;
    }

    // 直接设置“受惊剩余秒数”（<=0 则清空）
    public void ShowScaredSeconds(int seconds)
    {
        if (seconds <= 0)
        {
            scaredLeft = 0f;
            if (txtScared) txtScared.text = "";
        }
        else
        {
            scaredLeft = seconds;              // 让 Update 用 Time.deltaTime 平滑递减
            if (txtScared) txtScared.text = seconds.ToString();
        }
    }
}
