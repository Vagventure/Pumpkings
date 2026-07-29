# Audio And Music Code Map

Context: [Audio and music](../contexts/audio-music.md)

## Working Set

- `Assets/Scripts/AudioManager.cs` — source pools, playback policy, loops, duplicate suppression, and managed music handles.
- `Assets/Scripts/AudioSFXController.cs` — semantic gameplay/UI event-to-clip adapter.
- `Assets/Scripts/AudioTrigger.cs` and `AudioTriggerEvents.cs` — environmental audio vocabulary and event seam.
- `Assets/Scripts/EnvironmentalAnimationEventRelay.cs` — animation-event publisher.
- `Assets/Scripts/MusicController.cs` — music state orchestration and layer activation.
- `Assets/Scripts/MusicStateDefinition.cs` — base/layer track configuration.
- `Assets/Scripts/LevelController.cs` and `ProgressEventDefinition.cs` — starting/next music state references.

## Tests

There is no focused audio/music test suite. Pure validation and routing rules fit EditMode; fades, loops, timing, and source lifecycle may require `[UnityTest]` or PlayMode.
