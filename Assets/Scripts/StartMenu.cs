using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [Header("Scene Names")]
    public string level1Scene = "SampleScene";      // ÄãµÄA3³¡¾°Ãû
  

    public void LoadLevel1() => SceneManager.LoadScene(level1Scene);
   
}

