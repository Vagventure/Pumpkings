Status: ready-for-agent

# PRD: Tiered Reward Selection Flow

## Problem Statement

The game needs a popup-driven reward choice flow where the player picks one reward from a small set of card-like choices after reaching awareness milestones.

The current awareness reward flow is too narrow for the intended incremental design. Awareness should not reset after a reward. Instead, awareness is cumulative, the next target scales upward by tier, the awareness bar fills against the current tier target, and rewards are offered when the player crosses each configured tier.

The project also needs the existing bonus system to become the owner of reward selection without renaming every bonus data type. The manager should be renamed to `RewardManager`, while `BonusDefinition` and `BonusCatalog` remain the data model for effects that rewards activate.

## Solution

Introduce a tiered reward selection flow built around cumulative awareness.

Awareness will be configured as a list of tier point targets, such as Tier I at 100 awareness points and Tier II at 500 awareness points. When the player's cumulative awareness reaches a tier target, `AwarenessManager` emits an event identifying the reached tier. It does not choose rewards and does not reset awareness.

`RewardManager` listens for reached tiers, queues them, pauses gameplay through `GameManager`, chooses reward cards from the configured tier offer, displays the reward selection UI, activates the selected `BonusDefinition`, and then either shows the next queued tier reward or resumes gameplay.

The reward popup is a reusable UI surface made by the designer. Code supplies a card prefab representation and reward data. Each card receives a `BonusDefinition`, displays its name and effect description, and invokes selection when clicked.

## User Stories

1. As a player, I want to receive a reward choice when I reach an awareness tier, so that awareness progress gives meaningful upgrades.
2. As a player, I want to choose one reward from multiple cards, so that the upgrade path feels player-driven.
3. As a player, I want awareness to keep increasing cumulatively, so that the game feels incremental instead of resetting progress after every reward.
4. As a player, I want the awareness bar to refill against the current target tier, so that the displayed progress accurately matches the next milestone.
5. As a player, I want 300 awareness out of a 500-point tier target to show 60% fill, so that the progress bar is mathematically clear.
6. As a player, I want the game to pause while I choose a reward, so that I am not punished by spawning trash or ticking passive effects while reading options.
7. As a player, I want UI animations and buttons to remain responsive during reward selection, so that the popup feels polished.
8. As a player, I want multiple crossed tiers to be handled in order, so that a large awareness gain does not skip rewards.
9. As a player, I want Tier I rewards to appear before Tier II rewards if both are earned at once, so that progression remains understandable.
10. As a designer, I want to configure tier point targets in the inspector, so that awareness pacing can be tuned without code changes.
11. As a designer, I want to assign reward pools per tier, so that different stages of the game can offer different upgrades.
12. As a designer, I want a tier offer to support two or three visible choices, so that the reward popup can scale with the design.
13. As a designer, I want the vertical slice to be able to show all configured rewards for a tier, so that small-scope testing remains straightforward.
14. As a designer, I want future versions to randomly choose from a larger reward pool, so that the same tier can vary across runs.
15. As a designer, I want a reward to depend on a previously unlocked bonus, so that upgrades like level two can appear only after level one.
16. As a designer, I want reward cards to display effect descriptions, so that players understand what they are choosing.
17. As a designer, I want to build the visual popup prefab myself, so that layout, card visuals, and animation stay under UI control.
18. As a programmer, I want `AwarenessManager` to own awareness score and tier detection only, so that reward selection logic does not leak into awareness scoring.
19. As a programmer, I want `RewardManager` to own reward offers and selected bonus activation, so that reward behavior has a single runtime owner.
20. As a programmer, I want `BonusDefinition` and `BonusCatalog` to remain as the effect data model, so that the change does not require a broad asset rename.
21. As a programmer, I want the current `BonusManager` role renamed to `RewardManager`, so that the manager name matches its broader reward-selection responsibility.
22. As a programmer, I want passive reward timers to pause during reward selection, so that soft pause applies to timed effects too.
23. As a programmer, I want gameplay pause to avoid `Time.timeScale = 0`, so that UI animation and interaction do not depend on unscaled-time setup.
24. As a programmer, I want `GameManager` to expose explicit pause and resume operations, so that reward selection can pause gameplay consistently.
25. As a programmer, I want shop interaction paused during reward selection, so that players cannot trigger more purchases while a reward is pending.
26. As a future agent, I want reward UI scripts to be data-driven, so that the designer can swap card visuals without changing reward logic.
27. As a future agent, I want reached tier events to carry a tier identity, so that reward offers do not need to duplicate point thresholds.
28. As a future agent, I want reward queueing to be explicit, so that multi-tier jumps remain deterministic.
29. As a future agent, I want missing or exhausted reward pools to fail gracefully, so that a bad inspector setup does not break the whole game loop.
30. As a future agent, I want the implementation to stay small and inspector-driven, so that it fits the current jam-style Unity project.

## Implementation Decisions

