# UI Presentation Code Map

Contexts: [Reward item view](../contexts/reward-item-view.md), [Reveal text](../contexts/reveal-text.md), [Feel / SFX](../contexts/feel-sfx.md)

## Reward And HUD Views

- `Assets/Scripts/RewardItemView.cs` — shared runtime-configured shop/reward card.
- `Assets/Scripts/ActiveBonusSlotView.cs` and `HotBarController.cs` — compact active bonus HUD.
- `Assets/Scripts/RewardSelectionUI.cs` and `RewardSelectionRepresentation.cs` — reward selection/fallback presentation.
- `Assets/Scripts/RewardPresentationPresenter.cs`, `PassiveRewardPresenter.cs`, `ShopItemRewardPresenter.cs` — reward presentation adapters.

## Reveal, Layout, And Feel

- `Assets/Scripts/CursorController.cs`, `CursorTargetResolver.cs`, and `GrabCursorTarget.cs` — overlay cursor plus collectable-trash and clickable-UI hover routing.
- `Assets/Scripts/TrashPickupProgressView.cs` — detached, screen-aligned Recycling Patrol target/pickup progress that avoids trash-scale shear; manual player pickup currently leaves it hidden.
- `Assets/Scripts/MoneyFlyVfxController.cs` — world-to-overlay money icon flight with normalized screen-space transform.
- `Assets/Prefabs/UI/TrashPickupProgress.prefab` and `MoneyFlyIcon.prefab` — reusable presentation prefabs.

- `Assets/Scripts/UIRevealController.cs` and `UIVfxController.cs` — reveal configuration and timing.
- `Assets/Scripts/EventPresentationEvents.cs` — semantic presentation events.
- `Assets/Scripts/LayoutItemSlideIn.cs` — layout-safe entrance.
- `Assets/Scripts/RewardItemFeelFeedback.cs` — local accepted/rejected Feel playback.
- `Assets/Scripts/RuntimeUILayoutRefresher.cs` — deferred layout refresh helper.

## Tests

- `Assets/Tests/EditMode/CursorTargetResolverTests.cs`
- `Assets/Tests/EditMode/TrashPickupProgressViewTests.cs`
- `Assets/Tests/EditMode/MoneyFlyVfxControllerTests.cs`
- `Assets/Tests/EditMode/ProgressBarControllerTests.cs`

- `Assets/Tests/EditMode/EventDialogueDefinitionsTests.cs`
- `Assets/Tests/EditMode/LayoutItemSlideInTests.cs`
- `Assets/Tests/EditMode/RewardItemFeelFeedbackTests.cs`
