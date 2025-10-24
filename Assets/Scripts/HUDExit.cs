using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDExit : MonoBehaviour
{
    public string startScene = "StartScene";
    public void ExitToStart() => SceneManager.LoadScene(startScene);
}

