using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource backgroundSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip logoAppearSFX;
    [SerializeField] private AudioClip drumHitSFX;
    [SerializeField] private AudioClip samuraiDashSFX;
    [SerializeField] private AudioClip swordClash1SFX;
    [SerializeField] private AudioClip swordClash2SFX;
    [SerializeField] private AudioClip swordClash3SFX;
    [SerializeField] private AudioClip japaneseDrumSFX;
    [SerializeField] private AudioClip swordUnsheathSFX;
    [SerializeField] private AudioClip heavyDrumSFX;
    [SerializeField] private AudioClip hurtSFX;

    [Header("Background Music")]
    [SerializeField] private AudioClip gameBGM;
    [SerializeField] private AudioClip forestWindBG;

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float backgroundVolume = 0.5f;

    private Coroutine bgmFadeCoroutine;
    private Coroutine backgroundFadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        if (bgmSource == null)
        {
            GameObject bgmObject = new GameObject("BGM AudioSource");
            bgmObject.transform.SetParent(transform);
            bgmSource = bgmObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFX AudioSource");
            sfxObject.transform.SetParent(transform);
            sfxSource = sfxObject.AddComponent<AudioSource>();
        }

        if (backgroundSource == null)
        {
            GameObject backgroundObject = new GameObject("Background AudioSource");
            backgroundObject.transform.SetParent(transform);
            backgroundSource = backgroundObject.AddComponent<AudioSource>();
        }

        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;
        backgroundSource.volume = backgroundVolume;
    }

    #region Sound Effect Methods
    public void PlayLogoAppear()
    {
        PlaySFX(logoAppearSFX);
    }

    public void PlayDrumHit()
    {
        PlaySFX(drumHitSFX);
    }

    public void PlaySamuraiDash()
    {
        PlaySFX(samuraiDashSFX);
    }

    public void PlaySwordClash1()
    {
        PlaySFX(swordClash1SFX);
    }

    public void PlaySwordClash2()
    {
        PlaySFX(swordClash2SFX);
    }

    public void PlaySwordClash3()
    {
        PlaySFX(swordClash3SFX);
    }

    public void PlayRandomSwordClash()
    {
        int randomIndex = Random.Range(1, 4);
        switch (randomIndex)
        {
            case 1: PlaySwordClash1(); break;
            case 2: PlaySwordClash2(); break;
            case 3: PlaySwordClash3(); break;
        }
    }

    /// <summary>
    /// 根据攻击类型播放对应的剑击音效
    /// AttackX -> SwordClash1, AttackY -> SwordClash2, AttackB -> SwordClash3
    /// </summary>
    public void PlayAttackSound(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.AttackX:
                PlaySwordClash1();
                break;
            case AttackType.AttackY:
                PlaySwordClash2();
                break;
            case AttackType.AttackB:
                PlaySwordClash3();
                break;
        }
    }

    public void PlayJapaneseDrum()
    {
        PlaySFX(japaneseDrumSFX);
    }

    public void PlayHeavyDrum()
    {
        PlaySFX(heavyDrumSFX);
    }

    public void PlayHurt()
    {
        PlaySFX(hurtSFX);
    }

    public void PlaySwordUnsheath()
    {
        PlaySFX(swordUnsheathSFX);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    #endregion

    #region BGM Controls
    public void PlayBGM()
    {
        if (gameBGM != null && bgmSource != null)
        {
            bgmSource.clip = gameBGM;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
        
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }
    }

    public void FadeInBGM(float fadeTime = 2f)
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        bgmFadeCoroutine = StartCoroutine(FadeInCoroutine(bgmSource, fadeTime, bgmVolume));
        PlayBGM();
    }

    public void FadeOutBGM(float fadeTime = 2f, bool stopAfterFade = true)
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        bgmFadeCoroutine = StartCoroutine(FadeOutCoroutine(bgmSource, fadeTime, stopAfterFade));
    }
    #endregion

    #region Background Audio Controls
    public void PlayForestWind()
    {
        if (forestWindBG != null && backgroundSource != null)
        {
            backgroundSource.clip = forestWindBG;
            backgroundSource.loop = true;
            backgroundSource.Play();
        }
    }

    public void StopBackgroundAudio()
    {
        if (backgroundSource != null)
        {
            backgroundSource.Stop();
        }
        
        if (backgroundFadeCoroutine != null)
        {
            StopCoroutine(backgroundFadeCoroutine);
            backgroundFadeCoroutine = null;
        }
    }

    public void FadeInBackgroundAudio(float fadeTime = 2f)
    {
        if (backgroundFadeCoroutine != null)
        {
            StopCoroutine(backgroundFadeCoroutine);
        }
        backgroundFadeCoroutine = StartCoroutine(FadeInCoroutine(backgroundSource, fadeTime, backgroundVolume));
        PlayForestWind();
    }

    public void FadeOutBackgroundAudio(float fadeTime = 2f, bool stopAfterFade = true)
    {
        if (backgroundFadeCoroutine != null)
        {
            StopCoroutine(backgroundFadeCoroutine);
        }
        backgroundFadeCoroutine = StartCoroutine(FadeOutCoroutine(backgroundSource, fadeTime, stopAfterFade));
    }
    #endregion

    #region Fade Coroutines
    private IEnumerator FadeInCoroutine(AudioSource source, float fadeTime, float targetVolume)
    {
        source.volume = 0f;
        float currentTime = 0f;

        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, currentTime / fadeTime);
            yield return null;
        }

        source.volume = targetVolume;
        
        if (source == bgmSource)
            bgmFadeCoroutine = null;
        else if (source == backgroundSource)
            backgroundFadeCoroutine = null;
    }

    private IEnumerator FadeOutCoroutine(AudioSource source, float fadeTime, bool stopAfterFade)
    {
        float startVolume = source.volume;
        float currentTime = 0f;

        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeTime);
            yield return null;
        }

        source.volume = 0f;
        
        if (stopAfterFade)
        {
            source.Stop();
        }

        if (source == bgmSource)
            bgmFadeCoroutine = null;
        else if (source == backgroundSource)
            backgroundFadeCoroutine = null;
    }
    #endregion

    #region Volume Controls
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void SetBackgroundVolume(float volume)
    {
        backgroundVolume = Mathf.Clamp01(volume);
        if (backgroundSource != null)
            backgroundSource.volume = backgroundVolume;
    }
    #endregion
}