---
read_when: [editmode-tests, regression-test, test-patterns]
avoid_when: [documentation-only, playmode-only]
primary_files: [Assets/Tests/EditMode/, Assets/Scripts/, Assets/Scripts/Editor/]
tests: [Assets/Tests/EditMode/*.cs]
code_maps: [docs/code-map/testing-unity.md]
---

# EditMode Testing Context

## Purpose

EditMode tests cover synchronous domain rules, ScriptableObject validation, view setup that does not require rendered frames, and pure geometry/routing behavior. Use Unity MCP to run the smallest relevant EditMode selection when available; expand only after focused tests pass.

Coroutine, frame-order, animation, physics, NavMesh, and live scene wiring behavior may require `[UnityTest]`, PlayMode tests, or extraction of synchronous decision logic.

## Proportional Verification

Short, localized, low-risk changes may use only the smallest relevant static or targeted check and then be handed to the user with one concrete Play Mode behavior to verify. The handoff must say that Play Mode was not run.

Large, multi-step, cross-system, or high-risk changes require the relevant full flow: focused tests, compilation/console checks, Unity MCP inspection, wiring checks, and Play Mode for runtime-dependent behavior. Expand to this flow when compilation cannot expose the likely failure mode or more than one runtime owner is affected.

## Current Conventions

- Tests use NUnit `[Test]` and `Assert.That` constraint syntax.
- Create small `GameObject`/MonoBehaviour graphs or `ScriptableObject.CreateInstance` values inside each test.
- Configure private serialized state through small `BindingFlags` helpers when no suitable interface exists.
- Use test-only subclasses to expose protected seams where appropriate.
- Clean up deterministically in `try/finally` with `Object.DestroyImmediate`.
- Keep one behavior claim per test name and avoid depending on execution order.
- Tests are currently wrapped for the Unity Editor and live under `Assets/Tests/EditMode/`; there is no dedicated test asmdef.

## Current Coverage Routes

- `SpawnAreaSamplerTests`: rotated/scaled BoxCollider sampling.
- `StageManagerTests`: initial stage activation, zoom-transition start state, Cinemachine priority routing, start-index clamping, pause acquisition, and duplicate transition-request rejection.
- `InputRaycastCameraResolverTests`: active Cinemachine stage-camera selection when another camera owns `MainCamera`.
- `LevelControllerProgressEventsTests`: authored ordering and explicit progress-event sorting.
- `RewardCatalogTests`: default catalog and next-unowned reward selection.
- `RewardItemFeelFeedbackTests`: safe completion without a configured feedback player.
- `LayoutItemSlideInTests`: initial offset and interaction blocking.
- `TutorialSupportTests`: spawn blocking and shop unlock dependency interfaces, not `TutorialController` behavior.
- `EventDialogueDefinitionsTests`: speaker/choice fallbacks, dialogue data, resolver behavior, choice-side routing, and deterministic timestamps.
- `TrashPathFollowerTests`: river waypoint traversal, endpoint stopping, pickup pause, and dynamic pickup tracking.
- `CursorTargetResolverTests`: collectable-trash and interactable-UI cursor states.
- `ProgressBarControllerTests`: smooth retargeting from the current visual fill.
- `RecyclingPatrolCooldownTests`: linear paused cooldown state.
- `RecyclingPatrolEconomyTests`: path-targeted shop discounts and source-aware no-income removal.
- `RecyclingPatrolTargetSelectorTests`: Patrol Area eligibility and shortest reachable-path selection.

## Test Design Rules

- Test through the smallest stable interface when possible. Reflection is acceptable for Unity serialized setup, but avoid testing private implementation steps merely because they exist.
- For static event publishers and singletons, always destroy objects and ensure subscriptions cannot leak between tests.
- When a serialized field changes, check its custom editor and add/update validation tests when the field has behavioral invariants.
- Scene/prefab wiring claims require Unity MCP or live Unity inspection; EditMode tests do not prove Inspector references.
- A regression fix should add the narrowest test that would have failed before the fix.

## Known Gaps

There is no direct EditMode coverage for `ScoringService`, `GameManager`, audio/music, most `SpawnService` lifecycle behavior, purchase flow, or `TutorialController`. Current tests are synchronous; no `[UnityTest]`, setup fixtures, or coroutine tests are present.

## Important Locations

- Tests: `Assets/Tests/EditMode/`
- Runtime code: `Assets/Scripts/`
- Custom editors: `Assets/Scripts/Editor/`
- Unity MCP endpoint: `http://127.0.0.1:8080/mcp`
- Cached operation routing: [Unity MCP routing](../agents/unity-mcp-routing.md)
