---
read_when: [game-state, pause, resume, loss, pollution-limit]
avoid_when: [economy-without-state-change, ui-only]
primary_files: [Assets/Scripts/GameManager.cs, Assets/Scripts/ScoringService.cs, Assets/Scripts/RewardManager.cs]
tests: []
code_maps: [docs/code-map/economy-game-state.md]
---

# Game State And Loss Context

## Ownership

`GameManager` owns high-level runtime state, pause state, and scene-configured consequences of loss. `ScoringService` owns the numeric pollution value; `GameManager` supplies the authoritative maximum and listens to `OnPollutionChanged`.

The state model currently contains `Running` and `Lost`. There is no runtime restart or reset transition.

## Interface

- `CurrentState`, `MaxPollution`, `IsPaused`, and `IsGameplayActive` expose state.
- `PauseGame()` and `ResumeGame()` control reversible gameplay pause.
- `GameStateChanged(GameState)` and `GameLost` publish the one-way loss transition.

`IsGameplayActive` is true only while state is `Running` and the game is not paused. Input, purchasing, tutorial presentation, and reward flow use this gate.

## Loss Flow

1. `GameManager` clamps its configured maximum and pushes it to `ScoringService` during initialization.
2. While running, `OnPollutionChanged` at or above that maximum enters `Lost` once.
3. Loss clears pause bookkeeping, disables configured gameplay behaviours, applies hide/show object state, then publishes state events.

Loss uses current pollution only. Cumulative threat produced must not trigger loss directly.

## Pause Flow

Pause is available only while running and is idempotent. `GameManager` records the previous enabled state of every `pauseOnGamePause` behaviour, disables it, and restores the exact previous state on resume.

Pause does not change `Time.timeScale`; systems not included in the configured list continue running. Loss permanently disables both `disableOnLose` and `pauseOnGamePause` entries.

## Unity Setup

- Set `Max Pollution` on the single scene `GameManager`.
- Put permanently stopped gameplay behaviours in `disableOnLose`.
- Put reversibly paused behaviours in `pauseOnGamePause`.
- Configure `hideOnLose` and `showOnLose` for running/lost UI.
- Verify null entries and verify the intended loss panel in live Unity state. At the latest inspection, production loss UI wiring was incomplete, while `SpawnService` was the only verified pause entry.
- No custom editor currently exists for `GameManager`.

## Verification

There are no focused EditMode tests for loss or pause. Missing coverage includes below/at-threshold transitions, single event emission, current pollution versus threat produced, pause idempotence, exact behaviour restoration, loss while paused, null wiring entries, initial visibility, and initialization order with `ScoringService`.

## Important Files

- `Assets/Scripts/GameManager.cs`
- `Assets/Scripts/ScoringService.cs`
- `Assets/Scripts/RewardManager.cs`
- `Assets/Scenes/PROD_SCENE.unity`
