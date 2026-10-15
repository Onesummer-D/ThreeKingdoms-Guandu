using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public AudioClip fireWuchaoSFX;         // 预留专用燃烧SFX：素材库暂无合适音频，禁止误用箭矢音。火烧动画已另配BGM。

    [Header("挖地道专用音效")]
    public AudioClip digTunnelDiggingSFX;    // 园艺-用铲子（挖掘中）
    public AudioClip digTunnelSuccessSFX;    // 打造成功（挖通）
    public AudioClip paperTurnSFX;
    public AudioClip gridStepSFX;

    [Header("音量控制")]
    [Range(0f, 1f)] public float bgmVolume = 0.75f;

    // Keep the music fader, gameplay ducking and individual volume sliders
    // independent, so changing a slider never unexpectedly cancels a cue.
    private float currentBGMVolume = 0.75f;
    private float currentEffectsVolume = 0.9f;
    private float transitionGain = 1f;
    private float gameplayDuck = 1f;
    private float effectDuck = 1f;
    private float duckUntil;
    private Coroutine currentBGMCoroutine;
    private Coroutine volumeCoroutine;
    private Coroutine arrowRainCoroutine;
    private readonly Dictionary<AudioClip, float> nextAllowedPlay = new Dictionary<AudioClip, float>();
    private readonly List<float> activeEffectEnds = new List<float>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        sfxSource.playOnAwake = false;
        uiSource.playOnAwake = false;
        // Stereo UI and narrative sounds must not attenuate with 3D distance.
        bgmSource.spatialBlend = 0f;
        sfxSource.spatialBlend = 0f;
        uiSource.spatialBlend = 0f;

        currentBGMVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("BGMVolume", 0.75f));
        currentEffectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("EffectsVolume", 0.9f));
        bgmVolume = currentBGMVolume;
        sfxSource.volume = currentEffectsVolume;
        uiSource.volume = currentEffectsVolume;
        bgmSource.Stop();
        bgmSource.clip = null;
        UpdateMusicMix();

        // Start music only on a dedicated opening/menu scene. The story
        // selects its track through DialogueSystem on the first shown node.
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if ((scene.buildIndex == 0 || scene.name.Contains("Menu")) && warBackgroundBGM != null)
        {
            bgmSource.clip = warBackgroundBGM;
            bgmSource.Play();
        }
    }

    void Update()
    {
        // Duck for the audible portion of an important narrative SFX, then
        // release slowly to avoid an abrupt music jump between story nodes.
        float target = Time.unscaledTime < duckUntil ? 0.28f : 1f;
        float seconds = target < effectDuck ? 0.10f : 0.48f;
        effectDuck = Mathf.MoveTowards(effectDuck, target,
            Time.unscaledDeltaTime * (1f - 0.28f) / seconds);
        UpdateMusicMix();
    }

    private void UpdateMusicMix()
    {
        if (bgmSource == null) return;
        bgmSource.volume = Mathf.Clamp01(currentBGMVolume * transitionGain * gameplayDuck * effectDuck);
    }

    public void PlayBGM(AudioClip clip, bool loop = true, bool stopPrevious = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] 传入的BGM为null，保持当前BGM");
            return;
        }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        if (currentBGMCoroutine != null) StopCoroutine(currentBGMCoroutine);
        currentBGMCoroutine = StartCoroutine(PlayBGMWithTransition(clip, loop, stopPrevious));
    }

    private IEnumerator PlayBGMWithTransition(AudioClip newClip, bool loop, bool stopPrevious)
    {
        if (stopPrevious && bgmSource.isPlaying)
        {
            float start = transitionGain;
            const float fadeOut = 0.28f;
            for (float elapsed = 0f; elapsed < fadeOut; elapsed += Time.unscaledDeltaTime)
            {
                transitionGain = Mathf.Lerp(start, 0f, elapsed / fadeOut);
                UpdateMusicMix();
                yield return null;
            }
            bgmSource.Stop();
        }

        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        transitionGain = 0f;
        UpdateMusicMix();
        bgmSource.Play();

        const float fadeIn = 0.65f;
        for (float elapsed = 0f; elapsed < fadeIn; elapsed += Time.unscaledDeltaTime)
        {
            transitionGain = Mathf.Clamp01(elapsed / fadeIn);
            UpdateMusicMix();
            yield return null;
        }
        transitionGain = 1f;
        UpdateMusicMix();
        currentBGMCoroutine = null;
    }

    public void StopBGM()
    {
        if (currentBGMCoroutine != null) StopCoroutine(currentBGMCoroutine);
        currentBGMCoroutine = null;
        transitionGain = 1f;
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying) bgmSource.Pause();
    }

    public void ResumeBGM()
    {
        if (bgmSource != null && bgmSource.clip != null) bgmSource.UnPause();
    }

    // Mini-game mix is separate from the temporary effect duck.
    public void ReduceBGMVolume(float targetVolume = 0.2f)
    {
        FadeGameplayDuck(Mathf.Clamp01(targetVolume));
    }

    public void RestoreBGMVolume()
    {
        FadeGameplayDuck(1f);
    }

    private void FadeGameplayDuck(float target)
    {
        if (volumeCoroutine != null) StopCoroutine(volumeCoroutine);
        volumeCoroutine = StartCoroutine(FadeGameplayDuckRoutine(target));
    }

    private IEnumerator FadeGameplayDuckRoutine(float target)
    {
        float initial = gameplayDuck;
        const float duration = 0.30f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            gameplayDuck = Mathf.Lerp(initial, target, elapsed / duration);
            UpdateMusicMix();
            yield return null;
        }
        gameplayDuck = target;
        UpdateMusicMix();
        volumeCoroutine = null;
    }

    public void SetBGMVolume(float volume)
    {
        currentBGMVolume = Mathf.Clamp01(volume);
        bgmVolume = currentBGMVolume;
        PlayerPrefs.SetFloat("BGMVolume", currentBGMVolume);
        UpdateMusicMix();
    }

    public float GetBGMVolume() { return currentBGMVolume; }

    public void SetEffectsVolume(float volume)
    {
        currentEffectsVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = currentEffectsVolume;
        if (uiSource != null) uiSource.volume = currentEffectsVolume;
        PlayerPrefs.SetFloat("EffectsVolume", currentEffectsVolume);
    }

    public float GetEffectsVolume() { return currentEffectsVolume; }

    // Callers provide a relative cue emphasis; master effects slider provides
    // the actual gain. Old defaults multiplied 0.6 * 0.3 and buried cues.
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        PlayControlled(sfxSource, clip, volume, true);
    }

    // The intro/IF arrow rain is a short multi-shot sequence, not the same
    // one-shot used for isolated arrows. The individual clip is ~0.4 s; the
    // staggered volleys give the player time to recognize an incoming barrage.
    public void PlayArrowRain(AudioClip clip)
    {
        StopArrowRain();
        if (clip == null || sfxSource == null || currentEffectsVolume <= 0f ||
            AudioListener.volume <= 0.001f) return;

        // Keep the music below the entire volley, not only the first arrow.
        const float volleyDuration = 2.0f;
        duckUntil = Mathf.Max(duckUntil, Time.unscaledTime + volleyDuration);
        effectDuck = Mathf.Min(effectDuck, 0.20f);
        UpdateMusicMix();
        arrowRainCoroutine = StartCoroutine(ArrowRainRoutine(clip));
    }

    private IEnumerator ArrowRainRoutine(AudioClip clip)
    {
        // Slightly irregular attacks sound less like a button being pressed
        // repeatedly. Short overlaps evoke several archers firing at once.
        float[] intervals = { 0.18f, 0.29f, 0.16f, 0.35f, 0.20f, 0.15f, 0.25f };
        for (int i = 0; i <= intervals.Length; i++)
        {
            if (sfxSource != null && currentEffectsVolume > 0f &&
                AudioListener.volume > 0.001f)
                sfxSource.PlayOneShot(clip, i % 3 == 1 ? 0.88f : 1f);

            if (i < intervals.Length)
                yield return new WaitForSecondsRealtime(intervals[i]);
        }
        arrowRainCoroutine = null;
    }

    public void StopArrowRain()
    {
        if (arrowRainCoroutine == null) return;
        StopCoroutine(arrowRainCoroutine);
        arrowRainCoroutine = null;
        // Do not stop the shared SFX source; the last whoosh can decay.
        // Music returns through its existing smoothing in Update().
        duckUntil = Mathf.Min(duckUntil, Time.unscaledTime + 0.45f);
    }

    // Play the result while the grid scene is still visible. Return a
    // duration computed from the real clip length, never a hard-coded
    // two-second jump that cuts off a longer success sting.
    public float PlayGridResultSFX(bool success)
    {
        AudioClip result = success ? achievementSFX : alertSFX;
        if (result == null || sfxSource == null || currentEffectsVolume <= 0f ||
            AudioListener.volume <= 0.001f) return 0.9f;
        PlaySFX(result, 1f);
        return Mathf.Max(0.9f, result.length + 0.3f);
    }

    public void PlayUI(AudioClip clip, float volume = 0.9f)
    {
        if (clip == null) clip = defaultButtonClick;
        PlayControlled(uiSource, clip, volume, false);
    }

    public void PlayValueChange(float change)
    {
        if (change > 0f && valueChangeUpSFX != null)
            PlayControlled(uiSource, valueChangeUpSFX, 0.9f, true);
        else if (change < 0f && valueChangeDownSFX != null)
            PlayControlled(uiSource, valueChangeDownSFX, 0.9f, true);
    }

    public void PlayDigging()
    {
        if (digTunnelDiggingSFX != null) PlaySFX(digTunnelDiggingSFX, 0.95f);
    }

    public void PlayDigSuccess()
    {
        PlaySFX(digTunnelSuccessSFX != null ? digTunnelSuccessSFX : achievementSFX, 1f);
    }

    public void PlayAchievement()
    {
        if (achievementSFX != null) PlaySFX(achievementSFX, 1f);
    }

    public void PlayCraftSuccess()
    {
        if (craftSuccessSFX != null) PlaySFX(craftSuccessSFX, 1f);
    }

    public void PlayPuzzleCorrect() => PlaySFX(puzzleCorrectSFX, 0.95f);
    public void PlayPuzzleMove() => PlayControlled(sfxSource,
        gridStepSFX != null ? gridStepSFX : puzzleMoveSFX, 0.9f, false);
    public void PlayPaperTurn() => PlayControlled(uiSource, paperTurnSFX, 0.9f, false);
    public void PlayAlert() => PlaySFX(alertSFX, 1f);
    public void PlayError() => PlaySFX(errorSFX, 0.95f);

    private void PlayControlled(AudioSource source, AudioClip clip, float volume, bool waitForClip)
    {
        if (source == null || clip == null || currentEffectsVolume <= 0f) return;
        float now = Time.unscaledTime;
        if (nextAllowedPlay.TryGetValue(clip, out float allowed) && now < allowed) return;
        activeEffectEnds.RemoveAll(end => end <= now);
        // Prioritize story cues over incidental UI clicks in busy frames.
        if (source == uiSource && activeEffectEnds.Count >= 8) return;
        nextAllowedPlay[clip] = now + (waitForClip ? Mathf.Max(0.15f, clip.length) : 0.12f);
        activeEffectEnds.Add(now + clip.length);
        if (source == sfxSource)
        {
            duckUntil = Mathf.Max(duckUntil, now + Mathf.Max(0.25f, clip.length));
            effectDuck = Mathf.Min(effectDuck, 0.45f);
            UpdateMusicMix();
        }
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