- Rename the existing bonus runtime manager concept to `RewardManager`.
- Keep `BonusDefinition`, `BonusCatalog`, `BonusEffectType`, and existing bonus effect semantics.
- `RewardManager` remains responsible for active bonuses, bonus timers, cost modifiers, awareness modifiers, trash modifiers, and activation events.
- `RewardManager` gains reward selection ownership: tier offers, reward choice generation, reward queueing, UI presentation, selected reward activation, and popup-driven pause/resume coordination.
- `AwarenessManager` owns cumulative awareness score and tier detection.
- Awareness score does not reset when a reward is selected.
- Awareness tiers are configured as point targets on `AwarenessManager`.
- Reward offers are configured by tier identity on `RewardManager`, not by duplicating awareness point thresholds in reward data.
- The awareness bar fill uses cumulative current awareness divided by the current tier target. For example, 300 current awareness against a 500 target displays 60%.
- When a tier is crossed, `AwarenessManager` emits an event for that tier.
- If multiple tiers are crossed at once, each reached tier is emitted or queued in tier order so that rewards are presented in progression order.
- `RewardManager` queues reached tiers and shows one reward popup at a time.
- `RewardManager` filters reward candidates before display.
- A reward candidate is valid only if it is not already active.
- A reward candidate with no prerequisite bonus is valid by default.
- A reward candidate with a prerequisite bonus is valid only if that prerequisite is already active.
- Upgrade chains use direct `BonusDefinition` references as prerequisites, not string keys.
- The vertical slice may show all configured valid rewards for a tier when the configured visible choice count covers the full list.
- The system should still support random selection from a larger valid reward pool for later balancing.
- The visible reward count should be inspector-configurable per offer or otherwise easy to tune.
- `RewardSelectionUI` is a reusable UI controller that receives reward choices from `RewardManager`.
- `RewardSelectionUI` instantiates or binds card representations from a configured reward card prefab.
- `RewardSelectionRepresentation` is the reward card script.
- `RewardSelectionRepresentation` receives a `BonusDefinition`, displays the reward name and description/effect text, owns a button hookup, and reports selection back to the UI/controller.
- The visual popup prefab is designer-authored; code should not assume a final layout beyond the required hook fields.
- `GameManager` gains soft-pause operations for gameplay pause and resume.
- Soft pause does not use `Time.timeScale = 0`.
- Soft pause disables configured gameplay behaviours and shop interaction while leaving UI responsive.
- Passive reward timers must not advance while the game is paused.
- `RewardManager` should not be disabled by the soft pause list because it must continue to operate the reward popup and know when to resume.
- `RewardManager` opens reward selection by asking `GameManager` to pause gameplay.
- `RewardManager` resumes gameplay only after the last queued reward popup has completed.
- Existing lose-flow state should remain compatible with the new pause flow.
- Inspector-driven wiring is acceptable and expected for this vertical slice.

## Testing Decisions

- Good tests should validate externally visible behavior rather than private implementation details.
- `AwarenessManager` should be tested for cumulative awareness behavior.
- `AwarenessManager` should be tested for emitting reached tiers when awareness crosses configured targets.
- `AwarenessManager` should be tested for not resetting awareness after a tier reward is chosen.
- `AwarenessManager` should be tested for progress fill against the current tier target, including the 300 of 500 equals 60% case.
- `RewardManager` should be tested for choosing only valid reward candidates.
- `RewardManager` should be tested for excluding already active rewards.
- `RewardManager` should be tested for respecting prerequisite bonus references.
- `RewardManager` should be tested for queueing multiple reached tiers and presenting them in order.
- `RewardManager` should be tested for activating exactly the selected reward.
- `RewardManager` should be tested for pausing gameplay when reward selection opens and resuming only after the final queued selection closes.
- `RewardManager` should be tested for not advancing timed passive bonus effects while the game is paused.
- `GameManager` should be tested for soft pause and resume behavior through visible enabled/disabled behaviour state.
- Reward UI tests can stay lightweight for the vertical slice and focus on whether a card reports the selected reward through the public UI contract.
- Existing tests or future tests around purchase-to-awareness flow should be extended so that shop purchases increase cumulative awareness and can trigger tier events.
- If Unity test coverage is not yet established, prioritize small play mode or component-level integration tests around manager behavior over visual UI tests.

## Out of Scope

- Full visual design of the reward popup.
- Final card art, animation polish, sounds, and transitions.
- Full rename of all `Bonus` data types to `Reward`.
- Save/load persistence for cumulative awareness, claimed tiers, or active rewards.
- Complex weighted reward rarity.
- Duplicate reward replacement rules beyond active/prerequisite filtering.
- A complete upgrade tree editor.
- Rebalancing all tier point values and reward pools.
- Reworking the shop UI beyond pausing interaction during reward selection.
- Changing the existing trash, shop, pollution, or budget scoring rules except where they interact with active rewards.
- Using `Time.timeScale = 0` for reward selection pause.

## Further Notes

- The accepted terminology is that the manager becomes `RewardManager`, while effect data remains `BonusDefinition`.
- Awareness is an incremental cumulative score.
- Reward offers are tier-based, not threshold-based.
- The vertical slice should be small but not throwaway: it should show all configured rewards when scope is small, while preserving the future path to random choices from larger pools.
- The reward popup should be designer-authored in Unity, with code exposing clear hook points for the popup object, card prefab, card container, text fields, and button.
- Unity scene and prefab wiring will be required after implementation.
