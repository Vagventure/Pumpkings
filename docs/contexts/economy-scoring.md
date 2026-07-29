---
read_when: [pollution, budget, income, purchase, scoring]
avoid_when: [ui-only, spawn-placement]
primary_files: [Assets/Scripts/ScoringService.cs, Assets/Scripts/Trash.cs, Assets/Scripts/AwarenessManager.cs, Assets/Scripts/ProgressTracker.cs]
tests: [Assets/Tests/EditMode/RecyclingPatrolEconomyTests.cs]
code_maps: [docs/code-map/economy-game-state.md]
---

# Economy And Scoring Context

## Ownership

`ScoringService` owns current pollution, maximum pollution, spendable budget, purchase validation, and the UI representations of pollution and budget. It records the post-bonus pollution assigned to each active `Trash`, so later removal subtracts exactly the value that was added at spawn time.

Related ownership:

- `Trash` provides base pollution and income values.
- `RewardManager` calculates final cost, awareness, pollution, and income after bonuses.
- `GameManager` owns the loss decision and supplies the authoritative pollution maximum.
- `ProgressTracker` consumes gross `GoldGathered` and `ThreatProduced` deltas.
- `AwarenessManager` consumes confirmed shop purchases.

## Vocabulary And Invariants

- `current pollution` rises on spawn, falls on removal, is clamped to the configured maximum, and alone drives loss.
- `threat produced` is the gross post-bonus pollution created by spawns. Removing trash does not reduce this cumulative progress metric.
- `budget` is the current spendable balance.
- `gold gathered` is gross post-bonus income earned from trash. Purchases reduce budget but not gold gathered.
- These values are runtime state and reset with the scene/run.
- Authored `Trash.Score` and `Trash.Income` should be non-negative; the current `Trash` validation does not enforce that invariant.

## Event Interface

`ScoringService` subscribes to `SpawnService.TrashAdded`, source-aware `SpawnService.TrashRemovedWithSource`, and `RewardItemView.Clicked`. The legacy one-argument `TrashRemoved` event remains for audio, tutorial, wind, and other lifecycle consumers.

It publishes:

- `OnPollutionChanged(current, max)` as the canonical current-pollution event;
- `OnBudgetChanged(budget)`;
- `TrashIncomeAwarded(trash, income)` after confirmed removal, for source-aware cosmetic income presentation;
- `ThreatProduced(delta)` and `GoldGathered(delta)` for progress tracking;
- `ItemPurchaseConfirmed(view)` after budget deduction;
- semantic purchase-success and purchase-failure SFX events.

`OnScoreChanged` remains a legacy alias for current-pollution consumers.

## Main Flows

Spawn: calculate post-bonus pollution, register it for that trash, add and clamp current pollution, publish threat produced, then publish pollution state.

Removal: always subtract the registered spawn value. Player and existing passive-bonus removal then calculate post-bonus income, add it to budget, and publish budget, gold gathered, and source-aware income presentation. `RecyclingPatrol` removal stops after pollution publication and grants no income.

`MoneyFlyVfxController` consumes `TrashIncomeAwarded`, converts the removed trash world position into overlay-canvas space, and animates a configured icon to the budget target. The icon keeps identity local rotation, unit scale, and zero canvas depth throughout the flight. Budget changes immediately; the flight is presentation only.

Purchase: accept only a `RewardItemView` configured with `ShopItemDefinition`; ignore purchasing while gameplay is inactive; calculate final cost; reject insufficient budget; otherwise deduct budget, play accepted feedback, and publish purchase confirmation. `AwarenessManager` then applies post-bonus awareness.

## Unity Setup

- Scene requires one active `ScoringService`.
- Assign the pollution bar object with `ProgressBarController` when the bar is displayed.
- Assign optional TMP fields for budget, current threat, and maximum threat.
- `GameManager.MaxPollution` is authoritative and is pushed into `ScoringService` during initialization.
- No custom editor currently exists for `ScoringService` or `ProgressBarController`.

## Verification

`RecyclingPatrolEconomyTests` covers the public spawn/removal seam for player income versus Patrol no-income behavior and path-targeted shop discounts. Remaining gaps include clamping, bonus rounding, invalid and insufficient purchases, inactive gameplay, UI updates, and event ordering.

## Important Files

- `Assets/Scripts/ScoringService.cs`
- `Assets/Scripts/Trash.cs`
- `Assets/Scripts/RewardManager.cs`
- `Assets/Scripts/AwarenessManager.cs`
- `Assets/Scripts/ProgressTracker.cs`
- `Assets/Scripts/ProgressBarController.cs`
- `Assets/Scripts/MoneyFlyVfxController.cs`
