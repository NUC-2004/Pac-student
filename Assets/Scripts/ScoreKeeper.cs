using UnityEngine;

public class ScoreKeeper : MonoBehaviour
{
    public static ScoreKeeper I { get; private set; }

    // 只读属性（给别的脚本访问）
    public int Score => _score;
    public float Elapsed => _elapsed;

    int _score = 0;
    float _elapsed = 0f;
    bool _running = false;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        // 如果希望跨场景保留，就解开这行
        // DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (_running)
        {
            _elapsed += Time.deltaTime;
        }
    }

    // —— 计分相关 ——
    public void ResetScore(int start = 0)
    {
        _score = Mathf.Max(0, start);
        HUDController.I?.SetScore(_score);
    }

    public void AddScore(int delta)
    {
        _score = Mathf.Max(0, _score + delta);
        HUDController.I?.SetScore(_score);
    }

    // —— 计时相关 ——
    public void ResetTimer(float start = 0f)
    {
        _elapsed = Mathf.Max(0f, start);
        HUDController.I?.ResetTimer(_elapsed);
    }

    public void PauseTimer()
    {
        _running = false;
        HUDController.I?.PauseTimer();
    }

    public void ResumeTimer()
    {
        _running = true;
        HUDController.I?.ResumeTimer();
    }
}

