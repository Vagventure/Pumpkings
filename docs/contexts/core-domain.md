---
read_when: [gameplay-loop, domain-behavior, cross-system-feature]
avoid_when: [localized-implementation, tooling-only]
primary_files: [Assets/Scripts/SpawnService.cs, Assets/Scripts/ScoringService.cs, Assets/Scripts/RewardManager.cs]
tests: []
code_maps: [CODEMAP.md]
---

# Core Domain Context

Plastic Seeds is a short 3D point-and-click cleanup game. The player moves a 3D character through the scene, removes trash, reduces pollution pressure, earns budget, buys awareness-producing shop items, and reaches milestone events with narrative and optional rewards.

## Main Loop

1. Trash appears in the scene.
2. Current pollution rises.
3. Player clicks walkable ground or trash.
4. Player character walks via NavMesh.
5. Trash pickup removes trash after approach and pickup timing.
6. Current pollution falls and budget rises.
7. Player spends budget on shop items.
8. Awareness rises.
9. Progress events interrupt the run with narrative and optional reward choices.

## Scope Boundaries

- Runtime totals reset with the scene/run.
- No cross-session persistence is needed for this short browser game.
- Loss condition stays based on current pollution.
- Per-trash-type progress totals are out of scope.
- New reward effect types are out of scope for the progress event feature.
