# Player Navigation Code Map

Context: [Player navigation feel](../contexts/player-navigation-feel.md)

## Working Set

- `Assets/Scripts/TrashPickupProgressView.cs` — world-space progress view currently driven only by Recycling Patrol; manual player pickup leaves it hidden.
- `Assets/Scripts/CameraFacingRotationUtility.cs` — shared full-billboard and player semi-billboard rotation math.

- `Assets/Scripts/PointAndClickPlayerController.cs` — owns point-and-click input, NavMesh destination setting, trash targeting, approach, pickup timing, facing, and player animation playback.
- `Assets/Scripts/MousePickUpController.cs` — publishes confirmed `OnTrashClicked(Trash)` collection requests.
- `Assets/Scripts/InputRaycastCameraResolver.cs` — resolves the camera used for input raycasts.
- `Assets/Scripts/Trash.cs` — target identity, pollution, income, pickup time, and audio data.
- `Assets/PlayerAnimations/Vex_Controller.controller` — current player animation state machine.

## Cross-System Seams

- Pending trash destinations track both wind-movable trash and active `TrashPathFollower` river trash.

1. Player selects and approaches trash through `PointAndClickPlayerController`.
2. Completed pickup enters the lifecycle seam through `MousePickUpController.OnTrashClicked`.
3. Continue in [Spawning and pooling](spawning-pooling.md) for despawn and [Economy and game state](economy-game-state.md) for pollution/budget effects.

## Tests

- `Assets/Tests/EditMode/InputRaycastCameraResolverTests.cs` covers active Cinemachine camera selection when another stage camera owns `MainCamera`.
- `Assets/Tests/EditMode/CameraFacingRotationUtilityTests.cs` covers screen-aligned billboard rotation and the ±75-degree player-facing clamp.
- `Assets/Tests/EditMode/TrashPickupProgressViewTests.cs` covers detachment from non-uniformly scaled trash anchors.
- NavMesh, physics raycasts, animation timing, and scene layers still require PlayMode/live Unity verification or extracted synchronous logic.
