# Shop And Tutorial Code Map

Contexts: [Shop system](../contexts/shop-system.md), [Tutorial](../contexts/tutorial.md)

## Shop Working Set

- `Assets/Scripts/ShopController.cs` — unlocked definitions, view creation, and instance `ItemUnlocked` event.
- `Assets/Scripts/ShopItemDefinition.cs` — cost and awareness data.
- `Assets/Scripts/RewardItemView.cs` — runtime-configured shop/reward view and static click event.
- `Assets/Scripts/ShopItemRewardPresenter.cs` — shop unlock reward presenter.
- `Assets/Scripts/LayoutItemSlideIn.cs` — optional unlock entrance.
- `Assets/Scripts/ScoringService.cs` — purchase validation.
- `Assets/Scripts/RecyclingPatrolDefinition.cs`, `RecyclingPatrolService.cs`, and `RewardItemView.cs` — Patrol shop data, runtime availability/cooldown, and card state.

## Tutorial Working Set

- `Assets/Scripts/TutorialController.cs` — first-bottle and first-unlocked-shop-item pointer phases.
- `Assets/Scripts/SpawnService.cs` — per-type spawn blocking.
- `Assets/Scripts/GameManager.cs` and `RewardManager.cs` — gameplay/progress-flow gating.

## Tests

- `Assets/Tests/EditMode/TutorialSupportTests.cs` covers the spawn-blocking and shop-unlock interfaces, not the pointer controller itself.
- View animation behavior also uses `Assets/Tests/EditMode/LayoutItemSlideInTests.cs`.

## Cross-System Routes

For purchase economics use [Economy and game state](economy-game-state.md). For card presentation use [UI presentation](ui-presentation.md). For progress-event gating use [Progress events and dialogue](progress-events.md).
