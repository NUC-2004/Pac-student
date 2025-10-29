using UnityEngine;
using TMPro;

public class StartHighScoreUI : MonoBehaviour
{
    public TMP_Text txtHighScore;
    public TMP_Text txtBestTime;

    void Start()
    {
        int s = PlayerPrefs.GetInt("BestScore", 0);
        float t = PlayerPrefs.GetFloat("BestTime", 0f);

        if (txtHighScore) txtHighScore.text = s.ToString("D6");
        if (txtBestTime) txtBestTime.text = FormatTime(t);
    }

    string FormatTime(float sec)
    {
        int m = (int)(sec / 60f);
        int s = (int)(sec % 60f);
        int cs = (int)((sec - Mathf.Floor(sec)) * 100f); // 百分之一秒
        return $"{m:00}:{s:00}:{cs:00}";
    }
}