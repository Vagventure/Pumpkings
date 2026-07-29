# Major Systems Map

## Player Character And Navigation

- `PointAndClickPlayerController` owns 3D point-and-click player navigation and trash pickup approach behavior.
- `NavMeshAgent` moves the player character to clicked walkable ground.
- The current player animator is intentionally minimal: `Vex_Controller` uses `Idle`, `Walk`, and `SS_CrouchIdle`.
- Runtime code should stop and clear the agent path on arrival so the player character does not rotate in place.
- `MousePickUpController` only publishes confirmed pickup events; it does not own raw click detection anymore.

## Trash Data

- `Trash` stores trash type, display name, pollution score, income, pickup time, and audio clips.
- `TrashType` identifies trash categories.
- Per-type progress totals are out of scope for the next progress event feature.

## Spawning And Despawning

- `SpawnService` owns trash pooling, spawn loops, active trash tracking, pickup despawn, and auto-collection requests.
- Event spawn configurations select `Instant` or a directional burst. `Spawn Count` is the total budget for one activated event; directional gusts consume that budget in random-sized groups across successive bursts.
- `WindEventController` starts after a marked progress event, drives a non-looping gust Animator, and moves active `Trash.IsMovable` objects within their spawn areas.
- Spawned trash increases current pollution through `ScoringService`.
- Removed trash decreases current pollution and grants budget through `ScoringService`.

## Pollution, Budget, And Threat

- `ScoringService` owns current pollution, max pollution, budget, purchase validation, and pollution/budget UI updates.
- Current pollution can rise and fall during play.
- Budget can rise from cleanup and fall from purchases.
- `TotalGoldGathered` is gross earned gold from trash, not current budget.
- `TotalThreatProduced` is cumulative post-bonus pollution added by spawns, not current pollution.

## Awareness

- `AwarenessManager` reacts to confirmed shop purchases and passive awareness requests.
- Awareness currently only rises during a run.
- Progress threshold ownership lives in `ProgressTracker`, not `AwarenessManager`.
- Awareness UI should target the next configured awareness progress threshold.

## Level Configuration

- `LevelController` stores three progress event lists: awareness, gold gathered, and threat produced.
- Progress event thresholds should be sorted by required value.
- A progress event may select `GoToNextStage`; `StageManager` performs that transition after dialogue and reward presentation complete.

## Stage Sequence

- `StageManager` treats each stage as a root `GameObject` plus `CinemachineCamera` inside the shared Unity scene.
- It zooms out through the outgoing camera, switches root/camera objects and priorities at the widest view, then eases the incoming camera back to its authored lens.
- Global managers, shared UI, and the persistent output camera/brain stay outside stage roots.
- `RewardManager` retains gameplay pause and queued events until the zoom transition completes.

## Events And Rewards

- `EventDefinition` stores opening event/dialogue line content.
- `ProgressEventDefinition.dialogueChoices` stores selectable player answers for progress dialogue events.
- `EventPresentationResolver` is now intended to be one scene-level dialogue panel controller. `DiscoEventPresentationResolver` stacks lines; `VisualNovelEventPresentationResolver` shows one current line with persistent left/right speaker portraits.
- `RewardManager` owns bonus activation, progress event queueing, reward filtering/application, selected reward display, and gameplay pause/resume. It starts the explicitly assigned scene `EventPresentationResolver`; prefab-based presentation spawning is not supported.
- `RewardItemView` is the prefab UI view for shop item displays and card-style reward item displays. It receives reward data at runtime through `Configure(RewardItem item)`.
- `HotBarController` spawns `ActiveBonusSlotView` prefabs for active bonus HUD slots.
- `RewardManager` keeps one shared FIFO queue for all progress event sources.

## Bonuses

- `BonusDefinition` describes rule-changing bonuses.
- `BonusCatalog` groups available bonuses.
- `RewardManager` stores active bonuses and applies final cost, final awareness, final trash pollution, and final trash income modifiers.
- Timed bonuses can request auto-collection or passive awareness.

## Game State And Lose Flow

- `GameManager` owns high-level gameplay state and loss handling.
- It listens to current pollution and applies scene-configured disable/show/hide behavior on loss.
- Progress totals do not replace current pollution or loss logic.

## Audio

- `AudioManager` reacts to trash spawn/despawn and supports player, SFX, and music playback.

## UI Helpers

- `ProgressBarController` updates fill images for progress bars.
- `RewardSelectionUI` and `RewardSelectionRepresentation` are legacy/fallback reward choice UI. New progress dialogue choices should appear through resolver choice slots.
- `DialogueLineView` binds timestamp, speaker name, speaker role, body text, portrait, and current/past colors on NPC/player line prefabs.
- `RewardItemView` displays runtime-configured reward items in shop and card-style reward containers.
- `ActiveBonusSlotView` displays compact active bonus HUD slots with effect icons, short values, and radial cooldown overlays for timed bonuses.
