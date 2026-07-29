# Agent Readiness Review

## Overall Assessment

This `Assets/Scripts` slice is reasonably small and understandable for a jam project. The main gameplay loop is legible, script count is low, responsibilities are mostly separated, and the event flow is still simple enough for an agent to trace quickly.

The recent `GameManager` change improved the biggest architectural ambiguity: the lose condition is no longer buried inside `ScoringService`. The main remaining risk is still scene and inspector dependence, because the new lose flow must be wired in the scene before it works at runtime.

Current readiness level: moderate to good. Small and medium gameplay changes are now easier to place, but agents still need scene awareness for final wiring.

## What Is Working Well

- Class names are mostly clear and direct.
- Core responsibilities are split across input, spawning, scoring, game state, awareness, bonuses, audio, and UI helper code.
- Event flow is short and easy to trace.
- The project uses plain Unity patterns without deep inheritance or reflection-heavy systems.
- Script sizes are mostly small.
- `SpawnService` uses pooling, which is appropriate for WebGL and jam-scale simplicity.
- `Trash` and `ShopItem` are lightweight data carriers, which keeps core logic concentrated.
- `GameManager` now gives agents one obvious owner for lose-state behavior.

## Main Risks For Coding Agents

### Inspector And Scene Dependence

- Many critical references are injected through the inspector.
- Correct behavior depends on external wiring for:
  - `MouseController.mainCamera`
  - `MouseController.trashLayerMask`
  - `ScoringService` UI references
  - `AwarenessManager` UI references
  - `GameManager.scoringService`
  - `GameManager.disableOnLose`
  - `GameManager.hideOnLose`
  - `GameManager.showOnLose`
  - `BonusManager` references in other systems
  - `AudioManager` source arrays
  - `ShopItem.RequestPurchase()` button hookups
  - awareness reward choice button hookups

This means an agent can change code correctly and still break runtime behavior if scene objects do not match the assumptions.

### Hidden Coupling

- `SpawnService` and `MouseController` still depend on colliders and layer setup that only exists in prefabs or scenes.
- `BonusManager` hardcodes special handling for `ShopItemType.Flyer`.
- `SpawnService` auto-collection assumes the key trash type is `Bottle`.
- Reward definitions encode specific bonus IDs directly in awareness progression.
- `GameManager` now owns lose flow cleanly, but its effect depends on which objects are assigned into its disable/show/hide arrays.

These are understandable shortcuts for a jam, but they make future feature additions less predictable.

### Partial Event Architecture

- Some communication is cleanly event-based.
- Some communication is direct inspector reference calls.
- Some events appear currently unused inside this folder.

That mixed approach is still workable, but it makes it harder to know whether a change is fully safe without scene inspection.

## Naming Clarity

Mostly good:

- `SpawnService`, `ScoringService`, `GameManager`, `BonusManager`, `AwarenessManager`, `ShopItem`, `Trash`

Potential confusion:

- `ScoringService` still uses the names `currentScore` and `maxScore`, even though the values now effectively represent pollution.
- `ProgressBarController` is clear, but both higher-level managers store only a `GameObject` and then fetch the component manually.
- `Trash.Name` shadows the idea of Unity object naming and may be mistaken for `gameObject.name`.

## Script Size And Responsibility

Healthy:

- `Trash`, `TrashType`, `ShopItem`, `ProgressBarController`, `MouseController`, `GameManager`

Acceptable but denser:

- `SpawnService`
- `ScoringService`
- `AwarenessManager`
- `BonusManager`

Largest and most utility-heavy:

- `AudioManager`

`AudioManager` is still manageable, but it is the clearest candidate for future cleanup if audio grows further.

## Magic Numbers And Hardcoded Rules

Present in several places:

- Audio source caps: `10`, `3`, `3`
- Raycast distance: `100f`
- Default pitch and interval values
- Default awareness and bonus catalog entries
- Auto-collect fallback interval: `5f`
- Spawn interval clamp floor: `0.01f`
- Spawn delay floor: `0.1f`

These are not inherently wrong in a jam project, but comments or grouped constants would help future edits.

More important than the numbers themselves are the hardcoded design rules:

- Only flyers receive cost and awareness modifiers.
- Auto-collection targets bottles only.
- Slower spawning bonus only affects configs that opt in.
- Reward choice count is fixed at two.
- The lose threshold is now intentionally manual and inspector-driven.

## Missing Comments

The code is readable enough that heavy commenting is not necessary. What would help are small comments on decisions that are not obvious:

- Which `Behaviour` and `GameObject` references `GameManager` is expected to control on loss.
- Why `SpawnService.MaxPollutionScore` exists but does not drive the lose threshold anymore.
- Why `BonusManager.SpawnRulesChanged` exists if current spawn loops already poll bonus state dynamically.
- Which scene hookups are mandatory for reward choice to work.

## Compile And Runtime Concerns Seen From Code

No obvious compile error is visible in the inspected scripts, but the new `GameManager` requires inspector wiring before the lose flow works in play mode.

Runtime risk areas:

- `MouseController.CheckClick()` assumes `mainCamera` is valid before calling `ScreenPointToRay`.
- The lose flow will not activate until a scene object actually hosts `GameManager` and references are assigned.
- If no reward choices remain, awareness can keep hitting max and logging "No awareness rewards left." on later purchases.
- Several gameplay events may have no listeners unless scene objects outside this folder subscribe.

## Confidence For Future Agents

A future agent can confidently do these tasks from code alone:

- Add another simple bonus type by following current patterns.
- Add another shop item type if it behaves like existing default items.
- Adjust UI text or bar update logic.
- Tune spawn intervals, pool sizes, or audio behavior.
- Extend lose-state presentation through `GameManager` events.

A future agent should be cautious with these tasks:

- Changing loss logic or pollution cap logic without updating `GameManager` wiring.
- Adding new reward UI behavior.
- Refactoring away static events.
- Extending input or interaction modes.
- Adding features that depend on prefab-specific colliders, layers, or button hookups.

## Prioritized Recommendations

### Priority 1: Fix Before Adding More Gameplay

1. Add `GameManager` to the active scene and wire `ScoringService`, `disableOnLose`, `hideOnLose`, and `showOnLose`.
2. Document the required scene and inspector hookups for trash prefabs, UI bars, shop buttons, reward buttons, camera, audio pools, and lose-state objects.
3. Decide whether reward offering is purely text-driven or button-driven, then document the expected wiring so agents do not have to infer it.
4. Add a small null-safety guard or explicit setup expectation for `MouseController.mainCamera`.

### Priority 2: Improve Maintainability

1. Replace `GameObject` references for progress bars with direct `ProgressBarController` references if scene serialization allows it safely.
2. Either remove `BonusManager.SpawnRulesChanged` or wire it into a real spawn-rule refresh path so the code communicates intent clearly.
3. Add brief comments around the non-obvious gameplay shortcuts: flyer-only bonuses, bottle-only automation, two-choice rewards.
4. Consider splitting `ScoringService` responsibilities later if it keeps absorbing economy and UI logic.
5. Consider moving default reward and bonus catalog data out of code and into inspector-authored assets once the jam rules stabilize.

### Priority 3: Optional Polish

1. Standardize event naming and document which events are intended for scene/UI listeners versus purely internal script communication.
2. Expose more balancing values as named constants or better-labeled serialized fields.
3. Add lightweight editor validation messages for missing mandatory references.
4. Add a compact architecture note near the scripts explaining the core event loop for new contributors.
