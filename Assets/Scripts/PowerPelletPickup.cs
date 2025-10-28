using UnityEngine;

public class PowerPelletPickup : MonoBehaviour
{
    public int points = 50;
    public float scaredSeconds = 7f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        HUDController.I?.AddScore(points);
        HUDController.I?.StartScared(scaredSeconds);
        GameAudioManager.I?.PlayPowerPellet();   
        GameRule.I?.OnPelletEaten();         

        Destroy(gameObject);
    }
}