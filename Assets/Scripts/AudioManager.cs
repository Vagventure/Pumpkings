using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const int MaxSfxSources = 10;
    private const int MaxPlayerSources = 3;
    private const int MaxEnvironmentSources = 6;
    private const int MaxMusicSources = 16;
    public const int InvalidMusicHandle = -1;

    public static AudioManager Instance { get; private set; }

[Header("Random Pitch")]
[SerializeField] private bool randomizeTrashPitch = true;
[SerializeField] private Vector2 trashSpawnPitchRange = new Vector2(0.9f, 1.1f);
[SerializeField] private Vector2 trashDespawnPitchRange = new Vector2(0.9f, 1.15f);
[SerializeField] private Vector2 defaultSfxPitchRange = new Vector2(0.95f, 1.05f);
[SerializeField, Min(0f)] private float duplicateSfxSuppressionSeconds = 0.05f;

    [Header("SFX Pools")]
    [SerializeField] private AudioSource[] playerSources;
    [SerializeField] private AudioSource[] sfxSources;

    [Header("Environment")]
    [SerializeField] private AudioSource[] environmentSources;

    [Header("Long Running Sources")]
    [SerializeField] private AudioSource[] musicSources;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float playerVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float environmentVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Trash Event Volume")]
    [SerializeField, Range(0f, 1f)] private float trashSpawnVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float trashDespawnVolume = 0.8f;

    [Header("Source Settings")]
    [SerializeField] private bool applySourceSettingsOnAwake = true;
    [SerializeField] private bool force2DSound = true;
    [SerializeField] private bool logSkippedSounds;

    private int nextPlayerSourceIndex;
    private int nextSfxSourceIndex;
    private int nextEnvironmentSourceIndex;
    private int nextMusicHandle;
    private AudioSource playerLoopSource;
    private readonly Dictionary<AudioClip, float> lastSfxPlayTimes = new Dictionary<AudioClip, float>();
    private int[] musicSourceHandles;
    private Coroutine[] musicFadeCoroutines;

    private enum AudioChannel
    {
        Player,
        Sfx,
        Environment
    }


     private struct AudioRequest
{
    public AudioChannel Channel;
    public AudioClip Clip;
    public Vector3 WorldPosition;
    public float Volume;
    public float Pitch;
    public bool RandomizePitch;
    public Vector2 PitchRange;
    public bool InterruptIfBusy;

    public AudioRequest(
        AudioChannel channel,
        AudioClip clip,
        Vector3 worldPosition,
        float volume,
        float pitch,
        bool randomizePitch,
        Vector2 pitchRange,
        bool interruptIfBusy)
    {
        Channel = channel;
        Clip = clip;
        WorldPosition = worldPosition;
        Volume = volume;
        Pitch = pitch;
        RandomizePitch = randomizePitch;
        PitchRange = pitchRange;
        InterruptIfBusy = interruptIfBusy;
    }

    }
    

    
    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }

        if (applySourceSettingsOnAwake)
        {
            ApplySourceSettings();
        }
    }

    private void OnEnable()
    {
        SpawnService.TrashAdded += HandleTrashSpawned;
        SpawnService.TrashRemoved += HandleTrashDespawned;
    }

    private void OnDisable()
    {
        SpawnService.TrashAdded -= HandleTrashSpawned;
        SpawnService.TrashRemoved -= HandleTrashDespawned;
    }

    private bool SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }

private void HandleTrashSpawned(Trash trash)
{
    if (trash == null)
    {
        return;
    }

    RequestAudio(new AudioRequest(
        AudioChannel.Sfx,
        trash.SpawnSound,
        trash.transform.position,
        trashSpawnVolume,
        1f,
        randomizeTrashPitch,
        trashSpawnPitchRange,
        false
    ));
}

private void HandleTrashDespawned(Trash trash)
{
    if (trash == null)
    {
        return;
    }

    RequestAudio(new AudioRequest(
        AudioChannel.Sfx,
        trash.DespawnSound,
        trash.transform.position,
        trashDespawnVolume,
        1f,
        randomizeTrashPitch,
        trashDespawnPitchRange,
        false
    ));
}

public void PlayPlayerSound(AudioClip clip, bool randomizePitch = false)
{
    RequestAudio(new AudioRequest(
        AudioChannel.Player,
        clip,
        Vector3.zero,
        1f,
        1f,
        randomizePitch,
        defaultSfxPitchRange,
        true
    ));
}

