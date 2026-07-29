---
read_when: [recycling-patrol, patrol-area, patrol-cooldown, patrol-targeting]
avoid_when: [unrelated-shop-item, player-navigation-only]
primary_files: [Assets/Scripts/RecyclingPatrolService.cs, Assets/Scripts/RecyclingPatrolAgent.cs, Assets/Scripts/RecyclingPatrolDefinition.cs, Assets/Scripts/RecyclingPatrolTargetSelector.cs, Assets/Prefabs/Recycling Patrol/RecyclingPatrolPlaceholder.prefab, Assets/Prefabs/SHOP/ShopItemPrefab.prefab, Assets/Scenes/PROD_SCENE.unity]
tests: [Assets/Tests/EditMode/RecyclingPatrolCooldownTests.cs, Assets/Tests/EditMode/RecyclingPatrolEconomyTests.cs, Assets/Tests/EditMode/RecyclingPatrolTargetSelectorTests.cs]
code_maps: [docs/code-map/recycling-patrol.md]
---

# Recycling Patrol

## Ownership

`RecyclingPatrolService` owns purchase availability, the per-definition 20-second cooldown, shared target claims, scene references, and Patrol spawning. `RecyclingPatrolAgent` owns one spawned Patrol's NavMesh movement, target lock, pickup, five-second work window, and exit. `RecyclingPatrolDefinition` stores the shop economy and timing configuration.

## Purchase And Cooldown

- The default asset is `Assets/ScriptableObjects/REWARDS/Recycling Patrol.asset`: path `RecyclingPatrol`, level 1, cost 25, awareness 0, work duration 5 seconds, cooldown 20 seconds, and pickup multiplier 0.5.
- The item is in `DefaultRewardCatalog` but is not wired to an Act 1 event. A later progress-event choice must request `RewardPath.RecyclingPatrol`.
- Purchase requires active gameplay, no active cooldown, complete scene wiring, and at least one unclaimed, reachable trash inside `Patrol Area`.
- Cooldown starts at purchase and freezes while gameplay is inactive. Multiple Patrol instances may coexist after the shared shop cooldown expires.
- The shop card's dark horizontal fill shows cooldown remaining and reveals the card left-to-right. Its duration label belongs to the latest purchased Patrol and shows 5.0 seconds until first pickup begins.

## Target And Work Rules

- `Patrol Area` is a scene `Transform` with `BoxCollider`; only trash whose XZ position is inside the oriented box is eligible. A route may temporarily leave the box.
- Choose the eligible target with the shortest complete NavMesh path. Exclude player-collected and already-claimed trash.
- A target remains locked while valid. Retarget when it is removed, becomes player-collected, leaves the area, or loses its complete path.
- `TrashPickupProgressView` appears from target lock, stays at zero during travel, and fills during Patrol pickup. Manual player pickup does not show this view.
- Patrol pickup lasts `Trash.PickupTime * PickupDurationMultiplier`. The five-second work timer begins with the first pickup and continues even without a target.
- At expiry, finish an in-progress pickup, choose no new target, and drive to the configured off-screen exit.

## Economy

Patrol removal subtracts registered current pollution and still publishes the legacy trash-removed event for audio and lifecycle consumers. It does not add budget, publish `GoldGathered`, or publish `TrashIncomeAwarded`, so Money Fly VFX is not created. Player and existing passive-bonus removals retain normal income behavior.

## Unity Wiring

`PROD_SCENE` owns one `RecyclingPatrolService` under `_GameControllers`, with `SpawnService`, `Assets/Prefabs/Recycling Patrol/RecyclingPatrolPlaceholder.prefab`, `Patrol Area`, and off-screen entry/exit references assigned. The area is derived from the configured spawn-area collider bounds, while entry and exit sample to the baked NavMesh outside the Beach camera viewport.

`Assets/Prefabs/SHOP/ShopItemPrefab.prefab` owns the non-raycast cooldown overlay and left-side TMP duration label assigned to `RewardItemView`. No custom editors exist for the Patrol components or `RewardItemView`.
