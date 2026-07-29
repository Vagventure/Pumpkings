# Communication Style Map

The codebase primarily uses static events plus scene singletons.

## Existing Important Events

- `SpawnService.TrashAdded`
- `SpawnService.TrashRemoved`
- `SpawnTriggerEvents.Triggered(SpawnTriggerContext)`; directional contexts carry local wind direction and the remaining gust duration.
- `MousePickUpController.OnTrashClicked`
- `RewardItemView.Clicked`
- `ScoringService.ItemPurchaseConfirmed`
- `ScoringService.OnPollutionChanged`
- `RewardManager.AutoCollectRequested`
- `RewardManager.PassiveAwarenessRequested`

## Progress Events

- `AwarenessManager.AwarenessGained`
- `ScoringService.GoldGathered`
- `ScoringService.ThreatProduced`
- `ProgressTracker.ProgressEventReached`
- `RewardManager.ProgressEventCompleted`; `WindEventController` reacts when the completed definition uses `StartWind`.