public void PlayPlayerLoopSound(AudioClip clip)
{
    if (clip == null)
    {
        return;
    }

    if (playerLoopSource != null && playerLoopSource.isPlaying && playerLoopSource.clip == clip)
    {
        return;
    }

    if (playerLoopSource != null)
    {
        StopPlayerLoopSound(null);
    }

    AudioSource source = GetPlayerSource(false);

    if (source == null)
    {
        if (logSkippedSounds)
        {
            Debug.Log($"AudioManager: skipped player loop {clip.name}. No available player source.");
        }

        return;
    }

    playerLoopSource = source;
    source.Stop();
    source.clip = clip;
    source.loop = true;
    source.pitch = 1f;
    source.volume = Mathf.Clamp01(masterVolume * playerVolume);
    source.Play();
}

public void StopPlayerLoopSound(AudioClip clip)
{
    if (playerLoopSource == null)
    {
        return;
    }

    if (clip != null && playerLoopSource.clip != clip)
    {
        return;
    }

    playerLoopSource.Stop();
    playerLoopSource.clip = null;
    playerLoopSource.loop = false;
    playerLoopSource = null;
}

 public void PlaySfx(AudioClip clip, bool randomizePitch = false)
{
    RequestAudio(new AudioRequest(
        AudioChannel.Sfx,
        clip,
        Vector3.zero,
        1f,
        1f,
        randomizePitch,
        defaultSfxPitchRange,
        false
    ));
}

public void PlayEnvironmentSfx(AudioClip clip, bool randomizePitch = false)
{
    RequestAudio(new AudioRequest(
        AudioChannel.Environment,
        clip,
        Vector3.zero,
        1f,
        1f,
        randomizePitch,
        defaultSfxPitchRange,
        false
    ));
}

public void PlayEnvironmentLoopSfx(AudioClip clip)
{
    if (clip == null)
    {
        return;
    }

    AudioSource source = GetPlayingEnvironmentLoopSource(clip);

    if (source != null)
    {
        return;
    }

    source = GetEnvironmentSource(false);

    if (source == null)
    {
        if (logSkippedSounds)
        {
            Debug.Log($"AudioManager: skipped environment loop {clip.name}. No available environment source.");
        }

        return;
    }

    source.Stop();
    source.clip = clip;
    source.loop = true;
    source.pitch = 1f;
    source.volume = Mathf.Clamp01(masterVolume * environmentVolume);
    source.Play();
}

    private void RequestAudio(AudioRequest request)
    {
        HandleAudioRequested(request);
    }

    private void HandleAudioRequested(AudioRequest request)
    {
        if (request.Clip == null)
        {
            return;
        }

        if (ShouldSuppressDuplicateSfx(request))
        {
            return;
        }

        AudioSource source = GetSourceForRequest(request);

        if (source == null)
        {
            if (logSkippedSounds)
            {
                Debug.Log($"AudioManager: skipped sound {request.Clip.name}. No available source.");
            }

            return;
        }

        source.transform.position = request.WorldPosition;
        source.pitch = GetFinalPitch(request);

        float volume = GetFinalVolume(request);

        if (request.InterruptIfBusy)
        {
            source.Stop();
        }

        source.loop = false;
        source.PlayOneShot(request.Clip, volume);
    }

private bool ShouldSuppressDuplicateSfx(AudioRequest request)
{
    if (request.Channel != AudioChannel.Sfx || duplicateSfxSuppressionSeconds <= 0f)
    {
        return false;
    }

    float now = Time.unscaledTime;

    if (lastSfxPlayTimes.TryGetValue(request.Clip, out float lastPlayTime)
        && now - lastPlayTime < duplicateSfxSuppressionSeconds)
    {
        return true;
    }

    lastSfxPlayTimes[request.Clip] = now;
    return false;
}


