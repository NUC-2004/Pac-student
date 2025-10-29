using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager I { get; private set; }

    [Header("Music")]
    public AudioSource musicSource;   // ±≥æ∞“Ù¿÷
    public AudioClip levelMusic;      // ≥£πÊBGM
    public AudioClip scaredMusic;     // ”ƒ¡È ‹æ™BGM

    [Header("SFX")]
    public AudioSource sfxSource;     // “Ù–ßAudioSource
    public AudioClip stepClip;        // ◊ﬂ¬∑°∞ﬂ«ﬂ’°±
    public AudioClip pelletClip;      // ≥‘∆’Õ®∂π
    public AudioClip cherryClip;      // ≥‘”£Ã“
    public AudioClip powerPelletClip; // ≥‘ƒ‹¡ø∂π

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===== BGM =====
    public void PlayLevelMusic()
    {
        if (!musicSource || !levelMusic) return;
        if (musicSource.clip == levelMusic && musicSource.isPlaying) return;
        musicSource.loop = true;
        musicSource.clip = levelMusic;
        musicSource.Play();
    }

    public void PlayScaredMusic()
    {
        if (!musicSource || !scaredMusic) return;
        if (musicSource.clip == scaredMusic && musicSource.isPlaying) return;
        musicSource.loop = true;
        musicSource.clip = scaredMusic;
        musicSource.Play();
    }

    // ===== SFX =====
    public void PlayMoveTick()
    {
        if (sfxSource && stepClip) sfxSource.PlayOneShot(stepClip);
    }

    public void PlayPellet()
    {
        if (sfxSource && pelletClip) sfxSource.PlayOneShot(pelletClip);
    }

    public void PlayCherry()
    {
        if (sfxSource && cherryClip) sfxSource.PlayOneShot(cherryClip);
    }

    public void PlayPowerPellet()
    {
        if (sfxSource && powerPelletClip) sfxSource.PlayOneShot(powerPelletClip);
    }
}
