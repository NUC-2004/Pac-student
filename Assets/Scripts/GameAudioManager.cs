using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager I { get; private set; }

    [Header("Music")]
    public AudioSource musicSource;       // 背景音乐
    public AudioClip levelMusic;          // 常驻关卡音乐

    [Header("SFX")]
    public AudioSource sfxSource;         // 音效通道
    public AudioClip stepClip;            // 普通走动/咔哒
    public AudioClip pelletClip;          // 吃普通豆
    public AudioClip powerPelletClip;     // 吃能量豆（新增）

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => PlayLevelMusic();

    public void PlayLevelMusic()
    {
        if (musicSource == null || levelMusic == null) return;
        if (musicSource.clip == levelMusic && musicSource.isPlaying) return;
        musicSource.loop = true;
        musicSource.clip = levelMusic;
        musicSource.Play();
    }

    public void PlayStep(bool eatingNext)
    {
        var clip = eatingNext ? pelletClip : stepClip;
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayPellet() => PlayStep(true);
    public void PlayMoveTick() => PlayStep(false);

    // 新增：能量豆音效
    public void PlayPowerPellet()
    {
        if (sfxSource != null && powerPelletClip != null)
            sfxSource.PlayOneShot(powerPelletClip);
        else
            PlayPellet(); // 没设置专用音效时退化为普通吃豆声
    }
}
