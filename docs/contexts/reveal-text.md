---
read_when: [text-reveal, typewriter, ui-entrance, continue-behavior]
avoid_when: [non-ui-gameplay, audio-only]
primary_files: [Assets/Scripts/UIRevealController.cs, Assets/Scripts/UIVfxController.cs, Assets/Scripts/EventPresentationResolver.cs, Assets/Scripts/DialogueLineView.cs]
tests: [Assets/Tests/EditMode/EventDialogueDefinitionsTests.cs]
code_maps: [docs/code-map/ui-presentation.md]
---

# Reveal Text Context

## Decision

UI reveal belongs to the feel layer. Runtime controllers publish or expose the UI moment; `UIVfxController` owns reveal timing, sequencing, entrance playback, and completion behavior.

Use `UIRevealController` as the reusable reveal component for UI surfaces. It can be added to dialogue line prefabs, reward screens, shop panels, and future UI surfaces that need the same reveal behavior.

## Current Model

`EventDefinition` stores one presentation choice:

- `RevealTextDuringEntrance`: master enable for reveal text behavior on this event.

For progress dialogue events, `RewardManager` starts its explicitly assigned scene-level `EventPresentationResolver`. Progress presentation prefab fallback is removed.

`EventPresentationResolver` spawns `DialogueLineView` instances into its line container. Reveal is driven on the active line's `UIRevealController`, not on the old static event panel body text.

`UIVfxController` subscribes to that moment and currently owns:

- progress event entrance timing
- optional Feel `MMF_Player` entrance playback
- reveal text typewriter speed
- waiting for entrance before typewriter reveal starts
- sequential reveal of configured TMP text fields
- completing text reveal when the continue button is clicked
- start/stop semantic SFX signals for typing audio

`UIRevealController` owns the reveal setup:

- `RevealRoot`: the UI transform animated by the fallback entrance.
- `RevealCanvasGroup`: optional canvas group for fallback fade.
- `EntranceFeedback`: optional Feel `MMF_Player` entrance effect.
- `RevealTextEntries`: serialized TMP text reveal list.

`RevealTextEntry` fields:

- `RevealTextEntry.Text`: the TMP text field.
- `RevealTextEntry.Reveal`: whether that field participates in sequential typewriter reveal.

The list order is the reveal order. Do not add a separate order field.

`EventPresentationResolver` references the active line's optional `UIRevealController`. It should not own reveal root, canvas group, Feel player, or reveal text entries. If a dialogue line prefab has no `UIRevealController`, the event must remain usable with all line text visible and continue behavior working normally.

If `UIRevealController.EntranceFeedback` is assigned, `UIVfxController` plays it and skips the built-in fallback slide/fade/scale entrance. If no entrance feedback is assigned, `UIVfxController` uses its built-in fallback entrance on `RevealRoot`.

Progress dialogue line text reveal starts only after the active line entrance is complete. With Feel entrance assigned, the wait uses `MMF_Player.TotalDuration`. With fallback entrance, it uses `UIVfxController`'s fallback entrance duration.

If `RevealTextDuringEntrance` is false, all text is visible from the beginning and the reveal list is ignored.

If `RevealTextDuringEntrance` is true:

- entries with `Reveal = false` are visible from the beginning
- entries with `Reveal = true` are hidden first and revealed one by one in list order

`AudioSFXController` owns the typing audio clip and loop source. It subscribes to `UIVfxController` typing start/stop signals.

## Continue Button Behavior

The progress event continue button is visible from the beginning.

If any reveal text is incomplete, pressing the button completes every reveal text immediately and does not end the event.

If the event entrance is still running, pressing the button does not skip the entrance animation.

For dialogue events, the active line can continue only after the reveal text is fully visible. After the opening line, choices appear. After a selected player line, final continue completes the progress event. Other UI surfaces may use `UIRevealController` directly without event flow semantics.

## Future Direction

Reveal should remain selectable per TMP text field. Dialogue lines, reward screens, shop panels, and future UI surfaces can reveal any configured TMP fields while still running in one sequence. The same component also owns the optional Feel entrance hook, so new UI surfaces can reuse the same authoring model without event-specific code.

Keep the authoring model centralized:

- `UIRevealController` owns TMP reveal references and exposes which fields are revealable through `RevealTextEntry`.
- `UIVfxController` owns speed, timing, and sequencing.
- `AudioSFXController` owns typing SFX.
- `DialogueLineView` owns static line binding such as timestamp, speaker name, role, body, portrait, and current/past color targets.

Avoid adding reveal timing fields to `EventDefinition`. Event assets should choose presentation intent, not tune global feel values.

Avoid adding one-off reveal scripts to each text object. Put reusable reveal behavior on `UIRevealController` attached to the dialogue line prefab or other reusable UI surface.
