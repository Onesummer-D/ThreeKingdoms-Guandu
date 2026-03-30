using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("音频源（自动创建）")]
    public AudioSource bgmSource;       // BGM专用
    public AudioSource sfxSource;       // 剧情音效专用
    public AudioSource uiSource;        // UI音效专用

    [Header("BGM资源（在Inspector中拖拽赋值）")]
    public AudioClip warBackgroundBGM;      // 战争背景音(1) - 首页用
    public AudioClip bgmBackgroundIntro;    // 剧情点一（背景介绍用）
    public AudioClip bgmPlot1;              // 剧情点一
    public AudioClip bgmPlot2;              // 剧情点二
    public AudioClip bgmPlot3;              // 剧情点三
    public AudioClip bgmPlot4Normal;        // 剧情点四（前半）
    public AudioClip bgmPlot4Battle;        // 剧情点四-准备战斗（后半）
    public AudioClip bgmPlot5Battle;        // 剧情点五-战斗
    public AudioClip bgmPlot5Victory;       // 剧情点五-胜利（也用于if线5）
    public AudioClip bgmIfFailure;          // if-失败
    public AudioClip bgmIfVictory;          // if-胜利(1)
    public AudioClip bgmFireWuchao;         // 火烧乌巢（特殊BGM）

    [Header("通用音效")]
    public AudioClip defaultButtonClick;    // 选项点击音
    public AudioClip valueChangeUpSFX;      // 数值增加
    public AudioClip valueChangeDownSFX;    // 数值减少
    public AudioClip puzzleCorrectSFX;      // 拼图正确
    public AudioClip puzzleMoveSFX;         // 移动一格
    public AudioClip alertSFX;              // 警报
    public AudioClip errorSFX;              // 错误
    public AudioClip achievementSFX;        // 达成成就（走格子胜利用）
    public AudioClip craftSuccessSFX;       // 打造成功（拼图完成用）
    public AudioClip fireWuchaoSFX;         // 火烧乌巢音效（SFX）

    [Header("挖地道专用音效")]
    public AudioClip digTunnelDiggingSFX;    // 园艺-用铲子（挖掘中）
    public AudioClip digTunnelSuccessSFX;    // 打造成功（挖通）

    [Header("音量控制")]
    [Range(0f, 1f)] public float bgmVolume = 1f;

    private float originalBGMVolume = 1f;
    private float currentBGMVolume = 1f;
    private Coroutine currentBGMCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 自动创建音频源
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();

            bgmSource.loop = true;
            sfxSource.playOnAwake = false;
            uiSource.playOnAwake = false;

            // ✅ 音效默认音量调低（走格子保持1.0，其他0.6）
            sfxSource.volume = 0.6f;
            uiSource.volume = 0.5f;

            // ✅ 强制清空缓存
            if (bgmSource != null)
            {
                bgmSource.clip = null; // 清空缓存
                bgmSource.Stop();
            }

            // 加载保存的音量（如果没有保存过则默认为1）
            currentBGMVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
            if (currentBGMVolume <= 0.01f) currentBGMVolume = 1f;
            originalBGMVolume = currentBGMVolume;
            bgmVolume = currentBGMVolume;

            // 立即应用音量
            if (bgmSource != null) bgmSource.volume = currentBGMVolume;

            // 首页立即播放BGM
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0 ||
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Menu"))
            {
                if (warBackgroundBGM != null)
                {
                    bgmSource.clip = warBackgroundBGM;
                    bgmSource.loop = true;
                    bgmSource.volume = currentBGMVolume;
                    bgmSource.Play();
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 播放BGM（带过渡效果：淡出→停顿→淡入）
    public void PlayBGM(AudioClip clip, bool loop = true, bool stopPrevious = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] 传入的BGM为null，保持当前BGM");
            return;
        }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        if (currentBGMCoroutine != null)
            StopCoroutine(currentBGMCoroutine);

        currentBGMCoroutine = StartCoroutine(PlayBGMWithTransition(clip, loop, stopPrevious));
    }

    private IEnumerator PlayBGMWithTransition(AudioClip newClip, bool loop, bool stopPrevious)
    {
        // 极速模式：淡出0.05秒，不停顿，淡入0.1秒
        float fadeOut = 0.05f;   // 原来0.15→0.05
        float pauseTime = 0f;    // 原来0.1→0（不停顿！）
        float fadeIn = 0.1f;     // 原来0.3→0.1

        // 1. 淡出旧BGM（超快）
        if (stopPrevious && bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            for (float t = 0; t < fadeOut; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0, t / fadeOut);
                yield return null;
            }
            bgmSource.Stop();
        }

        // 2. 不停顿，直接播新的
        yield return new WaitForSeconds(pauseTime); // 这行可以删掉，但留着也没事

        // 3. 淡入新BGM（超快）
        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        bgmSource.volume = 0;
        bgmSource.Play();

        for (float t = 0; t < fadeIn; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, currentBGMVolume, t / fadeIn);
            yield return null;
        }
        bgmSource.volume = currentBGMVolume;
    }

    // 停止BGM
    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    // 暂停BGM
    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Pause();
    }

    // 恢复BGM
    public void ResumeBGM()
    {
        if (bgmSource != null && bgmSource.clip != null)
            bgmSource.UnPause();
    }

    // 降低BGM音量（小游戏用）
    public void ReduceBGMVolume(float targetVolume = 0.2f)
    {
        StopCoroutine("FadeVolume");
        StartCoroutine(FadeVolume(bgmSource.volume, targetVolume, 0.3f));
    }

    // 恢复BGM音量
    public void RestoreBGMVolume()
    {
        StopCoroutine("FadeVolume");
        StartCoroutine(FadeVolume(bgmSource.volume, currentBGMVolume, 0.3f));
    }

    public void SetBGMVolume(float volume)
    {
        currentBGMVolume = Mathf.Clamp01(volume);
        originalBGMVolume = currentBGMVolume;

        StopCoroutine("FadeVolume");
        if (currentBGMCoroutine != null)
            StopCoroutine(currentBGMCoroutine);

        if (bgmSource != null)
        {
            bgmSource.volume = currentBGMVolume;
        }

        PlayerPrefs.SetFloat("BGMVolume", currentBGMVolume);
        bgmVolume = currentBGMVolume;
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        bgmSource.volume = to;
    }

    // ✅ 播放剧情音效（默认60%音量）
    public void PlaySFX(AudioClip clip, float volume = 0.6f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // ✅ 播放UI音效（默认50%音量）
    public void PlayUI(AudioClip clip, float volume = 0.5f)
    {
        if (clip == null) clip = defaultButtonClick;
        if (clip != null) uiSource.PlayOneShot(clip, volume);
    }

    // ✅ 数值变化音效（50%音量）
    public void PlayValueChange(float change)
    {
        if (change > 0 && valueChangeUpSFX != null)
            uiSource.PlayOneShot(valueChangeUpSFX, 0.5f);
        else if (change < 0 && valueChangeDownSFX != null)
            uiSource.PlayOneShot(valueChangeDownSFX, 0.5f);
    }

    // 挖地道挖掘音效（60%音量）
    public void PlayDigging()
    {
        if (digTunnelDiggingSFX != null)
            sfxSource.PlayOneShot(digTunnelDiggingSFX, 0.6f);
    }

    // 挖地道成功音效 - 改用达成成就音效（保持100%，因为这个重要）
    public void PlayDigSuccess()
    {
        if (achievementSFX != null)
            sfxSource.PlayOneShot(achievementSFX, 1.0f);
    }

    // ✅ 播放达成成就音效（走格子胜利用）- 保持100%不变
    public void PlayAchievement()
    {
        if (achievementSFX != null)
            sfxSource.PlayOneShot(achievementSFX, 1.0f);
    }

    // ✅ 播放打造成功音效（拼图用）- 60%音量
    public void PlayCraftSuccess()
    {
        if (craftSuccessSFX != null)
            sfxSource.PlayOneShot(craftSuccessSFX, 0.6f);
    }

    // 小游戏快捷接口（60%音量）
    public void PlayPuzzleCorrect() => PlaySFX(puzzleCorrectSFX, 0.3f);
    public void PlayPuzzleMove() => PlaySFX(puzzleMoveSFX, 1.0f);  // 走格子保持1.0
    public void PlayAlert() => PlaySFX(alertSFX, 0.6f);
    public void PlayError() => PlaySFX(errorSFX, 0.6f);
}