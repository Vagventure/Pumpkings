---
read_when: [stage-transition, cinemachine, camera-zoom, level-sequence]
avoid_when: [unity-scene-loading, unrelated-camera-work]
primary_files: [Assets/Scripts/StageManager.cs, Assets/Scripts/ProgressEventDefinition.cs, Assets/Scripts/RewardManager.cs]
tests: [Assets/Tests/EditMode/StageManagerTests.cs]
code_maps: [docs/code-map/stage-transitions.md]
---

# Stage Transitions

## Model

The game keeps its short sequence inside one Unity scene. A `stage` is not a Unity scene asset; it is an authored pair of a stage-specific root `GameObject` and a matching `CinemachineCamera`, which may live separately under `_GameControllers/Cameras`.

`StageManager` owns the ordered stage list, initial root/camera activation, Cinemachine priorities, zoom transition, and replacement of the outgoing stage. Global managers, shared UI, stage camera objects, and the shared `CinemachineBrain` stay outside stage roots.

## Transition Flow

1. `GoToNextStage` pauses gameplay, activates the incoming root for readiness, and publishes `TransitionStarted`.
2. The outgoing camera widens its perspective field of view or orthographic size using unscaled time.
3. At the widest view, `StageManager` lowers and disables the outgoing camera/root, then raises and enables the incoming camera.
4. The incoming camera eases from the widened view back to its authored lens.
5. After both zoom phases, gameplay resumes and `TransitionCompleted` is published. Entering the final configured stage also publishes `SequenceCompleted`; the sequence never wraps.

Repeated advance requests are ignored while the zoom transition is active. Menu startup, automatic post-transition narrative events, prefab instantiation, Cinemachine Brain blending, and Unity scene loading are outside the current contract.

## Progress Event Integration

`ProgressEventCompletionEffect.GoToNextStage` starts a transition only after the full progress event flow, including dialogue and reward presentation, has completed. `RewardManager` defers its queued-event continuation and gameplay resume until `StageManager.TransitionCompleted`.

`ProgressEventCompletionEffect` remains a single mutually exclusive choice. A progress event cannot combine `StartWind` and `GoToNextStage`.
