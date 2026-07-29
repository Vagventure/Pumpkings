# Plastic Seeds Code Map

Thin router from task intent to one detailed code map. Read only the selected map, then verify its referenced source files. Use [CONTEXT-MAP.md](CONTEXT-MAP.md) for domain behavior.

| Task signal | Detailed code map |
| --- | --- |
| Player movement, clicking, NavMesh, trash pickup | [Player navigation](docs/code-map/player-navigation.md) |
| Trash spawning, pooling, limits, areas, triggers, wind or directional bursts | [Spawning and pooling](docs/code-map/spawning-pooling.md) |
| Pollution, budget, awareness, purchases, pause, loss | [Economy and game state](docs/code-map/economy-game-state.md) |
| Shop unlocks, shop cards, tutorial pointer | [Shop and tutorial](docs/code-map/shop-tutorial.md) |
| Recycling Patrol, Patrol Area, target claims, cooldown | [Recycling Patrol](docs/code-map/recycling-patrol.md) |
| Progress thresholds, rewards, dialogue events | [Progress events and dialogue](docs/code-map/progress-events.md) |
| Stage sequence, Cinemachine camera transitions, stage-root activation | [Stage transitions](docs/code-map/stage-transitions.md) |
| Reward UI, bonus HUD, reveal, layout, Feel | [UI presentation](docs/code-map/ui-presentation.md) |
| SFX, audio playback, music states | [Audio and music](docs/code-map/audio-music.md) |
| Tests, custom editors, scenes, project configuration | [Testing and Unity](docs/code-map/testing-unity.md) |

## Path Conventions

- Runtime: `Assets/Scripts/`
- Custom editors: `Assets/Scripts/Editor/`
- EditMode tests: `Assets/Tests/EditMode/`
- Production scene: `Assets/Scenes/PROD_SCENE.unity`

## Maintenance Contract

After a task, review this router and the selected detailed map. Edit them only when routing, ownership, event flow, important paths, custom editors, or test locations changed. Do not touch maps merely to record that a task occurred or to refresh timestamps.
