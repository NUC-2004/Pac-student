// Assets/Scripts/GameHUD.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI txtScore;
    public TextMeshProUGUI txtTimer;
    public TextMeshProUGUI txtScared; // 鬼害怕倒计时（开始隐藏）
    public TextMeshProUGUI txtLevelName;

    [Header("Lives")]
    public GameObject[] lifeIcons; // 3 个心

    [Header("Round UI")]
    public Image blocker;               // 半透明覆盖图（默认隐藏）
    public TextMeshProUGUI txtCountdown;// 大字 3/2/1/GO（默认隐藏）

    public void SetScore(int score)
    {
        if (txtScore) txtScore.text = score.ToString("000000");
    }

    public void SetTimer(float t)
    {
        int mm = Mathf.FloorToInt(t / 60f);
        int ss = Mathf.FloorToInt(t % 60f);
        int cs = Mathf.FloorToInt((t - Mathf.Floor(t)) * 100f); // 两位“毫秒”
        if (txtTimer) txtTimer.text = $"{mm:00}:{ss:00}:{cs:00}";
    }

    public void SetLives(int lives)
    {
        for (int i = 0; i < lifeIcons.Length; i++)
            if (lifeIcons[i]) lifeIcons[i].SetActive(i < lives);
    }

    public void ShowScaredTimer(int seconds)
    {
        if (txtScared)
        {
            txtScared.gameObject.SetActive(seconds > 0);
            if (seconds > 0) txtScared.text = seconds.ToString();
        }
    }

    public void ShowBlock(bool on)
    {
        if (blocker) blocker.gameObject.SetActive(on);
    }

    public void ShowCountdown(string text, bool on)
    {
        if (txtCountdown)
        {
            txtCountdown.gameObject.SetActive(on);
            if (on) txtCountdown.text = text;
        }
    }
}
