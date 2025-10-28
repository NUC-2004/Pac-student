using UnityEngine;

public class PelletPickup : MonoBehaviour
{
    public int points = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        HUDController.I?.AddScore(points);
        GameAudioManager.I?.PlayPellet();


        Destroy(gameObject);
    }
}

