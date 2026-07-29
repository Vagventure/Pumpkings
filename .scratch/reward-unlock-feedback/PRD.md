Status: ready-for-agent

# PRD: Reward Unlock Feedback And Shared Reward Icons

## Problem Statement

Reward unlocks currently change game state without enough audiovisual feedback. When a progress event unlocks a shop item or activates a bonus, the player needs a short, clear confirmation that something new was gained and where it now belongs.

The reward UI also lacks one shared source of truth for reward icons. A shop item can appear as a reward choice and later as a shop item, while a bonus can appear as a reward choice and later as an active bonus. If each UI surface owns its own icon reference, the same reward can drift visually across the game.

There is also no verified, wired passive-bonus container in the current scene. Code has a generic active bonus display owner concept, but the scene does not currently expose a visible, assigned passive/active bonus target that reward feedback can use. This PRD must therefore include UI target wiring as part of the feature, rather than assuming that a passive bonus container already exists.

## Solution

Add a shared icon field to `Reward Item` so both `Bonus` and `Shop Item` rewards use one icon source across reward choices, active bonus views, shop views, and unlock feedback.

Add a small reward unlock feedback module that plays a global unlock sound and, when an icon is configured, animates a temporary ghost icon to the correct UI target. Shop item unlocks use a fast drop-down animation toward a shop target owned by the shop runtime owner. Bonus unlocks use a fast slide-up animation toward a bonus target owned by the reward runtime owner.

The feedback plays while the reward flow remains paused. The real unlock is applied only after the feedback completes, so the player sees the visual transition first and then sees the real shop item or active bonus appear in its destination area.

If a reward item has no icon, the system should log an error, play the correct unlock sound, skip the visual animation, apply the reward, and continue the reward flow. Missing icons should be treated as authoring errors, not silently covered by fallback art.

## User Stories

1. As a player, I want a sound when I unlock a shop item, so that shop progression feels noticeable.
2. As a player, I want a sound when I unlock a bonus, so that passive progression feels noticeable.
3. As a player, I want a newly unlocked shop item to visually drop into the shop area, so that I understand where the new active skill went.
4. As a player, I want a newly unlocked bonus to visually slide into the active bonus area, so that I understand it is now active.
5. As a player, I want unlock feedback to be fast, so that it confirms the reward without slowing the run.
6. As a player, I want the game to remain paused during unlock feedback, so that I cannot accidentally interact with falling or sliding UI.
7. As a player, I want the real shop item to appear after the drop animation, so that the animation and final UI state make sense.
8. As a player, I want the real active bonus to appear after the slide animation, so that the animation and final UI state make sense.
9. As a player, I want reward cards, shop items, and active bonuses to show consistent icons, so that I recognize the same reward across different screens.
10. As a player, I want no broken visual effect if a reward has missing art, so that the reward flow still continues.
11. As a designer, I want every reward item to define one icon, so that I do not assign the same art repeatedly across reward, shop, and bonus UI.
12. As a designer, I want missing reward icons to log errors, so that authoring mistakes are easy to find.
13. As a designer, I want two global unlock sounds, so that shop item unlocks and bonus unlocks can sound different without configuring audio on every reward item.
14. As a designer, I want the shop target assigned on the shop runtime owner, so that shop feedback lands where the shop UI layout expects it.
15. As a designer, I want the bonus target assigned on the reward runtime owner, so that bonus feedback lands where the active bonus UI layout expects it.
16. As a designer, I want a passive or active bonus container created and wired if none exists, so that bonus unlock feedback has a visible destination.
17. As a designer, I want the feedback target to be a general panel target, not a specific slot, so that layout changes do not require reward logic changes.
18. As a designer, I want shop item feedback to use a drop-down animation, so that shop item unlocks feel like they are entering the shop.
19. As a designer, I want bonus feedback to use a slide-up animation, so that passive effects feel like they enter the active effects area.
20. As a programmer, I want reward feedback isolated in one module, so that reward application logic does not grow UI animation details.
21. As a programmer, I want the reward flow to call feedback before applying the reward, so that the order is deterministic and easy to reason about.
22. As a programmer, I want feedback animations to use unscaled time, so that the UI remains robust if pause behavior later changes to time-scale pause.
23. As a programmer, I want reward choice UI to read icons from the reward item model, so that reward UI has no duplicate icon data.
24. As a programmer, I want active bonus views to read icons from the reward item model, so that bonus UI has no duplicate icon data.
25. As a programmer, I want shop item views to read icons from the reward item model where applicable, so that shop UI has no duplicate icon data.
26. As a programmer, I want missing feedback targets to fail gracefully, so that a bad scene setup does not stall the reward queue.
27. As a future agent, I want the unlock feedback module to support only named animation types for now, so that adding polish later does not leak across reward logic.
28. As a future agent, I want the target contract to stay panel-based, so that slot-specific animation can be added later only if the design actually needs it.
29. As a future agent, I want the feature to avoid a broad event bus or tweening dependency, so that it stays small in the current Unity project.
30. As a future agent, I want Unity setup requirements documented clearly, so that scene and prefab wiring are not missed.

## Implementation Decisions

