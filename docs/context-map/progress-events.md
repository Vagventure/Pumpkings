# Progress Event Decisions

- `ProgressEventDefinition` is the unified model that replaced legacy tier definitions.
- Tier numbers should be removed from this flow.
- Runtime totals reset with the scene/run.
- Gold gathered and threat produced are cumulative gross totals.
- Threat produced uses post-bonus pollution values.
- Reward choices are optional per progress event.
- Empty progress events should not block the queue.
- New progress event UI should use one scene-level `EventPresentationResolver`.
- Use `DiscoEventPresentationResolver` for stacked dialogue and `VisualNovelEventPresentationResolver` for one-message visual novel presentation.
- `ProgressEventDefinition` must not contain presentation prefab data; presentation always goes through the assigned scene resolver.
- Player answers live in `ProgressEventDefinition.dialogueChoices`, not `EventDefinition`.
- `DialogueChoiceDefinition.buttonText` is the short choice label; `playerLine` is the full line spawned after selection.
- `EventPresentationResolver` owns active line reveal, choice hotkeys, player line reveal, and final continue.
- `Space` skips/continues reveal; `1/2/3` choose visible choice slots only while choices are active.
- Dialogue line prefabs should contain `DialogueLineView` and `UIRevealController`, not an event presentation resolver or a continue button.
- The continue button belongs to the scene dialogue panel.
