Status: ready-for-agent

# PRD: Reward Definitions For Bonus And Shop Item Skills

## Problem Statement

The current reward flow can only offer `BonusDefinition` rewards. That is too narrow for the next shop progression step.

Designers need progress events to unlock two kinds of skills:

- passive or permanent bonuses that activate immediately and are shown as active bonuses
- active shop items that appear in the shop and must be clicked/spent on by the player

The shop should start empty. Progress events should decide which active shop skills become visible. Some progress events will simply tell the player that a shop item was unlocked, while others may offer a choice between passive bonuses and active shop items.

The game also needs a visible active-bonus area. Existing active bonuses are stored by `RewardManager`, but there is no controller that spawns active bonus UI representations under a designer-provided parent.

## Solution

Introduce a generic `RewardDefinition` data model with two reward kinds: `Bonus` and `ShopItem`.

`ProgressEventDefinition` should reference `RewardDefinition` values instead of directly referencing `BonusDefinition` values. `RewardManager` should present reward choices using `RewardDefinition`, then apply the selected reward according to its kind.

A `Bonus` reward activates its configured `BonusDefinition` through the existing bonus activation path. A `ShopItem` reward unlocks and spawns its configured `ShopItem` prefab through a new `ShopController`.

Add a `ShopController` that owns which shop item prefabs are visible in the shop. Designers assign a parent transform, and the controller instantiates unlocked shop item prefabs under that parent. The shop starts empty, and duplicate unlocks are ignored.

Add a `BonusController` display owner that listens for active bonus changes and spawns active bonus UI views under a designer-provided parent. For this version, it may reuse the same reward card prefab used by the reward choice UI. The implementation should keep the prefab reference and card configuration isolated so a later task can swap the active-bonus view, change the label, or add right-click detail behavior without rewriting reward logic.

## User Stories

1. As a player, I want the shop to start empty, so that active skills feel earned through progress.
2. As a player, I want a progress event to unlock a new shop item, so that reaching a milestone changes what I can actively do.
3. As a player, I want an unlocked shop item to appear in the shop after I close or accept the event reward, so that the new active skill is visible immediately after the milestone.
4. As a player, I want shop item unlocks to use the same reward presentation as bonuses, so that milestone rewards feel consistent.
5. As a player, I want some events to show only one unlock reward, so that an event can clearly say "you unlocked this" without forcing a fake choice.
6. As a player, I want some events to offer a choice between multiple reward cards, so that I can choose between passive and active skill progression.
7. As a player, I want passive bonuses to appear in an active-bonus area after activation, so that I can see which passive skills are currently active.
8. As a player, I want active shop items to appear in the shop instead of the active-bonus area, so that active and passive skills are visually separated.
9. As a designer, I want to configure a reward as either `Bonus` or `ShopItem`, so that reward setup is clear in the Inspector.
10. As a designer, I want a `Bonus` reward to reference a `BonusDefinition`, so that existing passive effects keep using the current bonus model.
11. As a designer, I want a `ShopItem` reward to reference a shop item prefab, so that active skills can be unlocked by progress events.
12. As a designer, I want the same progress event reward list to contain both bonuses and shop items, so that reward pools can mix passive and active skills.
13. As a designer, I want `choicesToShow = 1` to be valid, so that a progress event can show a single unlock card the player clicks to accept.
14. As a designer, I want progress events without meaningful choices to still use reward UI, so that unlock messaging remains visible and designer-controlled.
15. As a designer, I want duplicate shop item unlocks to be ignored, so that a safe fallback exists if two progress paths reference the same item.
16. As a designer, I want to assign the shop items parent object, so that layout remains controlled by the Unity scene.
17. As a designer, I want to assign the active bonuses parent object, so that active passive skills can be displayed wherever the UI layout needs them.
18. As a designer, I want active bonus UI to reuse the reward card prefab for now, so that the feature can ship before the final active-bonus visual design is decided.
19. As a designer, I want active bonus UI code to allow a later different prefab, so that future right-click details or different bottom labels can be added cleanly.
20. As a programmer, I want reward application to be centralized, so that `Bonus` and `ShopItem` rewards follow one reward-selection path.
21. As a programmer, I want `RewardManager` to keep owning passive bonus activation and timers, so that existing bonus behavior is not duplicated.
22. As a programmer, I want `ShopController` to own shop item instantiation, so that shop visibility rules are not spread across progress events or UI code.
23. As a programmer, I want `BonusController` to own active bonus display only, so that it does not become another owner of bonus effects.
24. As a programmer, I want reward cards to consume `RewardDefinition`, so that the UI does not need separate card flows for bonuses and shop item unlocks.
25. As a programmer, I want invalid reward definitions to fail gracefully, so that missing bonus definitions or shop item prefabs do not block the whole reward queue.
26. As a future agent, I want the active bonus view setup to be loosely coupled to the reward choice UI, so that visual changes do not require changing reward logic.
27. As a future agent, I want shop-item-as-reward to support variants like "cheaper flyer" as a different shop item prefab, so that not every shop improvement has to be a passive cost modifier.

## Implementation Decisions

