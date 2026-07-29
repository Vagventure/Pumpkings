---
read_when: [shop, shop-unlock, shop-purchase, shop-item]
avoid_when: [reward-without-shop, economy-without-purchase]
primary_files: [Assets/Scripts/ShopController.cs, Assets/Scripts/ShopItemDefinition.cs, Assets/Scripts/RewardItemView.cs, Assets/Scripts/ScoringService.cs]
tests: [Assets/Tests/EditMode/TutorialSupportTests.cs, Assets/Tests/EditMode/RecyclingPatrolEconomyTests.cs]
code_maps: [docs/code-map/shop-tutorial.md]
---

# Shop System Context

## Ownership And Data

`ShopController` owns the runtime list of unlocked shop definitions and creates their views. `ShopItemDefinition : RewardItem` stores non-negative base cost and awareness value. `RecyclingPatrolDefinition` specializes it with work duration, cooldown, and pickup-speed configuration. `RewardItemView` is the shared runtime-configured view for shop and reward cards.

`ScoringService` owns purchase validation and budget deduction. `AwarenessManager` applies awareness after a confirmed purchase. `RewardManager` can unlock shop items as rewards and applies cost/awareness modifiers.

## Unlock Flow

1. A reward resolves to `ShopItemDefinition`.
2. `RewardManager` asks `ShopController.UnlockShopItem` to apply it.
3. Null definitions, duplicates, or a missing view prefab are rejected.
4. `ShopController` instantiates `RewardItemView` under the configured parent, calls `Configure(definition)`, records the definition, and optionally starts `LayoutItemSlideIn`.
5. `ShopController.ItemUnlocked(view)` publishes the created view for tutorial/presentation consumers.

Unlocked identity is ScriptableObject reference identity. Unlock state is runtime-only.

## Purchase Flow

1. `RewardItemView.Clicked` publishes the configured view.
2. `ScoringService` accepts it only when `ShopDefinition` is present and gameplay is active.
3. Runtime-specific availability is checked; Recycling Patrol requires a reachable, unclaimed trash in its Patrol Area and no active cooldown.
4. Final cost is calculated through `RewardManager`.
5. A rejected purchase plays local rejected feedback and semantic failure SFX.
6. A successful purchase deducts budget, plays accepted feedback, and publishes `ItemPurchaseConfirmed`.
7. `AwarenessManager` adds the final awareness value; the Patrol asset defaults to zero.

## View Invariants

- Shop and card-style rewards use `RewardItemView`; active bonus HUD slots use `ActiveBonusSlotView`.
- A view prefab must not serialize a reward asset. Runtime owners call `Configure(RewardItem)`.
- `ConfigureDisplay` is the external-confirmation presentation mode and should not behave as a normal purchase click.
- Displayed cost and awareness values must reflect active `RewardManager` bonuses.
- `ShopCheaper` targets either all shop items (`RewardPath.None`) or one configured reward path. The existing Printing Company bonus targets Posters only.
- Purchase-mode views react to budget and cost-bonus changes: unaffordable items are dimmed, while affordable items show the prefab's authored affordability border.
- Recycling Patrol views additionally disable purchase without an eligible target, show a left-to-right cooldown overlay, and show the latest Patrol's work time.

## Unity Setup

- `ShopController` needs a `RewardItemView` prefab; the items parent is optional and falls back to the controller transform.
- The optional unlock VFX target falls back to the items parent when it is a `RectTransform`.
- The view prefab assigns its button and desired TMP/Image references. `LayoutItemSlideIn` and `RewardItemFeelFeedback` are optional presentation modules.
- Shop views use their root `CanvasGroup` for the unaffordable dim state, isolate the cost label from that dimming, and assign an `Image` as the affordability border; display-only reward cards do not use this styling.
- `RewardManager` needs its shop controller and reward catalog references.
- No custom editor exists for `ShopController`, `RewardItemView`, or `ShopItemDefinition`; `RewardManagerEditor` manually exposes RewardManager fields.

## Verification

`TutorialSupportTests.UnlockShopItem_RaisesCreatedConfiguredView` covers successful instantiation, configuration, and `ItemUnlocked`. It does not test `TutorialController` itself.

Missing coverage includes null/duplicate/missing-prefab rejection, parent fallback, slide-in, purchase success/failure, gameplay gating, bonus-adjusted display values, and resulting awareness.

## Important Files

- `Assets/Scripts/ShopController.cs`
- `Assets/Scripts/ShopItemDefinition.cs`
- `Assets/Scripts/RewardItemView.cs`
- `Assets/Scripts/ScoringService.cs`
- `Assets/Scripts/AwarenessManager.cs`
- `Assets/Scripts/RewardManager.cs`
- `Assets/Scripts/ShopItemRewardPresenter.cs`
- `Assets/Tests/EditMode/TutorialSupportTests.cs`
