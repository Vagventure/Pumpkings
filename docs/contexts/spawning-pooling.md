---
read_when: [spawn, pooling, spawn-area, spawn-limit, spawn-trigger, wind, directional-burst]
avoid_when: [pickup-only, economy-only]
primary_files: [Assets/Scripts/SpawnService.cs, Assets/Scripts/WindEventController.cs, Assets/Scripts/SpawnData.cs, Assets/Scripts/SpawnAreaSampler.cs, Assets/Scripts/SpawnTrigger.cs, Assets/Scripts/Editor/SpawnServiceEditor.cs]
tests: [Assets/Tests/EditMode/SpawnAreaSamplerTests.cs, Assets/Tests/EditMode/SpawnServiceWindIntegrationTests.cs, Assets/Tests/EditMode/WindGustMathTests.cs, Assets/Tests/EditMode/WindEventControllerTests.cs, Assets/Tests/EditMode/TrashWindTests.cs, Assets/Tests/EditMode/TutorialSupportTests.cs]
code_maps: [docs/code-map/spawning-pooling.md]
---

# Spawning And Pooling Context

## Ownership

`SpawnService` owns the runtime trash lifecycle: pool creation, timed and event-driven spawning, global and per-type limits, active trash tracking, tutorial spawn blocking, despawning, and lifecycle publication.

Supporting modules:

- `SpawnData` is per-type ScriptableObject configuration: prefab, interval, per-type limit, and sprite variants.
- `SpawnAreaSampler` selects a position from a `BoxCollider`, `RectTransform`, or fallback local cube.
- `SpawnMode` selects timed or event-driven spawning.
- `SpawnTrigger` names environmental triggers; `SpawnTriggerEvents` is their static event seam.
- `EnvironmentalAnimationEventRelay` adapts animation events such as wave and wind into spawn triggers.
- `WindEventController` schedules unlocked gusts, selects local X/Z directions, drives the wind Animator, and moves active `Trash.IsMovable` objects.
- `TrashPath` owns scene-authored river waypoints, `TrashPathDefinition` owns reusable movement speed, and `TrashPathFollower` moves one pooled trash instance along an assigned active path.

## Interface And Events

`SpawnService` publishes `TrashAdded(Trash)` after activation and `TrashRemoved(Trash)` after a tracked object is deactivated. Confirmed player pickup enters through `MousePickUpController.OnTrashClicked`; bonus auto-collection enters through `RewardManager.AutoCollectRequested`.

The tutorial-facing interface is `SetSpawnBlocked(TrashType, bool)` and `IsSpawnBlocked(TrashType)`. Blocking prevents future spawns of that trash type; it does not remove already active trash.

`DespawnTrash(Trash)` accepts only tracked active trash. It updates counts before publishing `TrashRemoved`.

## Spawn Flow

1. `Awake` creates a fixed pool for every valid spawn configuration.
2. `OnEnable` subscribes pickup, auto-collect, and trigger events, then starts the timed loop.
3. The timed loop considers only `TimedSpawn` states, ordered by next due time and Inspector order, and attempts at most one ready spawn per tick.
4. An environmental trigger runs either the legacy `Instant` count or a `DirectionalBurst` distributed across its authored time window and upwind edge band.
5. A spawn is rejected when its configuration is invalid, its type is blocked, a global/per-type cap is reached, its pool is exhausted, or its spawn area is missing.
6. A successful spawn samples a position, activates a pooled object, records it, increments counts, and publishes `TrashAdded`.

An entry may instead provide one or more scene `TrashPath` references. A valid active path supplies waypoint zero as the spawn position and is assigned to the pooled `TrashPathFollower`; multiple paths are selected uniformly. A path-only entry with no `spawnArea` remains dormant while all configured paths are inactive, which gates river spawning through the `RIVER` stage root.

## Invariants

- Total active trash never exceeds `SpawnService.SpawnLimit`.
- Active trash for one configuration never exceeds `SpawnData.SpawnLimit`; its pool is created to that size.
- Timed interval applies only to `TimedSpawn`; trigger and event count apply only to `EventSpawn`.
- Missing spawn areas are logged once per runtime state rather than every attempt.
- Static event subscriptions must remain balanced in `OnEnable`/`OnDisable`.
- Sprite variants are selected during pool creation, not on every reactivation.
- Directional bursts advance only while gameplay is active because `GameManager` pauses behaviours without changing `Time.timeScale`.
- `WindEventController` must remain enabled during `GameManager` pause; do not add it to `pauseOnGamePause`. It freezes its Animator, timers, and motion internally.
- Movable trash is clamped to its owning spawn area's local X/Z bounds and is immune to wind once pickup begins.
- Path-following trash pauses while gameplay is inactive, stops at its final waypoint, stops during pickup, and resets its traversal whenever the pooled instance is assigned again.

## Unity Setup

- Configure each `SpawnService` entry with `SpawnData`, `SpawnMode`, and a spawn area.
- River-only timed entries may omit the spawn area when they provide a path under the matching stage root.
- For event spawning, assign the matching `SpawnTrigger` and choose `Instant` or `DirectionalBurst`. For wind, `Spawn Count` is renewed for each gust and distributed across that gust's directional bursts; directional bursts also expose duration and edge-inset ranges.
- A spawn area normally uses a `BoxCollider`; placement respects its local rotation and scale.
- Animation-driven spawning requires animation events wired to `EnvironmentalAnimationEventRelay`.
- `Editor/SpawnServiceEditor.cs` manually draws serialized fields. Update that custom editor whenever `SpawnService` serialized fields change.

## Verification

`SpawnAreaSamplerTests` verifies rotated BoxCollider sampling, directional edge bands, and X/Z clamping. `SpawnServiceWindIntegrationTests` covers real directional bursts, limits, pause, and legacy instant events. `WindGustMathTests` covers direction selection, movement ranges, and burst timing. `WindEventControllerTests` covers first/repeat scheduling. `TrashWindTests` covers movable-trash pool reset and pickup state.

There is currently no focused coverage for pool reuse, cap enforcement, event counts, timed ordering, lifecycle event order, auto-collection, invalid configurations, or re-enable behavior.

## Important Files

- `Assets/Scripts/SpawnService.cs`
- `Assets/Scripts/SpawnData.cs`
- `Assets/Scripts/SpawnAreaSampler.cs`
- `Assets/Scripts/SpawnMode.cs`
- `Assets/Scripts/SpawnTrigger.cs`
- `Assets/Scripts/SpawnTriggerEvents.cs`
- `Assets/Scripts/EnvironmentalAnimationEventRelay.cs`
- `Assets/Scripts/WindEventController.cs`
- `Assets/Scripts/WindDirection.cs`
- `Assets/Scripts/WindGustMath.cs`
- `Assets/Scripts/DirectionalBurstMath.cs`
- `Assets/Scripts/TrashPathDefinition.cs`
- `Assets/Scripts/TrashPath.cs`
- `Assets/Scripts/TrashPathFollower.cs`
- `Assets/Scripts/Editor/SpawnServiceEditor.cs`
- `Assets/Scripts/Editor/RiverLevelFeatureSetup.cs`
- `Assets/Tests/EditMode/SpawnAreaSamplerTests.cs`
- `Assets/Tests/EditMode/TrashPathFollowerTests.cs`
- `Assets/Tests/EditMode/TutorialSupportTests.cs`
