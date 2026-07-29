---
read_when: [progress-threshold, dialogue-event, reward-choice, event-queue]
avoid_when: [shop-only, unrelated-ui]
primary_files: [Assets/Scripts/ProgressTracker.cs, Assets/Scripts/ProgressEventDefinition.cs, Assets/Scripts/RewardManager.cs, Assets/Scripts/EventPresentationResolver.cs]
tests: [Assets/Tests/EditMode/LevelControllerProgressEventsTests.cs, Assets/Tests/EditMode/RewardCatalogTests.cs, Assets/Tests/EditMode/EventDialogueDefinitionsTests.cs]
code_maps: [docs/code-map/progress-events.md]
---

# Progress Event Context

Progress events replace awareness-only tiers with unified milestone events.

## Direction

- Remove tier-number semantics from the event/reward flow.
- Use `ProgressEventDefinition`; it replaces the legacy tier model.
- Configure progress events in `LevelController` as awareness, gold gathered, and threat produced lists.
- Add a runtime `ProgressTracker`.
- Keep totals runtime-only; no persistence is needed for this short browser game.
- Feed all progress events into one FIFO queue owned by `RewardManager`.
- Keep reward choices optional per progress event.

## Current Dialogue Event Model

Progress event presentation is now owned by a runtime event presentation resolver, not a one-off popup prefab per event.

Runtime ownership:

- `ProgressTracker` detects threshold crossings and publishes `ProgressEventContext`.
- `RewardManager` owns the FIFO queue, gameplay pause/resume, reward filtering, reward application, and progress event completion.
- `RewardManager` must use its explicitly assigned scene-level `EventPresentationResolver`.
- Progress presentation prefab fallback is removed; do not add presentation prefab fields back to `ProgressEventDefinition`.
- `EventPresentationResolver` owns dialogue UI state: current line reveal, choices, player line reveal, final continue, and hotkeys.
- `DiscoEventPresentationResolver` keeps previous lines visible as stacked dialogue history.
- `VisualNovelEventPresentationResolver` shows one current line and updates/dims persistent left/right speaker portraits.
- `UIVfxController` still owns reveal timing and typing SFX signals through `UIRevealController`.

Authoring data:

- `EventDefinition` stores a linear `dialogueLines` list. Each line stores `speakerSide`, `speaker`, `expression`, `text`, and optional `voiceClip`.
- `SpeakerDefinition` stores reusable speaker display data and Neutral/Happy/Sad portraits.
- `ProgressEventDefinition.dialogueChoices` stores player-selectable answers.
- `ProgressEventDefinition.completionEffect` optionally requests one semantic post-completion action. `StartWind` activates the scene wind cycle; `GoToNextStage` starts the Cinemachine stage transition. Both run only after the full progress event flow completes and cannot be combined on one event.
- Each `DialogueChoiceDefinition` stores `reward`, `buttonText`, `playerLine`, and optional `playerVoiceClip`.
- `buttonText` falls back to `reward.Title`; `playerLine` falls back to `buttonText`.
- If `dialogueChoices` is empty, `RewardManager` can still build runtime dialogue choices from configured `rewardItems`.

Dialogue flow:

- Event starts by rendering the first configured dialogue line.
- `Space` skips active reveal; after reveal it works as continue.
- `1/2/3` select visible choice slots only while choices are waiting.
- Selecting a choice hides/disables choices, spawns a player line, reveals it, then waits for final continue.
- Reward is returned to `RewardManager` only after the selected player line and final continue.
- After a reward is selected, `RewardManager` can show a centered reward display using `RewardItemView`. The player confirms it with an OK/button click; only then is the reward applied and the flow completed.

Runtime history:

- Dialogue history is runtime-only and global for the current session.
- Previous lines render as past lines through `DialogueLineView.SetPast(true)`.
- Current lines render as current lines through `DialogueLineView.SetPast(false)`.
- Timestamps currently come from a mock timestamp provider and are intended to be replaceable by a later `TimeController`.

## Prefab Contract

There should be one explicitly assigned scene-level dialogue panel with a concrete `EventPresentationResolver`.

Panel object:

- Has `EventPresentationResolver`.
- Has/points to `lineContainer`, or for Visual Novel uses `VisualNovelPanelBindings`.
- Has `continueButton`.
- Has `choiceSlots`.
- References left/right line prefabs.

Line prefabs:

- Have `DialogueLineView`.
- Have `UIRevealController` configured to reveal only the body text.
- Should not have an event presentation resolver.
- Should not have a continue button.
- Own visual style, alignment, colors, portrait placement, and TMP layout.

`EventPresentationResolver` has defensive guards for older prefabs that still contain nested resolvers, but the intended setup is to remove resolver components from dialogue line prefabs.

## Out Of Scope

- Per-trash-type progress totals.
- Cross-session persistence.
- New save/load systems.
- New reward effect types.
- Loss condition changes.
