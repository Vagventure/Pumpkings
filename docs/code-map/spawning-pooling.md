# Spawning And Pooling Code Map

Context: [Spawning and pooling](../contexts/spawning-pooling.md)

## Runtime And Data

- `Assets/Scripts/TrashPathDefinition.cs`, `TrashPath.cs`, and `TrashPathFollower.cs` — reusable river speed, scene-authored waypoints, and per-instance traversal.

- `Assets/Scripts/WindEventController.cs`, `WindDirection.cs`, and `WindGustMath.cs` — gust scheduling, per-gust spawn-count renewal, direction selection, movable-trash motion, and pure movement rules.
- `Assets/Scripts/EventSpawnPattern.cs` and `DirectionalBurstMath.cs` — instant/directional event policy and burst schedule calculations.
- `Assets/Scripts/SpawnService.cs` — pool creation, timed/event spawning, caps, blocking, active tracking, despawn, `TrashAdded`, and `TrashRemoved`.
- `Assets/Scripts/SpawnData.cs` — prefab, interval, per-type limit, and sprite variants.
- `Assets/Scripts/SpawnMode.cs` — timed versus event spawn mode.
- `Assets/Scripts/SpawnAreaSampler.cs` — placement sampling.
- `Assets/Scripts/SpawnTrigger.cs` and `SpawnTriggerEvents.cs` — trigger vocabulary and event seam.
- `Assets/Scripts/EnvironmentalAnimationEventRelay.cs` — animation-event adapter for environmental triggers.
- `Assets/Scripts/Trash.cs` and `TrashType.cs` — spawned entity data and type identity.
- `Assets/Animations/Environment/Wind/Wind_Event.controller` and `Wind_Gust.anim` — scene wind Animator controller, `PlayWind` trigger, and the three gust animation events.
- `Assets/Scenes/PROD_SCENE.unity` — current wind-enabled scene wiring (`WIND_EVENT`, `SpawnArea_Bags`, Bags spawn configuration, and wind audio trigger); bags are ordinary `SpawnService` pool instances.

## Editor And Tests

- `Assets/Tests/EditMode/TrashPathFollowerTests.cs` — traversal, endpoint stopping, pickup pause, and dynamic pickup tracking.
- `Assets/Scripts/Editor/RiverLevelFeatureSetup.cs` — creates the river-only spawn configuration and wires path/VFX/cursor assets.

- `Assets/Tests/EditMode/WindGustMathTests.cs` — direction, movement, and burst schedule rules.
- `Assets/Tests/EditMode/SpawnServiceWindIntegrationTests.cs` — event burst timing, limits, pause, edge placement, and instant-spawn regression.
- `Assets/Tests/EditMode/WindEventControllerTests.cs` — first and repeat gust scheduling.
- `Assets/Tests/EditMode/TrashWindTests.cs` — movable-trash spawn reset and pickup lock state.
- `Assets/Scripts/Editor/SpawnServiceEditor.cs` — manually drawn custom Inspector; update it with serialized `SpawnService` fields.
- `Assets/Tests/EditMode/SpawnAreaSamplerTests.cs` — rotated BoxCollider sampling.
- `Assets/Tests/EditMode/TutorialSupportTests.cs` — per-type blocking state only.

## Main Seams

- A spawn configuration may select an active `TrashPath`; path-only configurations remain dormant while their stage-owned path is inactive.

- Confirmed pickup enters from `MousePickUpController.OnTrashClicked`.
- Auto-collection enters from `RewardManager.AutoCollectRequested`.
- `TrashAdded` and `TrashRemoved` cross into scoring, audio, tutorial, and other lifecycle consumers.
