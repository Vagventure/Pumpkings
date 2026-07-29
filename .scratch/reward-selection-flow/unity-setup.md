# Unity setup: reward selection flow

## Shop Item

- `ShopItem.Purchase Button`: assign the local `Button` component.
- `Button.Target Graphic`: assign a child `Image` with `Raycast Target` enabled.
- `Button.Interactable`: enabled.
- `Button.OnClick`: can stay empty. `ShopItem` subscribes to the button click at runtime.
- `Cost`: set to the intended price.
- `Awareness Value`: set to the awareness gained after a successful purchase.

For quick testing, either set `ScoringService.Budget` above the shop item cost, or set the item cost to `0`.

## ScoringService

- `Game Manager`: assign the scene `GameManager`.
- `Reward Manager`: assign the scene `RewardManager`.
- `Pollution Bar`: assign the pollution fill/progress object.
- `Budget Representation`: assign the TMP text showing funds.
- `Budget`: set a test value high enough to buy the shop item.

## AwarenessManager

- `Reward Manager`: assign the scene `RewardManager`.
- `Awareness Bar`: assign the awareness fill/progress object.
- `Awareness Tiers`: configure cumulative tier targets.

Example:

```text
Tier 1 -> Required Awareness Points 100
Tier 2 -> Required Awareness Points 500
```

The fill is cumulative against the current target. Example: `300 / 500 = 60%`.

## RewardManager

- `Game Manager`: assign the scene `GameManager`.
- `Reward Selection UI`: assign the popup object with `RewardSelectionUI`.
- `Bonus Catalog`: assign `DefaultBonusCatalog`.
- `Reward Offers`: configure rewards per tier.

Example:

```text
Reward Offer
- Tier: 1
- Choices To Show: 2
- Rewards: [Bonus A, Bonus B, Bonus C]
```

`RewardManager` randomly picks up to `Choices To Show` valid rewards from the tier list. A reward is valid when it is not already active and its `Required Unlocked Bonus` is either empty or already active.

## RewardSelectionUI

- Put `RewardSelectionUI` on the popup root.
- `Reward Choice Prefab`: assign the reward card prefab.
- `Reward Choice Container`: assign the transform under the popup where cards should be instantiated.

The popup root can start inactive.

## RewardSelectionRepresentation

On the reward card prefab:

- `Display Name Text`: assign the TMP text for the reward name.
- `Description Text`: assign the TMP text for the reward description/effect.
- `Select Button`: assign the button for that card.

## GameManager pause wiring

`Pause On Game Pause` is for gameplay systems that must stop while the reward popup is open. UI input still needs to work, so do not pause generic UI/event-system objects.

Recommended for now:

- Add `SpawnService`.

Optional, depending on how you want reward selection to behave:

- Add `MousePickUpController` only if trash should not be collectable while the popup is open.
- Add shop button components or shop interaction scripts if the shop should not be usable while the popup is open.

Do not add:

- `RewardManager`, because it must keep the popup flow alive.
- `RewardSelectionUI`, because the popup must stay interactive.
- `EventSystem`, because UI clicks must keep working.

## MousePickUpController guardrail

`MousePickUpController` is only for world-space trash pickup. It raycasts against trash and emits the trash-click event consumed by `SpawnService`.

It is not the general mouse input controller for the whole game. UI buttons, reward cards, and popup interaction should use normal Unity UI input through `EventSystem`, `GraphicRaycaster`, and `Button` components.

Pause `MousePickUpController` only if you want to prevent collecting trash while the reward popup is open. Do not pause it because of UI interaction; UI does not need it.
