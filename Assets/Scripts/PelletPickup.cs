using UnityEngine;

/// <summary>
/// 小豆：玩家触发即+分、播音效、销毁、自减清关计数
/// 需求：本物体携带 2D Trigger Collider；玩家物体 Tag = "Player"
/// </summary>
public class PelletPickup : MonoBehaviour
{
    [Tooltip("吃到该小豆获得的分数")]
    public int points = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 加分
        if (HUDController.I != null) HUDController.I.AddScore(points);
        else if (ScoreKeeper.I != null) ScoreKeeper.I.AddScore(points);

        // 播放吃豆音效（若有）
        GameAudioManager.I?.PlayPellet();

        // 销毁豆子本体
        Destroy(gameObject);

        // 通知规则：清关计数 -1
        GameRule70.I?.OnPelletEaten();
    }
}

