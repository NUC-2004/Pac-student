using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RoundManager70 : MonoBehaviour
{
    [Header("Refs (拖引用)")]
    public PacStudentController player;   // Player 上的脚本，需有 public bool controlsEnabled
    public TextMeshProUGUI countdownText; // 大字：3 / 2 / 1 / GO!
    public Image blocker;                 // 半透明拦屏图（UI Image）
    public TextMeshProUGUI timerText;     // 计时器文本 mm:ss:cc

    [Header("Timing")]
    public float goHoldSeconds = 1f;      // “GO!” 持续时间

    bool running = false;
    float elapsed = 0f;

    void Start()
    {
        // 开局拦输入
        if (player) player.controlsEnabled = false;

        if (blocker) blocker.gameObject.SetActive(true);
        if (countdownText) countdownText.gameObject.SetActive(true);

        StartCoroutine(CoCountdown());
    }

    IEnumerator CoCountdown()
    {
        if (countdownText) countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        if (countdownText) countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        if (countdownText) countdownText.text = "1";
        yield return new WaitForSeconds(1f);
        if (countdownText) countdownText.text = "GO!";
        yield return new WaitForSeconds(goHoldSeconds);

        // 开始游戏：放开输入、隐藏遮罩
        if (countdownText) countdownText.gameObject.SetActive(false);
        if (blocker) blocker.gameObject.SetActive(false);
        if (player) player.controlsEnabled = true;
        running = true;
    }

    void Update()
    {
        if (!running) return;

        elapsed += Time.deltaTime;
        if (timerText)
        {
            int mm = Mathf.FloorToInt(elapsed / 60f);
            int ss = Mathf.FloorToInt(elapsed % 60f);
            int cs = Mathf.FloorToInt((elapsed - Mathf.Floor(elapsed)) * 100f); // 百分之一秒
            timerText.text = $"{mm:00}:{ss:00}:{cs:00}";
        }
    }
}

