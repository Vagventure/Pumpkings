---
read_when: [feel, semantic-sfx, ui-feedback, vfx-hooks]
avoid_when: [music-only, player-navigation-only]
primary_files: [Assets/Scripts/AudioSFXController.cs, Assets/Scripts/RewardItemFeelFeedback.cs, Assets/Scripts/RewardManager.cs, Assets/Scripts/ScoringService.cs]
tests: [Assets/Tests/EditMode/RewardItemFeelFeedbackTests.cs]
code_maps: [docs/code-map/ui-presentation.md, docs/code-map/audio-music.md]
---

# Feel / SFX Context

## Related Gameplay Feel Context

Player locomotion feel is documented separately in [Player navigation feel context](player-navigation-feel.md). The current gameplay feel direction is 3D point-and-click movement: click walkable ground, walk via NavMesh, stop cleanly, and use only `Idle`/`Walk` animation states for now.

## Decision

Game feel hooks for UI rewards, progress events, shop actions, and future UI VFX should be driven by semantic runtime events, not by clip or effect references scattered across gameplay controllers.

`AudioSFXController` owns SFX clip assignment and playback. Gameplay controllers publish intent events such as "progress event spawned" or "shop item purchased"; they do not store audio clips and do not call `AudioManager` directly for these UI/reward moments.

`AudioManager` remains the low-level playback backend. `AudioSFXController` is the presentation layer for SFX routing.

## Current SFX Event Surface

The current semantic SFX events are:

- `RewardManager.OnEventSpawnSFX`: progress dialogue panel/event appeared.
- `RewardManager.OnEventDurationSFX`: progress event started and should play duration/loop audio.
- `EventPresentationEvents.OnEventDurationStopSFX`: progress event ended and duration/loop audio should stop.
- `EventPresentationEvents.OnEventButtonClickSFX`: progress event continue button clicked.
- `RewardManager.OnRewardChoiceShownSFX`: reward choice UI appeared.
- `RewardManager.OnRewardChoiceSelectedSFX`: reward choice selected.
- `RewardManager.OnShopItemUnlockedSFX`: shop item reward unlocked.
- `RewardManager.OnBonusUnlockedSFX`: bonus reward unlocked.
- `ScoringService.OnShopItemPurchasedSFX`: shop item purchase succeeded.
- `ScoringService.OnShopItemPurchaseFailedSFX`: shop item purchase failed.

`AudioSFXController` subscribes to these events and exposes matching Inspector clip fields. `eventDurationSFXClip` uses a dedicated loop `AudioSource` so duration audio does not compete with one-shot SFX.

## Future VFX Direction

Use the same semantic moments for UI VFX. A future VFX controller should subscribe to gameplay/presentation events or a shared feel-event surface rather than adding VFX references to `RewardManager`, `ScoringService`, or `EventPresentationResolver`.

Preferred VFX moments:

- Progress event spawn: show dialogue panel or active line entrance effect.
- Event duration: optional ongoing typing/talking visual while the event is active.
- Event button click: button response effect local to the event button.
- Reward choice shown: card/panel reveal effect.
- Reward choice selected: selected card confirmation effect.
- Shop item unlocked: unlock effect at `ShopController.ShopItemUnlockedVfxTarget`.
- Bonus unlocked: unlock effect at `RewardManager.BonusUnlockedVfxTarget`.
- Shop item purchased: purchase confirmation effect on the clicked `RewardItemView`.
- Shop item purchase failed: denied/insufficient-budget feedback on the clicked `RewardItemView` or budget UI.

If a VFX needs a target transform, pass enough context with the event or expose a stable target property. Do not search scene hierarchy by names.

## Inspector Rules

Assign audio clips on `AudioSFXController`, not on `RewardManager`.

Keep UI VFX target references on their owning UI/controller component when already present:

- `RewardManager.BonusUnlockedVfxTarget` for bonus unlock destination.
- `ShopController.ShopItemUnlockedVfxTarget` for shop item unlock destination.
- `RewardItemView` for purchased/failed purchase item-local effects.

`RewardItemFeelFeedback` is the optional item-local Feel adapter. It exposes accepted and rejected `MMF_Player` references on the shared `RewardItemView` prefab and does not branch on `ShopItemDefinition` versus `BonusDefinition`. Global SFX continue to be routed through `AudioSFXController`; item-local Feel players should normally contain visual feedback only.

Do not edit Unity scene or prefab files automatically unless explicitly requested. Prefer code and concrete setup steps.

## Vocabulary

Use "feel" for the combined SFX/VFX response layer.

Use "progress event" for milestone narrative/reward moments. Do not reintroduce "tier" terminology.

Use "shop item purchased" for a successful spend of budget. Use "shop item unlocked" for a reward that makes a new shop item available.
