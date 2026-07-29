# Stage Transitions Code Map

Context: [Stage transitions](../contexts/stage-transitions.md)

## Runtime Owners

- `Assets/Scripts/StageManager.cs` — ordered stage roots/cameras, outgoing/incoming lens zoom, midpoint root/camera-object switch, priority routing, and transition/sequence signals.
- `Assets/Scripts/ProgressEventDefinition.cs` — authored `GoToNextStage` completion effect.
- `Assets/Scripts/RewardManager.cs` — hands gameplay pause and pending progress-event continuation across the zoom transition.
- `Assets/Scripts/GameManager.cs` — existing gameplay pause/resume implementation used by both reward and direct stage transitions.

## Unity Wiring

- `Assets/Scenes/PROD_SCENE.unity` — production hierarchy; `_GameControllers` is global, while `3D_Scene/BEACH` and `3D_Scene/RIVER` are current stage roots.
- `_GameControllers/Cameras` owns the separate `Beach` and `River` stage camera objects plus the shared `__Brain` object.
- Each stage entry references its root and matching `CinemachineCamera` under `_GameControllers/Cameras`.

## Tests

- `Assets/Tests/EditMode/StageManagerTests.cs` — initial stage selection, zoom-transition start state, priority assignment, start-index clamping, pause acquisition, and duplicate-request rejection.
