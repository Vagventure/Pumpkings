---
read_when: [tutorial, onboarding, first-bottle, first-shop-item, pointer]
avoid_when: [general-shop, general-spawning]
primary_files: [Assets/Scripts/TutorialController.cs, Assets/Scripts/SpawnService.cs, Assets/Scripts/ShopController.cs, Assets/Scripts/RewardItemView.cs]
tests: [Assets/Tests/EditMode/TutorialSupportTests.cs]
code_maps: [docs/code-map/shop-tutorial.md]
---

# Tutorial Context

## Current Scope And Ownership

`TutorialController` owns one shared animated pointer and two one-shot onboarding phases:

1. point at the first spawned bottle until that exact bottle is removed;
2. point at the first newly unlocked shop item until that exact view is clicked.

The controller is event-driven. Its only public state is `IsPointerVisible`; other behavior enters through spawn, shop, reward-view, and game-state events.

Unity MCP verified the implementation and scene wiring in `PROD_SCENE`. `TutorialController` lives on the full-screen `TutorialOverlay`, and its inactive `TutorialPointer` child uses the sliced red-arrow sprite. No prefab wiring is required.

## Bottle Phase

- The first `SpawnService.TrashAdded` bottle becomes the target.
- The tutorial blocks future bottle spawning through `SpawnService.SetSpawnBlocked(Bottle, true)`; already-active bottles remain.
- The pointer is projected near the target along the player-to-bottle screen direction.
- Only removal of the exact target dismisses the pointer and releases the block.
- If the target is destroyed or deactivated outside the normal removal event, the controller defensively dismisses the pointer and releases the block.
- `OnDisable` always releases the bottle block and clears presentation state.

## Shop Phase

- The first `ShopController.ItemUnlocked(view)` becomes the pending target.
- Presentation waits while a progress-event flow is open or gameplay is inactive.
- The pointer appears to the left of the unlocked `RewardItemView` and points right.
- Only clicking the exact target view dismisses it.

Each phase starts at most once per component lifetime. Missing required references still consume that phase in the current implementation, so wiring must be validated before play.

## Pointer Presentation

The pointer uses unscaled time for entrance, pulse, movement, and exit. Its `Image.raycastTarget` remains disabled. The pointer GameObject must be separate from the controller GameObject because the controller intentionally refuses to deactivate itself.

Overlay canvases use no canvas camera. Camera/world-space canvases use `Canvas.worldCamera`; bottle projection separately uses the assigned world camera.

## Unity Setup

- `___UI_Canvas_BootStrap/TutorialOverlay` is a full-screen UI-layer child with `LayoutElement.ignoreLayout` enabled.
- `TutorialPointer` is a separate inactive child with the right-facing `red arrow_0` sprite, a `160 x 82` RectTransform, and raycast targeting disabled.
- The controller references `___UI_Canvas_BootStrap`, `GameObject/MainCharacter`, `3DCamera`, `_GameControllers/SpawnService`, and `_GameControllers/ShopController`.
- No custom editor exists, so the default Inspector exposes the serialized tuning fields.

## Verification

`TutorialSupportTests` covers the dependency interfaces: per-type spawn blocking and shop-item unlock creation/publication. It does not instantiate or exercise `TutorialController`.

Missing coverage includes event subscriptions, one-shot behavior, exact-target dismissal, block cleanup, progress-event waiting, positioning, canvas modes, animation, missing references, and disable cleanup. Frame/coroutine behavior will require `[UnityTest]`, PlayMode coverage, or extraction into synchronous logic.

## Important Files

- `Assets/Scripts/TutorialController.cs`
- `Assets/Scripts/SpawnService.cs`
- `Assets/Scripts/ShopController.cs`
- `Assets/Scripts/RewardItemView.cs`
- `Assets/Tests/EditMode/TutorialSupportTests.cs`
