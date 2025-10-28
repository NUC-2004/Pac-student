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
        if (running && txtTimer)
        {
            timeSec += Time.deltaTime;
            int minutes = (int)(timeSec / 60f);
            int seconds = (int)(timeSec % 60f);
            int ms2 = (int)((timeSec - Mathf.Floor(timeSec)) * 100f);
            txtTimer.text = $"{minutes:00}:{seconds:00}:{ms2:00}";
        }

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

    public void ResetTimer(float start = 0f) { timeSec = start; }
    public void PauseTimer() { running = false; }
    public void ResumeTimer() { running = true; }

    public void SetScore(int newScore)
    {
        score = Mathf.Max(0, newScore);
        if (txtScore) txtScore.text = score.ToString("D6");
    }
    public void AddScore(int delta) => SetScore(score + delta);

    public void StartScared(float durationSec) { scaredLeft = Mathf.Max(0f, durationSec); }
}

