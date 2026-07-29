---
read_when: [architecture, ownership, cross-system-flow]
avoid_when: [localized-single-system-change]
primary_files: [Assets/Scripts/GameManager.cs, Assets/Scripts/SpawnService.cs, Assets/Scripts/ScoringService.cs, Assets/Scripts/RewardManager.cs, Assets/Scripts/EventPresentationResolver.cs]
tests: []
code_maps: [CODEMAP.md]
---

# Architecture Overview

## Runtime Owners

- `PointAndClickPlayerController` owns player click-to-move navigation, trash click targeting, NavMesh destination setting, arrival stopping, and direct animation playback.
- `MousePickUpController` publishes confirmed trash collection events after the player has reached the target trash and completed pickup timing.
- `SpawnService` owns trash lifecycle and publishes trash added/removed events.
- `WindEventController` owns per-scene wind scheduling, animation direction, and motion of wind-movable trash; it delegates object creation and limits to `SpawnService`.
- `TrashPathFollower` owns per-instance river traversal using scene geometry from `TrashPath` and reusable speed from `TrashPathDefinition`.
- `ScoringService` owns current pollution, budget, purchase validation, and UI updates for those values.
- `RecyclingPatrolService` owns Patrol purchase availability, shared target claims, cooldowns, and scene wiring; each `RecyclingPatrolAgent` owns its own NavMesh target/pickup/exit lifecycle.
- `CursorController` owns custom cursor presentation and central world/UI hover routing; `MoneyFlyVfxController` owns cosmetic income flight into the overlay HUD.
- `AwarenessManager` owns awareness value changes and awareness UI.
- `GameManager` owns gameplay state and loss from current pollution.
- `LevelController` owns level-specific milestone configuration.
- `RewardManager` owns bonuses, event queue orchestration, reward choice, and pause/resume during reward flow.
- `StageManager` owns the ordered in-scene stage sequence, zoom transition, stage-root/camera activation, Cinemachine priorities, and completion signals.
- `EventPresentationResolver` is the shared scene-level progress event presentation contract. Use `DiscoEventPresentationResolver` for stacked dialogue history and `VisualNovelEventPresentationResolver` for one-message-at-a-time visual novel presentation.

## UI Direction

Reward item UI prefabs use one component: `RewardItemView`. It displays a runtime-configured `RewardItem` and owns UI references for title, subtitle, description, icon, optional cost, effect icon, effect value, and optional button.

`RewardItemView` must not serialize a reward asset on the prefab. Controllers provide the asset at runtime through `Configure(RewardItem item)`.

Active bonus HUD slots use `ActiveBonusSlotView`, spawned by `HotBarController` from one configured slot prefab.

Progress event dialogue uses one scene-level `EventPresentationResolver` plus separate left/right `DialogueLineView` prefabs. The continue button and choice slots belong to the resolver panel. Line prefabs own visual style and text binding only.

See [Reward item view context](reward-item-view.md).
