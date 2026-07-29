Status: ready-for-agent

# PRD 004: Tier Event Reward Flow

## Problem Statement

The current awareness tier reward flow jumps directly from reaching a tier to showing reward choices. The game needs an intermediate event step before the reward panel, so a tier can introduce a short scene, dialogue, sound, animation, or other authored presentation before the player chooses a reward.

The current tier setup is also split across two places: awareness thresholds live on `AwarenessManager`, while reward offers live on `RewardManager`. This makes tier content harder to reason about because the designer must keep separate inspector lists aligned by tier number.

The desired model is that a tier is one authored unit: required awareness, optional event prefab, and reward pool all belong together.

## Solution

Introduce a `TierDefinition` ScriptableObject as the single source of truth for awareness tier content.

Each `TierDefinition` contains the tier number, required awareness value, optional event prefab, visible reward choice count, and reward pool. `AwarenessManager` uses the tier definitions only to detect awareness thresholds and emit reached tiers. `RewardManager` remains the owner of the runtime flow: it queues reached tiers, pauses gameplay, runs the optional tier event prefab, waits for the event to finish, then opens the reward panel using the tier's reward pool.

Tier event prefabs are intentionally prefab-driven, not shared data-only dialogue. Each event can own different text, assets, audio, animation, layout, and custom scripts. The shared runtime contract is small: the prefab has a component that can start the event and invoke an end callback when the event is complete.

If a tier has no event prefab, `RewardManager` skips the event step and opens the reward panel immediately.

## User Stories

1. As a player, I want a tier event to appear before reward choices, so that reaching a tier feels like a moment instead of only a menu.
2. As a player, I want the reward panel to appear after the event finishes, so that the reward feels like the event's payoff.
3. As a player, I want simple text-based tier events in the first version, so that the feature can ship before final narrative assets exist.
4. As a player, I want future tier events to support audio, images, scenes, and animation, so that important milestones can feel distinct.
5. As a player, I want tiers without events to still work, so that not every milestone needs a custom scene.
6. As a player, I want the game to remain paused while the event and reward panel are open, so that I can read and choose without gameplay pressure.
7. As a player, I want multiple crossed tiers to resolve in order, so that a large awareness gain does not skip events or rewards.
8. As a player, I want Tier 1's event and reward to complete before Tier 2 starts, so that progression remains understandable.
9. As a designer, I want each tier to be configured in one asset, so that the threshold, event, and rewards cannot drift apart.
10. As a designer, I want to assign an event prefab per tier, so that each event can have its own layout, text, art, sounds, and scripts.
11. As a designer, I want the event prefab to be optional, so that simple tiers can go straight to rewards.
12. As a designer, I want event prefabs to live under a configured UI bucket on the canvas, so that their placement is controlled by the scene UI hierarchy.
13. As a designer, I want `TierDefinition` rewards to act as a pool, so that the player can see a random subset of valid rewards.
14. As a designer, I want the number of reward choices to remain configurable, so that different tiers can show two, three, or another tuned count.
15. As a designer, I want existing `BonusDefinition` assets to remain the reward effects, so that current reward data is not renamed or recreated.
16. As a designer, I want reward prerequisite filtering to keep working, so that upgrade chains still unlock in the intended order.
17. As a programmer, I want `TierDefinition` to replace duplicated tier configuration, so that thresholds and rewards are not maintained in separate inspector lists.
18. As a programmer, I want `AwarenessManager` to stay focused on awareness scoring and tier detection, so that event and reward presentation do not leak into scoring.
19. As a programmer, I want `RewardManager` to remain the flow owner, so that queueing, pause, event presentation, reward selection, and resume happen in one place.
20. As a programmer, I want the tier event prefab contract to be small, so that custom event prefabs can vary without changing `RewardManager`.
21. As a programmer, I want `RewardManager` to handle missing event prefabs gracefully, so that partial tier setup does not block reward selection.
22. As a programmer, I want `RewardManager` to handle missing event controller components gracefully, so that bad prefab setup produces a warning and still proceeds.
23. As a programmer, I want the event instance destroyed after completion, so that old event UI does not remain in the canvas.
24. As a programmer, I want existing reward filtering to be reused, so that already active bonuses and unmet prerequisites are still excluded.
25. As a programmer, I want existing reward activation to be reused, so that selected rewards still activate through the current bonus system.
26. As a future agent, I want the model to support richer events later, so that audio, portraits, timeline scenes, or animated panels can be added without reworking tier progression.
27. As a future agent, I want the first implementation to be small and inspector-driven, so that Unity scene and prefab wiring remains explicit.

## Implementation Decisions

