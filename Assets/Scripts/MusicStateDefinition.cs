using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MusicStateDefinition", menuName = "Pumpkins/Music State Definition")]
public class MusicStateDefinition : ScriptableObject
{
    [Header("Base Track")]
    [SerializeField] private MusicTrackDefinition baseTrack = new();

    [Header("Layers")]
    [SerializeField] private List<MusicLayerDefinition> layers = new();

    public MusicTrackDefinition BaseTrack => baseTrack;
    public IReadOnlyList<MusicLayerDefinition> Layers => layers;

    private void OnValidate()
    {
        baseTrack ??= new MusicTrackDefinition();

        if (layers == null)
        {
            layers = new List<MusicLayerDefinition>();
        }

        baseTrack.Validate();

        foreach (MusicLayerDefinition layer in layers)
        {
            layer?.Validate();
        }
    }
}

[Serializable]
public class MusicTrackDefinition
{
    [SerializeField] private AudioClip audioClip;
    [SerializeField, Range(0f, 1f)] private float targetVolume = 1f;
    [SerializeField] private float fadeInSeconds = 1f;
    [SerializeField] private float fadeOutSeconds = 1f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    public AudioClip AudioClip => audioClip;
    public float TargetVolume => targetVolume;
    public float FadeInSeconds => fadeInSeconds;
    public float FadeOutSeconds => fadeOutSeconds;
    public AnimationCurve FadeInCurve => fadeInCurve;
    public AnimationCurve FadeOutCurve => fadeOutCurve;

    public void Validate()
    {
        targetVolume = Mathf.Clamp01(targetVolume);
        fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
        fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
        fadeInCurve ??= AnimationCurve.Linear(0f, 0f, 1f, 1f);
        fadeOutCurve ??= AnimationCurve.Linear(0f, 1f, 1f, 0f);
    }
}

[Serializable]
public class MusicLayerDefinition
{
    [SerializeField] private string layerKey;   // NEW
    [SerializeField] private MusicTrackDefinition track = new();
    [SerializeField] private MusicLayerTriggerType triggerType = MusicLayerTriggerType.Always;
    [SerializeField] private float startAtStateTimeSeconds;
    [SerializeField, Range(0f, 100f)] private float fadeInAtPercent = 50f;
    [SerializeField, Range(0f, 100f)] private float fadeOutAtPercent = 40f;

    public MusicTrackDefinition Track => track;
    public string LayerKey => layerKey;   // NEW
    public MusicLayerTriggerType TriggerType => triggerType;
    public float StartAtStateTimeSeconds => startAtStateTimeSeconds;
    public float FadeInAtPercent => fadeInAtPercent;
    public float FadeOutAtPercent => fadeOutAtPercent;

    public void Validate()
    {
        track ??= new MusicTrackDefinition();
        track.Validate();
        startAtStateTimeSeconds = Mathf.Max(0f, startAtStateTimeSeconds);
        fadeInAtPercent = Mathf.Clamp(fadeInAtPercent, 0f, 100f);
        fadeOutAtPercent = Mathf.Clamp(fadeOutAtPercent, 0f, 100f);

        if (fadeOutAtPercent > fadeInAtPercent)
        {
            fadeOutAtPercent = fadeInAtPercent;
        }
    }
}

public enum MusicLayerTriggerType
{
    Always,
    StateTime,
    CurrentPollutionPercent,
    Manual
}
