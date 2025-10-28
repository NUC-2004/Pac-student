using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRule : MonoBehaviour
{
    public static GameRule I;

    int remaining;          
    [Header("Optional")]
    public string startScene = "StartScene"; 
    public float backDelay = 1.0f;          

    void Awake() => I = this;

    void Start()
    {
        remaining = FindObjectsOfType<PelletPickup>(true).Length
                  + FindObjectsOfType<PowerPelletPickup>(true).Length;
   
    }

    public void OnPelletEaten()
    {
        remaining--;
        if (remaining <= 0)
            Invoke(nameof(ReturnToStart), backDelay);
    }

    void ReturnToStart()
    {
        if (!string.IsNullOrEmpty(startScene))
            SceneManager.LoadScene(startScene);
    }
}

