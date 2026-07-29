# Testing And Unity Code Map

Context: [EditMode testing](../contexts/editmode-testing.md). Unity MCP operations: [Unity MCP routing](../agents/unity-mcp-routing.md).

## Locations

- Runtime scripts: `Assets/Scripts/`
- Custom inspectors and editor utilities: `Assets/Scripts/Editor/`
- EditMode tests: `Assets/Tests/EditMode/`
- Production scene: `Assets/Scenes/PROD_SCENE.unity`
- Player animation assets: `Assets/PlayerAnimations/`
- Project/package configuration: `ProjectSettings/`, `Packages/`
- Project-scoped Codex agents: `.codex/agents/`
- Task/PRD records: `.scratch/`
- Domain context: `docs/contexts/`
- Cross-system maps: `docs/context-map/`
- Decisions: `docs/adr/`

## Test Inventory

- `EventDialogueDefinitionsTests.cs`
- `InputRaycastCameraResolverTests.cs`
- `LayoutItemSlideInTests.cs`
- `LevelControllerProgressEventsTests.cs`
- `RewardCatalogTests.cs`
- `RewardItemFeelFeedbackTests.cs`
- `RecyclingPatrolCooldownTests.cs`
- `RecyclingPatrolEconomyTests.cs`
- `RecyclingPatrolTargetSelectorTests.cs`
- `SpawnAreaSamplerTests.cs`
- `StageManagerTests.cs`
- `TutorialSupportTests.cs`

## Inspector Rule

When serialized fields change, search `Assets/Scripts/Editor/` for a matching `[CustomEditor]`. Manually drawn Inspectors must explicitly find and draw the changed property.
