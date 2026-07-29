# Progress Events And Dialogue Code Map

Context: [Progress events](../contexts/progress-events.md)

## Progress And Reward Working Set

- `Assets/Scripts/ProgressTracker.cs`, `ProgressMetric.cs`, `ProgressEventDefinition.cs` — totals, thresholds, context publication, and authored event data.
- `Assets/Scripts/LevelController.cs` — per-level progress lists and starting music state.
- `Assets/Scripts/RewardManager.cs` — FIFO orchestration, pause/resume, choices, reward application, and completion publication.
- `Assets/Scripts/StageManager.cs` — consumes `GoToNextStage`, runs the zoom transition, then returns pause/queue control after completion.
- `Assets/Scripts/RewardCatalog.cs`, `RewardItem.cs`, `BonusDefinition.cs` — reward data and selection.
- `Assets/Scripts/Editor/RewardManagerEditor.cs` — manually drawn RewardManager Inspector.

## Dialogue Presentation Working Set

- `Assets/Scripts/EventPresentationResolver.cs` — presentation interface/seam.
- `Assets/Scripts/DiscoEventPresentationResolver.cs` and `VisualNovelEventPresentationResolver.cs` — scene adapters.
- `Assets/Scripts/EventDefinition.cs`, `DialogueChoiceDefinition.cs`, `SpeakerDefinition.cs` — authored dialogue data.
- `Assets/Scripts/DialogueLineView.cs`, `DialogueChoiceView.cs`, `DialogueHistoryRuntime.cs`, `VisualNovelPanelBindings.cs` — runtime UI support.
- `Assets/Scripts/Editor/VisualNovelEventPresentationResolverEditor.cs` — custom Inspector.

## Tests

- `Assets/Tests/EditMode/LevelControllerProgressEventsTests.cs`
- `Assets/Tests/EditMode/RewardCatalogTests.cs`
- `Assets/Tests/EditMode/EventDialogueDefinitionsTests.cs`
