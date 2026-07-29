# Gameplay Loop Map

## What The Game Is About

Plastic Seeds is a short 3D point-and-click cleanup game about moving a player character through a scene, reducing pollution pressure, earning budget from cleanup, converting that budget into awareness, and unlocking event-driven bonuses.

## Current Main Gameplay Loop

1. `SpawnService` activates pooled `Trash` objects.
2. `SpawnService.TrashAdded` fires.
3. `ScoringService` applies post-bonus trash pollution as current pollution.
4. `GameManager` listens to current pollution and can enter the lost state.
5. `AudioManager` can play spawn audio.
6. The player clicks walkable ground or trash through `PointAndClickPlayerController`.
7. For walkable ground, the player character walks to the sampled NavMesh destination and stops cleanly.
8. For trash, the controller first validates a complete NavMesh path whose endpoint is within pickup range, then selects `pendingTrash`; invalid targets do not leave a partial pickup state.
9. When close enough, the player character cross-fades to `SS_CrouchIdle` for that trash's `PickupTime`.
10. `MousePickUpController.CollectTrash(trash)` fires `MousePickUpController.OnTrashClicked`.
11. `SpawnService` despawns the clicked trash.
12. `SpawnService.TrashRemoved` fires.
13. `ScoringService` removes the registered pollution value and adds post-bonus trash income to budget.
14. The player spends budget by clicking a `RewardItemView` configured with a `ShopItemDefinition`.
15. `ScoringService` validates the purchase and emits `ItemPurchaseConfirmed`.
16. `AwarenessManager` applies post-bonus awareness gain and updates the awareness bar.
17. `RewardManager` owns progress event dialogue flow, reward selection/application, bonus activation, and gameplay pause/resume during reward flow.

## Progress Event Loop

1. `AwarenessManager` publishes awareness gained.
2. `ScoringService` publishes gold gathered when trash grants income.
3. `ScoringService` publishes threat produced when trash adds pollution.
4. `ProgressTracker` accumulates run totals for awareness, gold gathered, and threat produced.
5. `ProgressTracker` checks the relevant `LevelController` progress event list.
6. Each crossed threshold emits a `ProgressEventDefinition`.
7. `RewardManager` enqueues all progress events into one FIFO queue.
8. `RewardManager` starts the scene-level `EventPresentationResolver`, passes event content and dialogue choices, applies the selected reward after final continue, then resumes gameplay or advances to the next queued event.