- `Reward Item` is the single source of truth for reward display icon data.
- `Bonus` and `Shop Item` definitions inherit or otherwise expose the shared reward item icon.
- Reward choice UI reads the icon from the selected reward item's shared icon field.
- Active bonus UI reads the icon from the bonus reward item's shared icon field.
- Shop item UI should read the icon from the shop item reward item's shared icon field when it displays an icon.
- Add a reward unlock feedback module with a small public interface that can play feedback for one reward and invoke a completion callback.
- The feedback module should support exactly two animation types for this PRD: `DropDown` and `SlideUp`.
- `DropDown` is used for shop item unlocks.
- `SlideUp` is used for bonus unlocks.
- The feedback module creates a temporary ghost icon under a configured overlay root.
- The ghost icon is visual-only; it is not the real reward card, shop item, or active bonus view.
- The feedback module should not know about reward filtering, purchase rules, bonus effects, or progress event queueing.
- The feedback module uses unscaled time for animation.
- The feedback animation should be short, with an intended duration around 0.35 to 0.55 seconds.
- The animation may include a small bounce or easing effect, but it should stay fast and readable.
- The feedback module owns two global sound references: one for shop item unlocks and one for bonus unlocks.
- The feedback module plays sound through the existing audio runtime owner.
- No per-reward-item sound field is needed for this PRD.
- If a reward item has no icon, the feedback module logs an error, plays the correct global sound, skips visual feedback, and completes.
- If a feedback target is missing, the system should log an error, play the correct global sound, skip visual feedback, and complete.
- Real reward application happens after unlock feedback completes.
- The reward runtime owner coordinates reward selection, feedback playback, reward application, and reward flow completion.
- The reward runtime owner must not resume gameplay until the feedback callback has applied the reward and completed the progress event flow.
- The reward UI should be hidden or made non-interactive before unlock feedback begins, so the player cannot click more reward choices during the animation.
- The shop runtime owner exposes the shop unlock feedback target.
- The reward runtime owner exposes the bonus unlock feedback target.
- The shop target and bonus target are panel-level targets, not individual newly created slots.
- The current project does not have a confirmed scene-wired passive bonus container. This feature includes creating or assigning a visible active/passive bonus target for bonus unlock feedback.
- The current active bonus display owner may be reused, but it should remain a display owner only and must not become responsible for applying bonus effects.
- Avoid introducing a plugin-style feedback framework for this PRD. The desired result is similar in spirit to a small "Feel" layer, but implemented as a focused local module.
- Avoid slot-specific animation and avoid passing instantiated UI objects between reward, shop, and bonus panels.

## Testing Decisions

- Good tests should validate external behavior: when feedback completes, whether the reward is applied, whether the completion callback fires, and whether invalid authoring data fails without stalling the flow.
- Tests should avoid asserting private coroutine names, easing math details, or internal timer fields.
- The reward item model should be tested or manually verified to expose a shared icon to both bonus and shop item definitions.
- Reward choice UI should be tested or manually verified to show the shared reward item icon when present and hide or clear its icon view when absent.
- The feedback module should be tested for calling completion after a shop item unlock feedback request.
- The feedback module should be tested for calling completion after a bonus unlock feedback request.
- The feedback module should be tested for missing icon behavior: log error path, play sound path if audio is assigned, no visual spawn, and completion.
- The feedback module should be tested for missing target behavior: log error path, play sound path if audio is assigned, no visual spawn, and completion.
- Reward flow should be tested for applying a shop item only after feedback completion.
- Reward flow should be tested for activating a bonus only after feedback completion.
- Reward flow should be tested for keeping gameplay paused until feedback and reward application complete.
- Shop item unlock feedback should be manually verified in play mode because it depends on UI layout and scene target placement.
- Bonus unlock feedback should be manually verified in play mode because the passive/active bonus panel target must be created and assigned in the scene.
- There is no established Unity test suite in the current repo. If tests are added as part of implementation, prioritize component-level tests for reward flow ordering and feedback completion over pixel-perfect animation tests.

## Out of Scope

- Per-reward custom unlock sounds are out of scope.
- Fallback icons are out of scope; missing icons are authoring errors.
- Slot-specific landing targets are out of scope.
- Physically moving the reward card UI instance into the shop or bonus panel is out of scope.
- Adding or integrating a third-party feedback plugin is out of scope.
- Full final VFX polish, particles, glow bursts, screen shake, or advanced tween sequencing are out of scope.
- Reworking purchase validation, budget rules, awareness rules, current pollution rules, or bonus effect math is out of scope.
- Save/load persistence for unlocked items, active bonuses, or icon assignments is out of scope.
- A full active/passive bonus panel redesign is out of scope, except for the minimum target/container needed for this feedback.
- Per-slot active bonus details, right-click details, or tooltip behavior are out of scope.
- Changing reward choice generation, reward prerequisites, or reward pool balancing is out of scope.

## Further Notes

- Code inspection before writing this PRD found no separate passive bonus container wired in the current scene.
- The existing bonus display code has a generic active bonus parent concept, but the current scene does not appear to have a connected bonus display owner or assigned passive bonus panel target.
- The implementation should treat "passive bonus container" and "active bonus target" as the same UI destination unless the design later splits active and passive bonus categories.
- The current shop item parent is also not assigned in the inspected scene, so Unity setup must include assigning the shop item parent and shop feedback target.
- This PRD intentionally keeps the module small: one shared icon source, one feedback presenter, two animation types, two global sounds, and owner-provided panel targets.
