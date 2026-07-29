Status: ready-for-agent

# PRD: GameManager-Driven Lose Flow

## Problem Statement

The current gameplay code mixes pollution tracking, budget tracking, UI updates, and the lose condition inside `ScoringService`. This makes the lose flow harder to reason about, harder to extend, and tied to a pollution limit that is currently derived from spawn setup rather than owned explicitly by the main game-state authority.

The project needs a small, jam-friendly architecture where:

- gameplay metrics stay in focused services,
- the game state has a single owner,
- the pollution loss threshold is set explicitly in the inspector,
- the lose flow can stop gameplay cleanly,
- UI and menu flow can react to loss without being hardcoded into the same class.

## Solution

Introduce a small `GameManager` as the owner of runtime game state and the pollution loss threshold. `ScoringService` will continue to own pollution and budget metrics, but it will no longer decide when the game is lost.

`GameManager` will:

- store the inspector-driven max pollution value,
- listen to pollution change events from `ScoringService`,
- switch the game state to `Lost` when pollution reaches the threshold,
- disable configured gameplay elements directly,
- toggle configured objects for lose-state presentation,
- emit a loss/state-change event so UI or flow systems can react separately.

This keeps the project small while separating runtime metrics from high-level game flow.

## User Stories

1. As a player, I want the game to end consistently when pollution reaches the limit, so that the lose condition feels reliable.
2. As a player, I want the lose threshold to be tuned directly in the inspector, so that the game can be balanced without depending on spawn internals.
3. As a player, I want gameplay to stop after losing, so that I do not keep clicking, spawning, or purchasing after the loss state is reached.
4. As a player, I want the loss UI flow to be able to change later, so that the game can evolve from a simple message to an overlay, restart prompt, or main menu return.
5. As a designer, I want pollution balance to be explicit, so that I can tune difficulty without editing code.
6. As a designer, I want the game-state owner to be obvious, so that future changes to lose flow do not require digging through unrelated services.
7. As a programmer, I want `ScoringService` to own runtime metrics only, so that score and budget logic stay small and understandable.
8. As a programmer, I want `GameManager` to listen to events rather than polling in `Update`, so that the flow stays lightweight and easy to trace.
9. As a programmer, I want the lose threshold to stop depending on the first trash prefab, so that multiple trash types do not silently distort game balance.
10. As a programmer, I want lose-state actions to be configured through simple serialized references, so that scene wiring remains jam-friendly.
11. As a programmer, I want UI to react through events, so that screen flow can evolve independently from state evaluation.
12. As a future agent, I want one clear owner of the game state, so that I can add win/lose/pause logic without guessing which class is responsible.
13. As a future agent, I want disable/toggle behavior to be visible in one place, so that I can inspect lose flow without opening many scene objects.
14. As a future agent, I want pollution events to expose current and max values, so that bars and other listeners can stay synchronized.
15. As a future agent, I want the project to keep small focused classes, so that implementation context stays compact.

## Implementation Decisions

- Add a dedicated `GameManager` as the owner of runtime game state.
- `GameManager` stores the pollution threshold as a serialized inspector value instead of deriving it from spawn configuration.
- `GameManager` listens to `ScoringService` pollution-change events rather than polling in `Update`.
- `GameManager` owns transitions like `Running -> Lost`.
- `GameManager` emits a state/loss event for external listeners.
- `GameManager` also directly disables configured gameplay components when the game is lost.
- `GameManager` can also toggle configured objects off/on for lose-state presentation.
- UI-specific behavior such as overlays, restart panels, or main-menu flow stays outside `GameManager`.
- `ScoringService` remains responsible for runtime metrics: current pollution, budget, purchase validation, and pollution bar updates.
- `ScoringService` no longer owns the lose decision.
- The pollution limit becomes an externally assigned runtime value, supplied by `GameManager`.
- The implementation should preserve current jam simplicity and inspector-driven workflows instead of introducing a larger framework.
- The design should avoid a "god object" manager; `GameManager` owns state and flow, not every piece of gameplay logic.

## Testing Decisions

- Good tests should validate externally visible behavior, not private implementation details.
- `GameManager` should be tested for:
  - switching to `Lost` when pollution reaches the configured threshold,
  - emitting the expected state/loss event once,
  - disabling configured gameplay behaviors,
  - toggling configured lose-state objects.
- `ScoringService` should be tested for:
  - changing pollution and budget correctly on trash spawn/despawn,
  - preserving purchase validation behavior,
  - emitting pollution updates with the configured max value.
- UI flow controllers are not the first testing priority for this change.
- If the repo later adds automated tests, prioritize behavior-first play mode or lightweight integration-style tests over tests that mirror private method structure.

## Out of Scope

- Generic bonus system rework.
- Awareness system redesign.
- Main menu scene loading implementation.
- Full restart flow implementation.
- Win-state design.
- Reworking inspector-driven scene wiring into a code-driven composition model.
- Prefab or scene cleanup outside the minimum hooks required by the new `GameManager`.

## Further Notes

- The project explicitly prefers small classes and low context overhead.
- Inspector-driven wiring is acceptable in this repo; the goal is not to eliminate it, only to make the critical game-state flow clearer.
- The lose threshold is intentionally manual for now because the project is still in a balancing-heavy jam phase.
