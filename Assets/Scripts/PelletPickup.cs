using UnityEngine;

public class PelletPickup : MonoBehaviour
{
    public int points = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (HUDController.I != null) HUDController.I.AddScore(points);
        else if (ScoreKeeper.I != null) ScoreKeeper.I.AddScore(points);

        //  播吃豆音效（修正：使用 AudioManager）
        AudioManager.I?.PlayPellet();

        Destroy(gameObject);

        GameRule70.I?.OnPelletEaten();
    }
}

