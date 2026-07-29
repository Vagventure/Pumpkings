---
read_when: [audio, sfx, music, audio-routing]
avoid_when: [visual-only-feedback, gameplay-without-audio]
primary_files: [Assets/Scripts/AudioManager.cs, Assets/Scripts/AudioSFXController.cs, Assets/Scripts/AudioTrigger.cs, Assets/Scripts/MusicController.cs, Assets/Scripts/MusicStateDefinition.cs]
tests: []
code_maps: [docs/code-map/audio-music.md]
---

# Audio And Music Context

## Ownership

- `AudioManager` is the low-level playback module. It owns AudioSource pools, volume/pitch policy, duplicate suppression, player/environment loops, and managed music handles/fades. It also reacts directly to trash lifecycle sounds stored on `Trash`.
- `AudioSFXController` is the semantic event-to-clip adapter. Gameplay and UI publish intent events; this controller selects clips and delegates playback to `AudioManager`.
- `MusicController` owns music state orchestration.
- `MusicStateDefinition` configures a base track and optional layers.
- `AudioTriggerEvents` carries environmental trigger vocabulary raised by animation events.

See [Feel / SFX Context](feel-sfx.md) for semantic UI and reward SFX policy.

## SFX Flow And Policy

Gameplay/UI event -> `AudioSFXController` mapping -> `AudioManager` player, SFX, or environment channel.

Event-duration and typing sounds use dedicated looping sources owned by `AudioSFXController`. On disable, the controller stops those loops and the tracked player-walk loop.

`AudioManager` prefers a free source. Player one-shots may interrupt through round-robin selection; ordinary SFX/environment requests are skipped when their pools are busy. Environment loops ignore a duplicate clip that is already looping. Null clips are safe no-ops.

Short duplicate suppression applies to the ordinary SFX channel by clip and unscaled time. Pool assignment has channel caps; extra Inspector sources are ignored and warned about during validation.

## Music Flow

`MusicController` can start from `LevelController.StartingMusicState`. A completed progress event can switch state through `ProgressEventDefinition.MusicStateAfterCompletion`.

Switching state fades/stops previous base and layers, starts valid tracks through managed handles, then applies layer rules:

- `Always`: fade in immediately;
- `StateTime`: activate after configured state time;
- `CurrentPollutionPercent`: fade in/out using configured thresholds and hysteresis.

Music fades use unscaled delta time; state-time layer activation uses scaled game time. Pollution layers react to `ScoringService.OnPollutionChanged`.

## Invariants And Risks

- Track volume, fade duration, thresholds, and curves are normalized by `MusicStateDefinition` validation.
- `FadeOutAtPercent` must not exceed `FadeInAtPercent`.
- A managed handle is valid only while its source remains allocated to that handle.
- Singleton event subscriptions must be balanced.
- Current code has no audio/music tests. `AudioManager` also does not currently clear its singleton in `OnDestroy`, unlike the other audio controllers; verify this behavior before changing lifecycle assumptions.

## Unity Setup

- Scene requires configured `AudioManager`, `AudioSFXController`, and `MusicController` when those systems are used.
- Assign channel source arrays and volumes on `AudioManager`.
- Assign semantic clips and optional dedicated duration/typing sources on `AudioSFXController`; missing loop sources can be created at runtime.
- Assign `LevelController` and starting music state for automatic music startup.
- Music state assets need a valid base clip and only valid optional layer clips.
- No custom editor currently exists for these audio/music classes.

## Verification

Missing coverage includes pool saturation, interruption, duplicate suppression, loop cleanup, volume/pitch setup, singleton lifecycle, managed-handle reuse/fades, state switching, layer timing/hysteresis, definition validation, and publisher subscription symmetry. Time-dependent fades and layers may need `[UnityTest]` or PlayMode tests unless their decision logic is extracted.

## Important Files

- `Assets/Scripts/AudioManager.cs`
- `Assets/Scripts/AudioSFXController.cs`
- `Assets/Scripts/AudioTrigger.cs`
- `Assets/Scripts/AudioTriggerEvents.cs`
- `Assets/Scripts/MusicController.cs`
- `Assets/Scripts/MusicStateDefinition.cs`
- `Assets/Scripts/LevelController.cs`
- `Assets/Scripts/ProgressEventDefinition.cs`
