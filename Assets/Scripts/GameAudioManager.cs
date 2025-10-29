using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip introMusic;
    public AudioClip normalMusic;

    // Start is called before the first frame update
    void Start()
    {
        audioSource.clip = introMusic;
        audioSource.Play();
        Invoke("PlayNormalMusic", 3f);
    }

    // Update is called once per frame
    void Update()
    {
    }
    void PlayNormalMusic()
    {
        audioSource.clip = normalMusic;
        audioSource.loop = true;
        audioSource.Play();
    }
}