- Add a `RewardDefinition` ScriptableObject or serializable definition that represents a reward card and reward application target.
- `RewardDefinition` has a reward kind enum with exactly two values: `Bonus` and `ShopItem`.
- A `Bonus` reward references a `BonusDefinition`.
- A `ShopItem` reward references a shop item prefab containing `ShopItem`.
- `RewardDefinition` owns shared display fields such as display name and description for reward-card presentation.
- `ProgressEventDefinition` changes from a list of `BonusDefinition` rewards to a list of `RewardDefinition` rewards.
- `choicesToShow = 1` remains valid and is the intended way to show a single "you unlocked this" reward card.
- `RewardManager` builds reward choices from `RewardDefinition` values.
- `RewardManager` applies selected rewards through one reward application path.
- Applying a `Bonus` reward calls the existing bonus activation behavior.
- Applying a `ShopItem` reward asks `ShopController` to unlock the configured shop item prefab.
- Existing passive bonus behavior, timers, cost modifiers, awareness modifiers, trash modifiers, and passive awareness behavior remain owned by `RewardManager`.
- `RewardManager.BonusActivated` remains the event for passive bonus activation.
- Add a `ShopController` runtime component.
- `ShopController` has a designer-assigned parent transform for instantiated shop item prefabs.
- The shop starts empty; there are no starting shop item prefabs in this version.
- `ShopController` tracks unlocked shop item prefabs at runtime and ignores duplicate unlocks by prefab reference.
- `ShopController` warns and skips invalid unlock requests such as null prefabs or prefabs without a `ShopItem`.
- Add or adapt a `BonusController` runtime component that displays active passive bonuses.
- `BonusController` has a designer-assigned parent transform for active bonus views.
- `BonusController` listens to passive bonus activation and spawns a representation for each active bonus.
- `BonusController` should display active bonuses only; it must not apply gameplay effects.
- Active bonus display can reuse the reward choice card prefab for this version.
- The active bonus display path should expose its own prefab field, even if it points to the same prefab as reward selection in this version.
- Reward card UI should be adapted to configure from `RewardDefinition`, not only from `BonusDefinition`.
- Reward card UI should support a display-only mode or null click callback so it can be reused by active bonus display.
- Missing, invalid, already-active, or duplicate rewards should not stall the progress event queue.
- Shop item rewards are active skills. Bonus rewards are passive or permanent skills.
- A shop improvement may be implemented as a different shop item prefab instead of a passive modifier when that better matches the design.

## Testing Decisions

- Good tests should validate external behavior: which rewards are offered, which reward application action happens, which objects are spawned, and whether duplicates are ignored.
- Tests should avoid asserting private list names or internal implementation details.
- `RewardManager` should be tested for applying a `Bonus` reward by activating the referenced bonus.
- `RewardManager` should be tested for applying a `ShopItem` reward by requesting a shop item unlock.
- `RewardManager` should be tested for filtering or skipping invalid reward definitions without blocking the queue.
- `RewardManager` should be tested for supporting a single visible reward when `choicesToShow = 1`.
- `ShopController` should be tested for spawning an unlocked shop item prefab under the configured parent.
- `ShopController` should be tested for ignoring duplicate unlocks of the same prefab.
- `ShopController` should be tested for rejecting null or invalid shop item prefabs gracefully.
- `BonusController` should be tested for spawning one active bonus view when a bonus is activated.
- `BonusController` should be tested for not spawning duplicate active bonus views for the same active bonus.
- Reward card UI tests can stay lightweight and focus on public configuration behavior: display text, click callback, and display-only mode.
- If Unity UI tests are too costly for this project right now, prioritize edit-mode/component tests for reward application and controller spawning, with manual Unity verification for actual prefab layout.

## Out of Scope

- Final active bonus visual design is out of scope.
- Right-click details for active bonuses are out of scope, but the implementation should not make that hard to add later.
- Custom active bonus labels or alternate card layouts are out of scope for this version.
- Starting shop items are out of scope; the shop starts empty.
- Save/load persistence for unlocked shop items or active bonuses is out of scope.
- Weighted reward rarity is out of scope.
- A complete skill tree editor is out of scope.
- Rebalancing shop item costs, awareness values, or bonus values is out of scope.
- Replacing static events with a broader event bus or dependency injection rewrite is out of scope.
- Changing purchase validation, budget rules, or current pollution rules is out of scope.
- Reworking `ShopItem` effect execution beyond existing purchase behavior is out of scope.

## Further Notes

- This PRD intentionally expands the earlier progress event reward model, which previously treated new reward types as out of scope.
- Use the terms `Bonus` and `ShopItem` for reward kinds. Do not introduce longer enum names like `PassiveBonus` or `ActiveShopItem` unless a later task explicitly revisits naming.
- Designers should be able to create an event like "You reached X gold; you unlocked this shop item" by configuring one `ShopItem` reward and `choicesToShow = 1`.
- Designers should also be able to create mixed reward choices where one card activates a passive bonus and another card unlocks a shop item.
- Unity scene and prefab wiring will be required after implementation because `ShopController`, `BonusController`, reward card slots, and reward definitions all need Inspector references.
