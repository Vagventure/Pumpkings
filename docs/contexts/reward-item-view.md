---
read_when: [reward-card, shop-card, bonus-hud, reward-item-ui]
avoid_when: [reward-logic-without-view, unrelated-ui]
primary_files: [Assets/Scripts/RewardItemView.cs, Assets/Scripts/ActiveBonusSlotView.cs, Assets/Scripts/HotBarController.cs, Assets/Scripts/LayoutItemSlideIn.cs, Assets/Scripts/RewardItemFeelFeedback.cs]
tests: [Assets/Tests/EditMode/LayoutItemSlideInTests.cs, Assets/Tests/EditMode/RewardItemFeelFeedbackTests.cs]
code_maps: [docs/code-map/ui-presentation.md]
---

# Reward Item View Context

## Decision

Reward item UI prefabs use one component: `RewardItemView`.

Do not create separate prefab scripts for shop item views and card-style bonus item views. A shop item, a card-style bonus item, and any other card-style reward item display are the same UI shape: title, subtitle, description, icon, optional cost, effect icon, effect value, and an optional button.

Compact active bonus HUD slots are a separate UI shape owned by `ActiveBonusSlotView`.

## Data Model

- `RewardItem` is the shared ScriptableObject base for reward data.
- `ShopItemDefinition : RewardItem` stores shop-specific gameplay values such as cost and awareness value.
- `BonusDefinition : RewardItem` stores bonus-specific gameplay values such as effect type, target, flat value, percent value, and interval.

`RewardItem` is data, not a prefab component. It exists as an asset.

## Prefab Model

`RewardItemView` is the only data-binding MonoBehaviour that should be added to shop item or card-style bonus item prefabs. Optional presentation modules may sit beside it without owning reward data.

`RewardItemView` owns:

- `Button`
- `Title Text`
- `Subtitle Text`
- `Description Text`
- `Icon Display`
- Optional `Icon Dim Overlay`
- Optional `Cost Text`
- Optional `Effect Icon Display`
- Optional `Effect Value Text`
- Optional `Cooldown Overlay` for Recycling Patrol shop cards
- Optional `Duration Text` for the latest Recycling Patrol instance

`ActiveBonusSlotView` owns compact active bonus HUD slot references:

- `Effect Icon Display`
- `Value Text`
- `Cooldown Fill Image`

`RewardItemView` does not serialize a `RewardItem` field. The reward data is runtime-only and must be supplied by calling `Configure(RewardItem item)`.

Optional presentation modules are:

- `LayoutItemSlideIn`, which keeps the layout root stationary and animates a configured child RectTransform.
- `RewardItemFeelFeedback`, which owns optional accepted/rejected Feel players and is independent of the configured reward subtype.

## Runtime Flow

- `ShopController` instantiates a `RewardItemView` prefab and calls `Configure(shopItemDefinition)`.
- `HotBarController` instantiates an `ActiveBonusSlotView` prefab for active bonuses.
- `ScoringService` listens to `RewardItemView.Clicked` and treats the click as a purchase only when the configured reward is a `ShopItemDefinition`.
- Recycling Patrol purchase-mode views poll cached runtime availability, display a horizontal cooldown overlay whose dark fill remains on the right, and show the latest purchased Patrol's five-second work timer.
- Successful and rejected interactions ask the clicked `RewardItemView` to play its local Feel feedback. Existing global SFX routing remains separate.
- When a reward card is accepted from an external Continue button, the resolver waits for the view's accepted feedback before closing the card and completing reward application.
- Active bonuses are not purchased through the view; they are displayed by `HotBarController` after `RewardManager` activates them.

## Inspector Rules

On reward item UI prefabs, assign only view references:

- Button, if the view is clickable.
- Title Text.
- Subtitle Text, if present.
- Description Text.
- Icon Display.
- Icon Dim Overlay, if the view displays shop cost over the icon.
- Cost Text, if the view displays shop items.
- Effect Icon Display.
- Effect Value Text.
- Cooldown Overlay, authored as a full-card dark `Image` for Recycling Patrol.
- Duration Text, positioned on the left side of the shop card.

On active bonus slot prefabs, assign only view references:

- Effect Icon Display.
- Value Text.
- Cooldown Fill Image.

For animated layout entries, keep the view object as the direct child of the Layout Group, then assign a visual child as `LayoutItemSlideIn.Animated Element`. Assign an optional `CanvasGroup` to block interaction during the slide.

Do not assign a reward asset on the prefab. The asset is chosen by the event or controller at runtime.

## Removed Shapes

These prefab scripts are intentionally removed and should not be recreated:

- `ShopItem`
- `BonusItem`
- `RewardItemPresentation`

Shop and card-style bonus item views should not diverge again unless there is a real gameplay or UI behavior that cannot be expressed by `RewardItemView`.