private float GetFinalPitch(AudioRequest request)
{
    if (!request.RandomizePitch)
    {
        return Mathf.Max(0.01f, request.Pitch);
    }

    float minPitch = Mathf.Min(request.PitchRange.x, request.PitchRange.y);
    float maxPitch = Mathf.Max(request.PitchRange.x, request.PitchRange.y);

    float randomizedPitch = UnityEngine.Random.Range(minPitch, maxPitch);

    return Mathf.Max(0.01f, randomizedPitch);
}

    private AudioSource GetSourceForRequest(AudioRequest request)
    {
        switch (request.Channel)
        {
            case AudioChannel.Player:
                return GetPlayerSource(request.InterruptIfBusy);

            case AudioChannel.Sfx:
                return GetSfxSource(request.InterruptIfBusy);

            case AudioChannel.Environment:
                return GetEnvironmentSource(request.InterruptIfBusy);

            default:
                return null;
        }
    }

    private AudioSource GetPlayerSource(bool interruptIfBusy)
    {
        AudioSource freeSource = GetFreeSource(playerSources, MaxPlayerSources);

        if (freeSource != null)
        {
            return freeSource;
        }

        if (!interruptIfBusy)
        {
            return null;
        }

        return GetRoundRobinSource(
            playerSources,
            MaxPlayerSources,
            ref nextPlayerSourceIndex
        );
    }

    private AudioSource GetSfxSource(bool interruptIfBusy)
    {
        AudioSource freeSource = GetFreeSource(sfxSources, MaxSfxSources);

        if (freeSource != null)
        {
            return freeSource;
        }

        if (!interruptIfBusy)
        {
            return null;
        }

        return GetRoundRobinSource(
            sfxSources,
            MaxSfxSources,
            ref nextSfxSourceIndex
        );
    }

    private AudioSource GetEnvironmentSource(bool interruptIfBusy)
    {
        AudioSource freeSource = GetFreeSource(environmentSources, MaxEnvironmentSources);

        if (freeSource != null)
        {
            return freeSource;
        }

        if (!interruptIfBusy)
        {
            return null;
        }

        return GetRoundRobinSource(
            environmentSources,
            MaxEnvironmentSources,
            ref nextEnvironmentSourceIndex
        );
    }

    private AudioSource GetPlayingEnvironmentLoopSource(AudioClip clip)
    {
        if (environmentSources == null)
        {
            return null;
        }

        int count = Mathf.Min(environmentSources.Length, MaxEnvironmentSources);

        for (int i = 0; i < count; i++)
        {
            AudioSource source = environmentSources[i];

            if (source != null && source.isPlaying && source.loop && source.clip == clip)
            {
                return source;
            }
        }

        return null;
    }

    private AudioSource GetFreeSource(AudioSource[] sources, int maxSources)
    {
        if (sources == null)
        {
            return null;
        }

        int count = Mathf.Min(sources.Length, maxSources);

        for (int i = 0; i < count; i++)
        {
            AudioSource source = sources[i];

            if (source == null)
            {
                continue;
            }

            if (!source.isPlaying)
            {
                return source;
            }
        }

        return null;
    }

    private AudioSource GetRoundRobinSource(
        AudioSource[] sources,
        int maxSources,
        ref int nextIndex)
    {
        if (sources == null)
        {
            return null;
        }

        int count = Mathf.Min(sources.Length, maxSources);

        if (count <= 0)
        {
            return null;
        }

        for (int i = 0; i < count; i++)
        {
            nextIndex %= count;

            AudioSource source = sources[nextIndex];

            nextIndex++;

            if (source != null)
            {
                return source;
            }
        }

        return null;
    }

    private float GetFinalVolume(AudioRequest request)
    {
        float channelVolume = 1f;

        switch (request.Channel)
        {
            case AudioChannel.Player:
                channelVolume = playerVolume;
                break;

            case AudioChannel.Sfx:
                channelVolume = sfxVolume;
                break;

            case AudioChannel.Environment:
                channelVolume = environmentVolume;
                break;
        }

        return Mathf.Clamp01(masterVolume * channelVolume * request.Volume);
    }

    public void PlayMusic(AudioClip clip, int sourceIndex = 0, bool loop = true)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetMusicSource(sourceIndex);

        if (source == null)
        {
            if (logSkippedSounds)
            {
                Debug.Log("AudioManager: music request ignored. No music source assigned.");
            }

            return;
        }

        source.Stop();
        ClearManagedMusicSource(sourceIndex);
        source.clip = clip;
        source.loop = loop;
        source.volume = Mathf.Clamp01(masterVolume * musicVolume);
        source.pitch = 1f;
        source.Play();
    }

    public void StopMusic(int sourceIndex = 0)
    {
        AudioSource source = GetMusicSource(sourceIndex);

        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
        ClearManagedMusicSource(sourceIndex);
    }

    public int PlayMusicTrack(AudioClip clip, float initialVolume = 0f, bool loop = true)
    {
        if (clip == null)
        {
            return InvalidMusicHandle;
        }

        EnsureMusicRuntimeState();
        int sourceIndex = GetAvailableMusicSourceIndex();

        if (sourceIndex < 0)
        {
            if (logSkippedSounds)
            {
                Debug.Log($"AudioManager: music request ignored. No free music source for {clip.name}.");
            }

            return InvalidMusicHandle;
        }

        AudioSource source = musicSources[sourceIndex];
        StopMusicFade(sourceIndex);

        int handle = ++nextMusicHandle;

        if (handle == InvalidMusicHandle)
        {
            handle = ++nextMusicHandle;
        }

        musicSourceHandles[sourceIndex] = handle;

        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.pitch = 1f;
        source.volume = GetFinalMusicVolume(initialVolume);
        source.Play();

        return handle;
    }

    public void FadeMusicHandle(
        int handle,
        float targetVolume,
        float duration,
        AnimationCurve curve)
    {
        int sourceIndex = GetMusicSourceIndexForHandle(handle);

        if (sourceIndex < 0)
        {
            return;
        }

        StopMusicFade(sourceIndex);
        musicFadeCoroutines[sourceIndex] = StartCoroutine(FadeMusicSource(
            sourceIndex,
            handle,
            Mathf.Clamp01(targetVolume),
            Mathf.Max(0f, duration),
            curve,
            false));
    }

    public void StopMusicHandle(
        int handle,
        float fadeOutSeconds,
        AnimationCurve fadeOutCurve)
    {
        int sourceIndex = GetMusicSourceIndexForHandle(handle);

        if (sourceIndex < 0)
        {
            return;
        }

        StopMusicFade(sourceIndex);
        musicFadeCoroutines[sourceIndex] = StartCoroutine(FadeMusicSource(
            sourceIndex,
            handle,
            0f,
            Mathf.Max(0f, fadeOutSeconds),
            fadeOutCurve,
            true));
    }

    public void StopAllManagedMusic(float fadeOutSeconds, AnimationCurve fadeOutCurve)
    {
        EnsureMusicRuntimeState();
        int count = GetMusicSourceCount();

        for (int i = 0; i < count; i++)
        {
            int handle = musicSourceHandles[i];

            if (handle != InvalidMusicHandle)
            {
                StopMusicHandle(handle, fadeOutSeconds, fadeOutCurve);
            }
        }
    }

    private AudioSource GetMusicSource(int sourceIndex)
    {
        if (musicSources == null || musicSources.Length == 0)
        {
            return null;
        }

        int count = Mathf.Min(musicSources.Length, MaxMusicSources);

        if (count <= 0)
        {
            return null;
        }

        sourceIndex = Mathf.Clamp(sourceIndex, 0, count - 1);

        return musicSources[sourceIndex];
    }

    private IEnumerator FadeMusicSource(
        int sourceIndex,
        int handle,
        float targetVolume,
        float duration,
        AnimationCurve curve,
        bool stopAtEnd)
    {
        AudioSource source = GetMusicSourceByIndex(sourceIndex);

        if (source == null || musicSourceHandles[sourceIndex] != handle)
        {
            yield break;
        }

        float startVolume = source.volume;
        float finalVolume = GetFinalMusicVolume(targetVolume);

        if (duration <= 0f)
        {
            source.volume = finalVolume;
            CompleteMusicFade(sourceIndex, handle, stopAtEnd);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (musicSourceHandles[sourceIndex] != handle)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float curveValue = curve == null
                ? GetDefaultMusicFadeCurveValue(progress, finalVolume < startVolume)
                : Mathf.Clamp01(curve.Evaluate(progress));

            if (finalVolume < startVolume)
            {
                source.volume = Mathf.Lerp(finalVolume, startVolume, curveValue);
            }
            else
            {
                source.volume = Mathf.Lerp(startVolume, finalVolume, curveValue);
            }

            yield return null;
        }

        if (musicSourceHandles[sourceIndex] != handle)
        {
            yield break;
        }

        source.volume = finalVolume;
        CompleteMusicFade(sourceIndex, handle, stopAtEnd);
    }

    private void CompleteMusicFade(int sourceIndex, int handle, bool stopAtEnd)
    {
        AudioSource source = GetMusicSourceByIndex(sourceIndex);

        if (source == null || musicSourceHandles[sourceIndex] != handle)
        {
            return;
        }

        musicFadeCoroutines[sourceIndex] = null;

        if (!stopAtEnd)
        {
            return;
        }

        source.Stop();
        source.clip = null;
        ClearManagedMusicSource(sourceIndex);
        ClearManagedMusicSource(sourceIndex);
        ClearManagedMusicSource(sourceIndex);
        musicSourceHandles[sourceIndex] = InvalidMusicHandle;
    }

    private int GetAvailableMusicSourceIndex()
    {
        int count = GetMusicSourceCount();

        for (int i = 0; i < count; i++)
        {
            AudioSource source = musicSources[i];

            if (source == null)
            {
                continue;
            }

            if (!source.isPlaying && musicSourceHandles[i] == InvalidMusicHandle)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetMusicSourceIndexForHandle(int handle)
    {
        if (handle == InvalidMusicHandle)
        {
            return -1;
        }

        EnsureMusicRuntimeState();
        int count = GetMusicSourceCount();

        for (int i = 0; i < count; i++)
        {
            if (musicSourceHandles[i] == handle)
            {
                return i;
            }
        }

        return -1;
    }

    private AudioSource GetMusicSourceByIndex(int sourceIndex)
    {
        int count = GetMusicSourceCount();

        if (sourceIndex < 0 || sourceIndex >= count)
        {
            return null;
        }

        return musicSources[sourceIndex];
    }

    private int GetMusicSourceCount()
    {
        if (musicSources == null)
        {
            return 0;
        }

        return Mathf.Min(musicSources.Length, MaxMusicSources);
    }

    private void StopMusicFade(int sourceIndex)
    {
        if (musicFadeCoroutines == null
            || sourceIndex < 0
            || sourceIndex >= musicFadeCoroutines.Length
            || musicFadeCoroutines[sourceIndex] == null)
        {
            return;
        }

        StopCoroutine(musicFadeCoroutines[sourceIndex]);
        musicFadeCoroutines[sourceIndex] = null;
    }

    private void ClearManagedMusicSource(int sourceIndex)
    {
        EnsureMusicRuntimeState();

        if (sourceIndex < 0 || sourceIndex >= musicSourceHandles.Length)
        {
            return;
        }

        StopMusicFade(sourceIndex);
        musicSourceHandles[sourceIndex] = InvalidMusicHandle;
    }

    private float GetFinalMusicVolume(float volume)
    {
        return Mathf.Clamp01(masterVolume * musicVolume * Mathf.Clamp01(volume));
    }

    private static float GetDefaultMusicFadeCurveValue(float progress, bool fadingOut)
    {
        return fadingOut ? 1f - progress : progress;
    }

    private void EnsureMusicRuntimeState()
    {
        int count = GetMusicSourceCount();

        if (count <= 0)
        {
            musicSourceHandles = Array.Empty<int>();
            musicFadeCoroutines = Array.Empty<Coroutine>();
            return;
        }

        if (musicSourceHandles == null || musicSourceHandles.Length != count)
        {
            musicSourceHandles = new int[count];

            for (int i = 0; i < count; i++)
            {
                musicSourceHandles[i] = InvalidMusicHandle;
            }
        }

        if (musicFadeCoroutines == null || musicFadeCoroutines.Length != count)
        {
            musicFadeCoroutines = new Coroutine[count];
        }
    }

    private void ApplySourceSettings()
    {
        ApplySettingsToPool(playerSources, MaxPlayerSources, 32);
        ApplySettingsToPool(sfxSources, MaxSfxSources, 96);
        ApplySettingsToPool(environmentSources, MaxEnvironmentSources, 96);
        ApplySettingsToPool(musicSources, MaxMusicSources, 160);
    }

    private void ApplySettingsToPool(
        AudioSource[] sources,
        int maxSources,
        int priority)
    {
        if (sources == null)
        {
            return;
        }

        int count = Mathf.Min(sources.Length, maxSources);

        for (int i = 0; i < count; i++)
        {
            AudioSource source = sources[i];

            if (source == null)
            {
                continue;
            }

            source.playOnAwake = false;
            source.priority = priority;

            if (force2DSound)
            {
                source.spatialBlend = 0f;
            }
        }
    }

    private void OnValidate()
    {
        WarnIfTooManySources(playerSources, MaxPlayerSources, "Player Sources");
        WarnIfTooManySources(sfxSources, MaxSfxSources, "SFX Sources");
        WarnIfTooManySources(environmentSources, MaxEnvironmentSources, "Environment Sources");
        WarnIfTooManySources(musicSources, MaxMusicSources, "Music Sources");
    }

    private void WarnIfTooManySources(
        AudioSource[] sources,
        int maxSources,
        string label)
    {
        if (sources == null)
        {
            return;
        }

        if (sources.Length <= maxSources)
        {
            return;
        }

        Debug.LogWarning(
            $"AudioManager: {label} has {sources.Length} sources assigned, but only first {maxSources} will be used."
        );
    }
}
