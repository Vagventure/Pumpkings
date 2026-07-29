Status: ready-for-agent

# PRD: Deepen Scoring Service Responsibilities

## Problem Statement

The current scoring module owns several gameplay facts at once: pollution pressure, budget, purchase validation, reward-adjusted values, and direct UI updates. After the scene-singleton cleanup, callers no longer need to wire this module through the Inspector, but the module is still shallow in a different way: its interface exposes implementation-shaped concepts such as generic score names, budget UI mutation, and purchase confirmation flow.

From the user's perspective, this makes the game harder to evolve. A small change to the cleanup loop, shop economy, or pollution presentation can require reading unrelated code paths. The module is also named as if it only tracks score, while in practice it coordinates the resource phase of the game.

## Solution

Deepen the scoring module into a clearer runtime economy module with a small, stable interface around pollution and budget. Move presentation concerns behind a separate UI adapter or presenter module, and sharpen the domain language so callers talk about pollution, cleanup income, budget, and purchases instead of generic score.

This PRD assumes the already-implemented scene-singleton and event cleanup remains in place. It does not ask agents to reintroduce Inspector references for singleton scene modules or redesign the event model from scratch.

## User Stories

1. As a player, I want pollution to rise when trash appears, so that the pressure of the cleanup game is visible.
2. As a player, I want pollution to fall when trash is removed, so that cleanup feels immediately meaningful.
3. As a player, I want cleanup to grant budget, so that direct action feeds into the upgrade loop.
4. As a player, I want purchases to spend budget only when I can afford them, so that the shop has clear resource rules.
5. As a player, I want reward bonuses to modify trash pollution, trash income, shop costs, and awareness values consistently, so that bonuses feel reliable.
6. As a player, I want the pollution bar to reflect pollution state accurately, so that I can judge when I am close to losing.
7. As a player, I want the budget text to reflect current budget accurately, so that purchase decisions are clear.
8. As a designer, I want pollution terminology in the code to match the game concept, so that balancing work does not require translating from generic score names.
9. As a designer, I want the pollution cap to remain controlled by the game state module, so that loss threshold tuning remains in one place.
10. As a designer, I want budget and pollution values to be inspectable in Play Mode, so that balancing can be validated quickly.
11. As a developer, I want purchase validation to be exercised through one public module interface, so that tests cover real gameplay behavior.
12. As a developer, I want UI updates separated from economy state changes, so that logic can be tested without creating UI objects.
13. As a developer, I want event publishing to happen after state changes are complete, so that subscribers see consistent current values.
14. As a developer, I want cleanup income and pollution deltas to be stored per spawned trash when needed, so that later bonus changes do not corrupt removal math.
15. As a developer, I want failed purchases to leave budget and awareness unchanged, so that shop behavior is deterministic.
16. As a developer, I want successful purchases to emit one clear confirmation event, so that awareness growth remains decoupled from purchase validation.
17. As a developer, I want the module name and public properties to communicate pollution and budget ownership, so that future agents do not add unrelated responsibilities.
18. As a developer, I want legacy score aliases either removed or marked as compatibility only, so that new work uses the correct vocabulary.
19. As a QA tester, I want a narrow checklist for pollution, budget, purchase, and UI behavior, so that regressions can be caught without inspecting implementation details.
20. As a future agent, I want documentation to state which module owns economy state and which module owns presentation, so that later refactors do not reintroduce shallow coupling.

## Implementation Decisions

- Keep scene-singleton access for the existing scene modules. That work is already done and is not part of this PRD.
- Keep event-based communication for world facts and gameplay notifications, such as trash added, trash removed, purchase requested, purchase confirmed, pollution changed, and budget changed.
- Treat the current scoring module as the owner of runtime pollution and budget state until a better domain name is chosen.
- Rename internal fields and public vocabulary from generic score terms toward pollution terms where compatibility allows.
- Preserve compatibility aliases only when existing scene wiring or other modules still need them.
- Extract direct progress bar and budget text mutation behind a small UI-facing module, adapter, or presenter.
- The runtime economy module should publish state changes; the UI-facing module should subscribe and render them.
- Purchase validation remains in the runtime economy module because it owns budget.
- Awareness growth remains outside the runtime economy module and continues to react to confirmed purchases.
- Reward-adjusted value calculations remain owned by the reward module; the economy module asks for final values when applying gameplay rules.
- Do not move lose-state ownership into the economy module. The game state module remains responsible for deciding when pollution causes loss.
- The external interface should support the main gameplay loop directly: trash appeared, trash removed, purchase requested, pollution cap set, current budget read, current pollution read.
- Avoid adding abstract interfaces unless there are at least two real adapters or a concrete testing seam that gives leverage.
- Update the game context map after implementation so the domain ownership is clear.

## Testing Decisions

- Good tests should exercise external behavior through the module interface and gameplay events, not private helper methods.
- Test pollution behavior: adding trash increases pollution by the final reward-adjusted amount.
- Test removal behavior: removing registered trash subtracts the same pollution amount that was added.
- Test budget behavior: removing trash increases budget by final reward-adjusted income.
- Test purchase behavior: failed purchases do not change budget and do not emit purchase confirmation.
- Test purchase behavior: successful purchases subtract final reward-adjusted cost and emit purchase confirmation exactly once.
- Test pollution cap behavior: setting a cap clamps current pollution and emits a consistent pollution changed value.
- Test UI adapter behavior separately with fake values or event invocations, so UI rendering does not require testing the full economy loop.
- If Unity Edit Mode tests are available, prefer focused tests around the runtime economy module and a minimal UI adapter.
- If no Unity test harness exists yet, add a small Edit Mode test assembly as part of the implementation only if it does not require broad project restructuring.

## Out of Scope

- Replacing scene singleton access with dependency injection.
- Reworking spawn pooling, click input, or reward selection flow.
- Moving loss ownership back into the scoring/economy module.
- Redesigning all static events across the project.
- Rebalancing trash values, shop costs, awareness tiers, or reward definitions.
- Changing scene or prefab content beyond Inspector cleanup required by the implementation.
- Creating a new save/load system for budget or pollution.

## Further Notes

The main architectural goal is depth: a small interface should hide the messy ordering of trash appearance, cleanup, reward modifiers, budget changes, purchase confirmation, and UI updates. This should increase locality for future changes to the resource phase of the game while preserving the existing jam-scale runtime model.
