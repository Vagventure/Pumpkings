# Later Checks That Need Prefab Or Scene Inspection

- `PROD_SCENE` contains `RIVER/TrashPath_River` with five authored waypoints and a river-only timed `SpawnService` entry using `RiverBottles.asset`; adjust waypoint transforms and `RiverPathDefinition` speed when tuning the level.
- `GameplayVfxCanvas` owns `CursorController`, `MoneyFlyVfxController`, the custom cursor image, and the money-flight layer. Its money target is the existing `MONEY` TMP object.
- All three trash prefabs contain `TrashPathFollower` and an inactive `PickupProgress` instance from `Assets/Prefabs/UI/TrashPickupProgress.prefab`.
- Add one `RecyclingPatrolService` and assign `SpawnService`, a 2D Patrol prefab, an oriented BoxCollider `Patrol Area`, and off-screen NavMesh entry/exit transforms. The Patrol prefab needs `RecyclingPatrolAgent`, `NavMeshAgent`, and a placeholder `SpriteRenderer`; disable obstacle avoidance.
- Extend the shared shop-item view prefab with a full-card dark cooldown `Image` and a left-side TMP duration label, then assign both on `RewardItemView`.

- Add `StageManager` under `_GameControllers` and configure ordered entries for `3D_Scene/BEACH`, `3D_Scene/RIVER`, and later stage roots.
- Keep stage camera objects under `_GameControllers/Cameras`, outside the `BEACH` and `RIVER` roots. `Beach` and `River` currently provide the corresponding `CinemachineCamera`; `__Brain` provides the shared `CinemachineBrain`.
- For each `StageManager` entry, assign the stage root and its matching camera under `_GameControllers/Cameras`; set `Starting Stage Index` to the desired first stage.
- Tune `Zoom Out Duration`, `Zoom In Duration`, `Zoom Field Of View Offset`, `Zoom Orthographic Size Multiplier`, and `Zoom Curve` on `StageManager` if the default transition needs adjustment.
- Keep `StageManager` out of `GameManager.pauseOnGamePause`; it must run the unscaled-time zoom coroutine while gameplay behaviours are paused.

- In wind-enabled scenes, wire a separate `WindEventController`, Animator, and `EnvironmentalAnimationEventRelay`; keep `WindEventController` out of `GameManager.pauseOnGamePause` because it freezes itself. Wind-spawned trash remains ordinary pooled scene trash owned by `SpawnService`; no visual-root proxy participates in spawning or movement.
- Configure Bags in `SpawnService` as `EventSpawn` / `WindSpawnTrigger` / `DirectionalBurst`, assign the active `SpawnArea_Bags` BoxCollider, and set `Is Movable` on the plastic-bag prefab. This wiring is present in `PROD_SCENE` and `1_PlasticBag`.
- Use `Assets/Animations/Environment/Wind/Wind_Event.controller` with its `PlayWind` trigger and non-looping `Wind_Gust.anim`; the clip relays `WindAudioEvent`, `WindSpawnAndMovementEvent(float)`, and `WindEndEvent`.
- Verify current awareness tier data before replacing it with progress events.
- Add a `ProgressTracker` object to the scene.
- Configure `LevelController` awareness, gold gathered, and threat produced event lists.
- Verify there is one scene-level dialogue panel with a concrete `EventPresentationResolver`.
- Verify `RewardManager.Event Resolver` references the scene-level presentation resolver explicitly. `RewardManager` must not search the scene for another resolver.
- Verify `EventPresentationResolver.lineContainer` points at the dialogue line container.
- Verify `EventPresentationResolver` line prefabs point at prefabs with `DialogueLineView`.
- Verify line prefabs do not contain an event presentation resolver and do not contain continue buttons.
- Verify the continue button is on the dialogue panel, not on NPC/player line prefabs.
- Verify dialogue choice slot prefabs/objects contain `DialogueChoiceView` and are assigned to resolver choice slots.
- Verify `ProgressEventDefinition.dialogueChoices` contains button labels, player lines, rewards, and optional player voice clips.
- Verify legacy `RewardSelectionUI` does not open for dialogue events that returned a selected reward through `EventPresentationResolver`.
- Verify awareness bar target after tier removal.
