# Recycling Patrol Code Map

Context: [Recycling Patrol](../contexts/recycling-patrol.md)

## Runtime

- `Assets/Scripts/RecyclingPatrolDefinition.cs` — shop data for work duration, cooldown, and pickup multiplier.
- `Assets/Scripts/RecyclingPatrolService.cs` — scene wiring, availability, cooldowns, shared claims, and agent spawning.
- `Assets/Scripts/RecyclingPatrolAgent.cs` — per-instance NavMesh movement, target lifecycle, pickup, work timer, and exit.
- `Assets/Scripts/RecyclingPatrolTargetSelector.cs` — public area/claim/path-length target selection seam.
- `Assets/Scripts/RecyclingPatrolNavigation.cs` — shared complete-path calculation used by purchase availability and live agents.
- `Assets/Scripts/RecyclingPatrolCooldown.cs` — public paused linear cooldown seam.
- `Assets/Scripts/TrashRemovalSource.cs`, `SpawnService.cs`, and `ScoringService.cs` — source-aware removal and Patrol's no-income rule.
- `Assets/Scripts/Trash.cs` and `TrashPickupProgressView.cs` — target marker and pickup progress.
- `Assets/Scripts/RewardItemView.cs` — card availability, cooldown overlay, and latest-Patrol duration text.

## Data And Editor

- `Assets/ScriptableObjects/REWARDS/Recycling Patrol.asset` — configured level-1 unlock.
- `Assets/ScriptableObjects/Catalogs/DefaultRewardCatalog.asset` — catalog membership.
- `Assets/Prefabs/Recycling Patrol/RecyclingPatrolPlaceholder.prefab` — 2D sprite placeholder with `NavMeshAgent` and `RecyclingPatrolAgent`.
- `Assets/Prefabs/SHOP/ShopItemPrefab.prefab` — cooldown overlay and latest-Patrol duration label wiring.
- `Assets/Scenes/PROD_SCENE.unity` — `RecyclingPatrolService`, Patrol Area, and off-screen NavMesh entry/exit wiring.
- `Assets/Scripts/BonusDefinition.cs`, `RewardManager.cs`, and `Editor/BonusDefinitionEditor.cs` — `ShopCheaper` targeting by `RewardPath` (`None` means all).

## Tests

- `Assets/Tests/EditMode/RecyclingPatrolCooldownTests.cs`
- `Assets/Tests/EditMode/RecyclingPatrolEconomyTests.cs`
- `Assets/Tests/EditMode/RecyclingPatrolTargetSelectorTests.cs`

NavMesh travel, pickup coroutines, target VFX, multiple simultaneous agents, and shop-card rendering require live Play Mode verification when their wiring or timing changes.
