using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class StartMusicLoop : MonoBehaviour
{
    void Awake()
    {
        var src = GetComponent<AudioSource>();
        src.loop = true;
        if (!src.isPlaying) src.Play();
    }
}

