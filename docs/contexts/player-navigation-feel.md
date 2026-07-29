---
read_when: [player-movement, point-and-click, navmesh, trash-pickup]
avoid_when: [ui-only, spawn-only]
primary_files: [Assets/Scripts/PointAndClickPlayerController.cs, Assets/Scripts/MousePickUpController.cs, Assets/Scripts/Trash.cs, Assets/Scripts/InputRaycastCameraResolver.cs]
tests: [Assets/Tests/EditMode/InputRaycastCameraResolverTests.cs]
code_maps: [docs/code-map/player-navigation.md]
---

# Player Navigation Feel Context

## Current Direction

Plastic Seeds is now a 3D point-and-click game. The player controls a 3D character by clicking walkable ground, in the style of narrative CRPG movement such as Disco Elysium.

The current locomotion prototype should stay deliberately simple for movement: click ground, walk to the sampled NavMesh destination, stop cleanly, and play `Walk` while moving and `Idle` while stopped.

Trash pickup is the one current interaction layered onto movement: click trash, walk to a reachable NavMesh point near it, crouch for that trash's configured pickup time, collect the trash, then return to idle.

## Runtime Owner

`PointAndClickPlayerController` owns player movement input, trash click targeting, NavMesh destination setting, arrival stopping, facing, pickup timing, and direct player animation playback.

Input raycasts resolve an explicitly assigned camera first, then the active Cinemachine brain output/live stage camera, then `Camera.main`. A miss on the resolved camera must not fall through to another active stage camera.

Do not reintroduce keyboard movement, dash, jump, combat-facing, or skill targeting unless a task explicitly asks for it. Crouch currently exists only as the trash pickup pose, not as crouch locomotion.

## Movement Feel Rules

- Left-clicking valid walkable ground should send the player character to that point.
- Clicking UI should not move the player character.
- Clicking trash should select that trash as `pendingTrash`, then send the player character to a reachable point near it.
- Clicking walkable ground should clear any pending trash target and move normally.
- Clicking blocked world objects without a `Trash` component should not also set a movement destination.
- When the player character reaches the destination, the NavMesh path should be reset and velocity cleared so the character does not rotate or slide in place.
- Rotation should follow current movement while moving but remain within `maxCameraFacingAngle` (75 degrees by default) left or right of the active camera-facing direction, preventing edge-on character presentation. Rotation should not continue after arrival.
- If the `NavMeshAgent` is not on a NavMesh, movement commands should be ignored instead of throwing runtime errors.
- Trash approach ignores the trash object's vertical offset when sampling the NavMesh, validates a complete path, and accepts the target only when the reachable endpoint is within `trashPickupDistance`; failed targeting must not leave stale `pendingTrash` state.
- A pending target is tracked while either wind or `TrashPathFollower` is moving it, so the NavMesh destination follows river trash instead of becoming stale.
- A destination must not be reset in the same frame it is set; `NavMeshAgent` may need a frame before path state becomes meaningful.

## Trash Pickup Rules

- `Trash` owns `PickupTime`. This duration controls how long the player character remains in pickup crouch before the trash is collected.
- `TrashPickupProgressView` is temporarily disabled for manual player pickup. It is owned by Recycling Patrol targeting: visible at zero while the Patrol travels, filled over `Trash.PickupTime / 2` during Patrol pickup, and hidden on retarget, removal, or pool reset. While visible it detaches from non-uniform trash scale and remains screen-aligned.
- Trash click detection uses the `Trash` layer and falls back from raycast to a small sphere cast so thin or angled trash colliders are still clickable.
- After selecting trash, movement destination should prefer the walkable ground under the same screen click, then fall back to sampling around the trash position/hit point.
- Trash collection happens through `MousePickUpController.CollectTrash(trash)`, which raises `MousePickUpController.OnTrashClicked`.
- `SpawnService` remains the despawn owner by subscribing to `MousePickUpController.OnTrashClicked`.
- `Debug Click Raycasts` logs `[DEBUG-click-ray]` lines for diagnosing which collider/layer was hit and whether `SetDestination` succeeded.

## Animation Direction

The current player animation graph is intentionally minimal but includes pickup crouch.

- `Assets/PlayerAnimations/Vex_Controller.controller` should expose `Idle`, `Walk`, and `SS_CrouchIdle` in the Base Layer for now.
- `PointAndClickPlayerController` directly cross-fades to animation states by name.
- `SS_CrouchIdle` is used only for trash pickup. The player remains in it for `Trash.PickupTime`.
- Do not depend on animator parameters such as `Speed`, `IsMoving`, `IsCrouching`, or trigger-heavy `Any State` transitions for the prototype.
- Root motion should remain disabled by the controller unless a later locomotion task deliberately moves authority from `NavMeshAgent` to animation root motion.

## Important Files

- `Assets/Scripts/PointAndClickPlayerController.cs` - point-and-click player movement, NavMesh guards, arrival stopping, facing, and direct `Idle`/`Walk` playback.
- `Assets/Scripts/Trash.cs` - trash identity, scoring values, income, pickup timing, and audio references.
- `Assets/Scripts/MousePickUpController.cs` - confirmed trash pickup event publication.
- `Assets/Scripts/TrashPickupProgressView.cs` - pickup progress presentation and camera-facing behavior.
- `Assets/Scripts/CameraFacingRotationUtility.cs` - screen-aligned billboard rotation and camera-relative semi-billboard yaw clamping.
- `Assets/PlayerAnimations/Vex_Controller.controller` - current simplified player animator controller.
- `Assets/PlayerAnimations/SS_Idle.fbx` - idle animation source.
- `Assets/PlayerAnimations/SS_Walk.fbx` - walk animation source.
- `Assets/PlayerAnimations/SS_CrouchIdle.fbx` - pickup crouch animation source, if present in project assets.
- `ProjectSettings/NavMeshAreas.asset` - project-level NavMesh area definitions.
- Scene objects and prefabs must provide the actual `NavMeshAgent`, player model, animator assignment, movement surface layers, and baked NavMesh.

## Unity Setup Expectations

Agents should not edit `.unity` scene files or `.prefab` files automatically unless the user explicitly asks.

When code changes affect the player character, final responses must include concrete Unity setup steps: what object gets `PointAndClickPlayerController`, what camera/layers to assign, whether `Vex_Controller` must be assigned to the animator, whether `SS_CrouchIdle` state names match, whether `Trash.PickupTime` needs tuning, and whether NavMesh needs to be baked or verified.

## Out Of Scope For Now

- Crouch locomotion outside trash pickup.
- Run, sprint, evade, hit, attack, or skill animations.
- Dialogue interaction routing.
- Click-to-interact pathing around objects.
- Camera follow or cinematic camera behavior.
- Player combat, stats, health, dash, jump, or skill systems.