- Add a `TierDefinition` ScriptableObject.
- `TierDefinition` is the source of truth for tier number, required awareness, optional event prefab, visible reward choice count, and reward pool.
- Keep using `BonusDefinition` as the reward effect data model.
- Keep using `RewardSelectionUI` for the reward panel.
- Keep `RewardManager` as the owner of the tier flow. Do not add a separate `TierFlowManager`.
- Move tier queueing responsibility into the `RewardManager` flow that includes both events and rewards.
- `AwarenessManager` should reference the same tier definitions and derive threshold checks from `requiredAwareness`.
- `AwarenessManager` should continue emitting reached tiers in tier order when awareness crosses one or more thresholds.
- `AwarenessManager` should not know about event prefabs, reward UI, or reward activation.
- `RewardManager` should locate the reached `TierDefinition` by tier number.
- `RewardManager` should pause gameplay before opening an event or reward panel.
- `RewardManager` should resume gameplay only after the event and reward panel are both complete and no queued tier remains.
- If a reached tier has an event prefab, `RewardManager` instantiates it under a configured canvas bucket or event container.
- If a reached tier has no event prefab, `RewardManager` skips directly to reward selection.
- Tier event prefabs use a shared runtime contract, such as a `TierEventController` component with start and end behavior.
- The event contract should let `RewardManager` start the event and provide a callback that is invoked when the event calls its end operation.
- The first concrete event prefab can be text-only, but the architecture must not assume text-only events.
- If the event prefab is missing its required controller component, `RewardManager` should log a warning, clean up the instance if needed, and continue to reward selection.
- After a tier event finishes, `RewardManager` destroys the event instance before opening the reward panel.
- `TierDefinition.rewards` is a pool, not necessarily the exact visible cards.
- `RewardManager` filters the tier reward pool using existing active-bonus and prerequisite rules.
- `RewardManager` randomly selects up to the tier's configured choice count from the valid reward pool.
- Existing reward fallback behavior should remain: if the reward UI is missing but valid rewards exist, the implementation may activate the first valid reward with a warning.
- Existing active bonus, passive awareness, cost modifier, trash modifier, and timed bonus behavior remain in `RewardManager`.
- Existing soft pause behavior remains in `GameManager`; this PRD does not introduce `Time.timeScale = 0`.
- Existing inspector data must be migrated from `AwarenessManager` awareness tiers and `RewardManager` reward offers into new `TierDefinition` assets.

## Testing Decisions

- Good tests should validate external behavior rather than private implementation details.
- `AwarenessManager` should be tested for detecting reached tiers from `TierDefinition.requiredAwareness`.
- `AwarenessManager` should be tested for emitting multiple reached tiers in order when one awareness gain crosses multiple thresholds.
- `AwarenessManager` should be tested for current tier target and progress behavior after moving threshold data into tier definitions.
- `RewardManager` should be tested for queueing reached tiers and processing them one at a time.
- `RewardManager` should be tested for running an event before showing rewards when the tier has an event prefab.
- `RewardManager` should be tested for skipping directly to rewards when the tier has no event prefab.
- `RewardManager` should be tested for showing rewards only after the event signals completion.
- `RewardManager` should be tested for destroying or clearing the event instance after completion.
- `RewardManager` should be tested for filtering tier reward pools using active bonus and prerequisite rules.
- `RewardManager` should be tested for random selection respecting the tier's configured choice count.
- `RewardManager` should be tested for pausing gameplay while an event or reward panel is open.
- `RewardManager` should be tested for resuming gameplay only after the final queued tier flow completes.
- `RewardManager` should be tested for handling a missing event controller gracefully.
- UI visual polish does not need automated tests in this PRD; component-level flow tests are enough for the first implementation.

## Out of Scope

- Final visual design for tier event panels.
- Final narrative writing for tier event text.
- Final audio, portraits, animations, or timeline scenes for tier events.
- Save/load persistence for completed events, claimed tiers, active rewards, or awareness score.
- A custom Unity editor for bulk migrating old tier data.
- A graph-based event scripting system.
- Weighted reward rarity.
- Replacing `BonusDefinition` with a differently named reward asset type.
- Creating a separate `TierFlowManager`.
- Reworking reward card visuals.
- Reworking the soft pause system beyond what is needed for event plus reward flow.

## Further Notes

- The accepted flow is: tier reached, optional event prefab, event ends, reward panel opens, player chooses reward, next queued tier or gameplay resumes.
- Event prefabs are preferred over data-only dialogue because each event may need unique text, assets, sounds, and scripts.
- The first event implementation can be a simple text prefab, but it should use the same event controller contract intended for richer events.
- The Unity scene will need a canvas bucket for event prefabs and new `TierDefinition` assets for each configured tier.
