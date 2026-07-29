# Current Code Assumptions

- A scene contains one active instance of each manager singleton.
- The scene is 3D and contains a player character with `PointAndClickPlayerController`, `NavMeshAgent`, and an animator using `Vex_Controller`.
- Walkable ground is included in `PointAndClickPlayerController.movementSurfaceLayerMask`.
- Walkable ground is covered by a baked or runtime NavMesh.
- Trash is included in `PointAndClickPlayerController.clickBlockerLayerMask` so pickup clicks do not also move the player directly.
- `PointAndClickPlayerController.forceGameplayInputLayers` should normally stay enabled so movement uses only the walkable-ground layer and trash selection uses only the trash layer.
- When `PointAndClickPlayerController.inputCamera` is empty, input raycasts dynamically follow the active Cinemachine stage camera.
- Decorative occluders such as water texture planes should not need input layers. If they have colliders, they should not be on movement or trash layers.
- Trash prefabs have colliders reachable by `GetComponentInParent<Trash>()`.
- Trash objects are on a layer included in click detection.
- UI references are assigned in the Inspector.
- New progress dialogue events use one explicitly assigned scene-level `EventPresentationResolver`; prefab-based presentation fallback is removed.
- Dialogue line prefabs should contain `DialogueLineView`, not an event presentation resolver.
- `RewardSelectionUI` is assigned if reward choice should be visible.
- `GameManager` is responsible for current-pollution loss behavior.
