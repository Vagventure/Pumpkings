using System.Collections.Generic;
using UnityEngine;

public class MusicController : MonoBehaviour
{
    public static MusicController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LevelController levelController;

    [Header("Startup")]
    [SerializeField] private bool playStartingStateOnStart = true;

    [Header("Debug")]
    [SerializeField] private MusicStateDefinition currentState;
    [SerializeField, Range(0f, 100f)] private float currentPollutionPercent;

    private readonly List<LayerRuntime> activeLayers = new();
    private int baseHandle = AudioManager.InvalidMusicHandle;
    private float stateStartedAt;

    public MusicStateDefinition CurrentState => currentState;

    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }
    }

    private void OnEnable()
    {
        RewardManager.ProgressEventCompleted += HandleProgressEventCompleted;
        ScoringService.OnPollutionChanged += HandlePollutionChanged;
    }

    private void Start()
    {
        if (!playStartingStateOnStart)
        {
            return;
        }

        SyncCurrentPollution();
        LevelController controller = GetLevelController();

        if (controller != null)
        {
            SwitchToState(controller.StartingMusicState);
        }
    }

    private void Update()
    {
        UpdateStateTimeLayers();
    }

    private void OnDisable()
    {
        RewardManager.ProgressEventCompleted -= HandleProgressEventCompleted;
        ScoringService.OnPollutionChanged -= HandlePollutionChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
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

    public void SwitchToState(MusicStateDefinition nextState)
    {
        if (nextState == null)
        {
            return;
        }

        if (nextState == currentState && baseHandle != AudioManager.InvalidMusicHandle)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            Debug.LogWarning("MusicController: Cannot switch music state because AudioManager is missing.");
            return;
        }

        SyncCurrentPollution();
        StopCurrentState(audioManager);

        currentState = nextState;
        stateStartedAt = Time.time;
        baseHandle = PlayTrackMuted(audioManager, nextState.BaseTrack);

        if (baseHandle != AudioManager.InvalidMusicHandle)
        {
            FadeInHandle(audioManager, baseHandle, nextState.BaseTrack);
        }

        StartStateLayers(audioManager, nextState);
    }

    private void StopCurrentState(AudioManager audioManager)
    {
        if (baseHandle != AudioManager.InvalidMusicHandle)
        {
            MusicTrackDefinition baseTrack = currentState == null ? null : currentState.BaseTrack;
            audioManager.StopMusicHandle(
                baseHandle,
                baseTrack == null ? 0f : baseTrack.FadeOutSeconds,
                baseTrack == null ? null : baseTrack.FadeOutCurve);
            baseHandle = AudioManager.InvalidMusicHandle;
        }

        for (int i = 0; i < activeLayers.Count; i++)
        {
            LayerRuntime runtime = activeLayers[i];
            MusicTrackDefinition track = runtime.Layer == null ? null : runtime.Layer.Track;
            audioManager.StopMusicHandle(
                runtime.Handle,
                track == null ? 0f : track.FadeOutSeconds,
                track == null ? null : track.FadeOutCurve);
        }

        activeLayers.Clear();
    }

    private void StartStateLayers(AudioManager audioManager, MusicStateDefinition state)
    {
        IReadOnlyList<MusicLayerDefinition> layers = state.Layers;

        if (layers == null)
        {
            return;
        }

        for (int i = 0; i < layers.Count; i++)
        {
            MusicLayerDefinition layer = layers[i];

            if (layer == null || layer.Track == null || layer.Track.AudioClip == null)
            {
                continue;
            }

            int handle = PlayTrackMuted(audioManager, layer.Track);

            if (handle == AudioManager.InvalidMusicHandle)
            {
                continue;
            }

            LayerRuntime runtime = new LayerRuntime(layer, handle);
            activeLayers.Add(runtime);

            if (layer.TriggerType == MusicLayerTriggerType.Always)
            {
                FadeInLayer(audioManager, runtime);
                continue;
            }

            if (layer.TriggerType == MusicLayerTriggerType.CurrentPollutionPercent)
            {
                EvaluatePollutionLayer(runtime);
            }
        }
    }

    private int PlayTrackMuted(AudioManager audioManager, MusicTrackDefinition track)
    {
        if (track == null || track.AudioClip == null)
        {
            return AudioManager.InvalidMusicHandle;
        }

        return audioManager.PlayMusicTrack(
            track.AudioClip,
            0f,
            true);
    }

    private void FadeInHandle(AudioManager audioManager, int handle, MusicTrackDefinition track)
    {
        audioManager.FadeMusicHandle(
            handle,
            track.TargetVolume,
            track.FadeInSeconds,
            track.FadeInCurve);
    }

    private void FadeInLayer(AudioManager audioManager, LayerRuntime runtime)
    {
        if (runtime.Active)
        {
            return;
        }

        FadeInHandle(audioManager, runtime.Handle, runtime.Layer.Track);
        runtime.Active = true;
    }

    private void FadeOutLayer(AudioManager audioManager, LayerRuntime runtime)
    {
        if (!runtime.Active)
        {
            return;
        }

        MusicTrackDefinition track = runtime.Layer.Track;
        audioManager.FadeMusicHandle(
            runtime.Handle,
            0f,
            track.FadeOutSeconds,
            track.FadeOutCurve);
        runtime.Active = false;
    }

    private void UpdateStateTimeLayers()
    {
        if (activeLayers.Count == 0)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            return;
        }

        float stateTime = Time.time - stateStartedAt;

        for (int i = 0; i < activeLayers.Count; i++)
        {
            LayerRuntime runtime = activeLayers[i];

            if (runtime.Layer.TriggerType != MusicLayerTriggerType.StateTime)
            {
                continue;
            }

            if (stateTime >= runtime.Layer.StartAtStateTimeSeconds)
            {
                FadeInLayer(audioManager, runtime);
            }
        }
    }

    private void HandleProgressEventCompleted(ProgressEventContext context)
    {
        ProgressEventDefinition definition = context.Definition;

        if (definition == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(definition.MusicLayerKeyToActivate))
        {
            ActivateLayer(definition.MusicLayerKeyToActivate);
        }

        if (definition.MusicStateAfterCompletion != null)
        {
            SwitchToState(definition.MusicStateAfterCompletion);
        }
    }

    private void HandlePollutionChanged(int currentPollution, int maxPollution)
    {
        currentPollutionPercent = maxPollution <= 0
            ? 0f
            : Mathf.Clamp01((float)currentPollution / maxPollution) * 100f;

        for (int i = 0; i < activeLayers.Count; i++)
        {
            EvaluatePollutionLayer(activeLayers[i]);
        }
    }

    private void SyncCurrentPollution()
    {
        ScoringService scoringService = ScoringService.Instance;

        if (scoringService != null)
        {
            currentPollutionPercent = scoringService.MaxPollution <= 0
                ? 0f
                : Mathf.Clamp01((float)scoringService.CurrentPollution / scoringService.MaxPollution) * 100f;
        }
    }

    private void EvaluatePollutionLayer(LayerRuntime runtime)
    {
        if (runtime.Layer == null
            || runtime.Layer.TriggerType != MusicLayerTriggerType.CurrentPollutionPercent)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            return;
        }

        if (!runtime.Active && currentPollutionPercent >= runtime.Layer.FadeInAtPercent)
        {
            FadeInLayer(audioManager, runtime);
            return;
        }

        if (runtime.Active && currentPollutionPercent <= runtime.Layer.FadeOutAtPercent)
        {
            FadeOutLayer(audioManager, runtime);
        }
    }

    private LevelController GetLevelController()
    {
        if (levelController == null)
        {
            levelController = LevelController.Instance;
        }

        return levelController;
    }

    private sealed class LayerRuntime
    {
        public LayerRuntime(MusicLayerDefinition layer, int handle)
        {
            Layer = layer;
            Handle = handle;
        }

        public MusicLayerDefinition Layer { get; }
        public int Handle { get; }
        public bool Active { get; set; }
    }

    public void ActivateLayer(string layerKey)
    {
        if (string.IsNullOrEmpty(layerKey))
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            Debug.LogWarning("MusicController: Cannot activate layer because AudioManager is missing.");
            return;
        }

        for (int i = 0; i < activeLayers.Count; i++)
        {
            LayerRuntime runtime = activeLayers[i];

            if (runtime.Layer != null
                && runtime.Layer.TriggerType == MusicLayerTriggerType.Manual
                && runtime.Layer.LayerKey == layerKey)
            {
                FadeInLayer(audioManager, runtime);
            }
        }
    }
}
