# Plastic Seeds Context Map

Intent-based router for additional project context. Read [CONTEXT.md](CONTEXT.md) first, then choose only the rows relevant to the task. Use [CODEMAP.md](CODEMAP.md) for source locations.

Every focused document under `docs/contexts/` begins with routing frontmatter (`read_when`, `avoid_when`, `primary_files`, `tests`, and `code_maps`). Agents and Spark may inspect that metadata to choose a route without loading the document body.

## Task Routes

| Task signal | Read this focused context | Add these maps when needed |
| --- | --- | --- |
| Core gameplay loop or overall domain | [Core domain](docs/contexts/core-domain.md) | [Gameplay loop](docs/context-map/gameplay-loop.md), [Major systems](docs/context-map/systems.md) |
| Pollution, budget, income, purchases or scoring | [Economy and scoring](docs/contexts/economy-scoring.md) | [Core domain](docs/contexts/core-domain.md), [Communication style](docs/context-map/communication-style.md) |
| Player movement, clicking, NavMesh, trash pickup animation | [Player navigation feel](docs/contexts/player-navigation-feel.md) | [Current assumptions](docs/context-map/assumptions.md), [Prefab and scene checks](docs/context-map/prefab-scene-checks.md) |
| Trash spawning, pooling, spawn limits, areas, triggers or wind gusts | [Spawning and pooling](docs/contexts/spawning-pooling.md) | [Communication style](docs/context-map/communication-style.md), [Prefab and scene checks](docs/context-map/prefab-scene-checks.md) |
| Progress thresholds, dialogue events, choices or rewards | [Progress events](docs/contexts/progress-events.md) | [Progress event decisions](docs/context-map/progress-events.md), [Communication style](docs/context-map/communication-style.md) |
| Shop unlocks, shop purchases or shop item data | [Shop system](docs/contexts/shop-system.md) | [Reward item view](docs/contexts/reward-item-view.md), [Economy and scoring](docs/contexts/economy-scoring.md) |
| Recycling Patrol, patrol targeting, Patrol Area or patrol cooldown | [Recycling Patrol](docs/contexts/recycling-patrol.md) | [Shop system](docs/contexts/shop-system.md), [Economy and scoring](docs/contexts/economy-scoring.md), [Player navigation feel](docs/contexts/player-navigation-feel.md) |
| Reward cards or active bonus HUD | [Reward item view](docs/contexts/reward-item-view.md) | [Major systems](docs/context-map/systems.md), [Prefab and scene checks](docs/context-map/prefab-scene-checks.md) |
| Tutorial pointer, first bottle or first unlocked shop item | [Tutorial](docs/contexts/tutorial.md) | [Spawning and pooling](docs/contexts/spawning-pooling.md), [Shop system](docs/contexts/shop-system.md), [Progress events](docs/contexts/progress-events.md) |
| Game state, pause/resume, pollution loss or lose UI | [Game state and loss](docs/contexts/game-state-loss.md) | [Economy and scoring](docs/contexts/economy-scoring.md), [Prefab and scene checks](docs/context-map/prefab-scene-checks.md) |
| Stage sequence, Cinemachine camera transition or stage-root transition | [Stage transitions](docs/contexts/stage-transitions.md) | [Progress events](docs/contexts/progress-events.md), [Prefab and scene checks](docs/context-map/prefab-scene-checks.md) |
| UI entrance, typewriter reveal, Feel feedback | [Reveal text](docs/contexts/reveal-text.md) | [Feel / SFX](docs/contexts/feel-sfx.md), [Prefab and scene checks](docs/context-map/prefab-scene-checks.md) |
| Audio playback, semantic SFX routing or music states | [Audio and music](docs/contexts/audio-music.md) | [Feel / SFX](docs/contexts/feel-sfx.md), [Communication style](docs/context-map/communication-style.md) |
| EditMode tests, test patterns or regression coverage | [EditMode testing](docs/contexts/editmode-testing.md) | Relevant focused context for the system under test |
| Architecture, ownership or cross-system event flow | [Architecture overview](docs/contexts/architecture-overview.md) | [Major systems](docs/context-map/systems.md), [Communication style](docs/context-map/communication-style.md) |
| Terminology or domain naming | [Glossary](docs/contexts/glossary.md) | [Core domain](docs/contexts/core-domain.md) |
| Scene/prefab/Inspector wiring | Relevant focused context above | [Prefab and scene checks](docs/context-map/prefab-scene-checks.md), [Current assumptions](docs/context-map/assumptions.md) |

## Decision Records

- Read ADRs under [`docs/adr/`](docs/adr/) only when they touch the task area.
- Current ADR: [Active bonus hotbar and operational panel](docs/adr/001-active-bonus-hotbar-and-operational-panel.md).

## Retrieval Rules

- Start with one route and expand only when a dependency crosses into another system.
- Prefer a focused context over a broad project scan.
- For implementation tasks, pair the selected context route with the matching source/test row in `CODEMAP.md`.
- If context conflicts with code or live Unity state, verify the implementation and update the stale route or document after the task.
