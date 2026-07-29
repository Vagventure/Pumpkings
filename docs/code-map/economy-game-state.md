# Economy And Game State Code Map

Contexts: [Economy and scoring](../contexts/economy-scoring.md), [Game state and loss](../contexts/game-state-loss.md)

## Economy Working Set

- `Assets/Scripts/MoneyFlyVfxController.cs` — cosmetic world-to-overlay income flight driven by `ScoringService.TrashIncomeAwarded`.

- `Assets/Scripts/ScoringService.cs` — current pollution, maximum pollution, budget, purchase validation, UI publication, `GoldGathered`, and `ThreatProduced`.
- `Assets/Scripts/Trash.cs` — base pollution and income.
- `Assets/Scripts/RewardManager.cs` — final cost, pollution, income, and awareness modifiers.
- `Assets/Scripts/AwarenessManager.cs` — awareness state/UI and `AwarenessGained`.
- `Assets/Scripts/ProgressBarController.cs` — normalized bar display.
- `Assets/Scripts/RewardItemView.cs` and `ShopItemDefinition.cs` — purchase input and shop data.
- `Assets/Scripts/TrashRemovalSource.cs` and `RecyclingPatrolService.cs` — source-aware Patrol removal and purchase availability; Patrol removal reduces pollution without income.

## Game State Working Set

- `Assets/Scripts/GameManager.cs` — `Running`/`Lost`, pause/resume, gameplay-active gate, configured loss consequences.
- `Assets/Scenes/PROD_SCENE.unity` — authoritative wiring for pause, disable, and lose UI arrays; inspect through Unity MCP when relevant.

## Main Flow

Pollution and awareness values remain immediate while their shared `ProgressBarController` interpolates only the rendered fill using unscaled time.

`SpawnService.TrashAdded` -> pollution/threat produced -> possible loss. `SpawnService.TrashRemoved` -> pollution decrease/budget/gold gathered. Purchase confirmation -> awareness. `GameManager` alone decides loss from current pollution.

## Tests

No focused EditMode suite currently covers scoring, purchases, loss, or pause/resume.
