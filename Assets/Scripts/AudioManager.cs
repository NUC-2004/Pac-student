using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    [Header("Sources")]
    public AudioSource bgmSource;   // —≠ª∑≤•∑≈BGM£®2D£©
    public AudioSource sfxSource;   // ≤•∑≈∂Ã“Ù–ß£®2D£©

    [Header("SFX Clips")]
    public AudioClip pelletSfx;     // ≥‘–°∂π
    public AudioClip powerPelletSfx;// ≥‘¥Û¡¶ÕË
    public AudioClip eatGhostSfx;   // ≥‘”ƒ¡È

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- BGM ----
    public void PlayBgm(AudioClip clip, bool loop = true)
    {
        if (!bgmSource) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.loop = loop;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // ---- SFX ----
    public void PlayPellet() { PlaySfx(pelletSfx); }
    public void PlayPowerPellet() { PlaySfx(powerPelletSfx); }
    public void PlayEatGhost() { PlaySfx(eatGhostSfx); }

    public void PlaySfx(AudioClip clip, float vol = 1f)
    {
        if (sfxSource && clip) sfxSource.PlayOneShot(clip, vol);
    }
}
