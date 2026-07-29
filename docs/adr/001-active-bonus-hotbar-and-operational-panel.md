# ADR 001: Active Bonus Hotbar And Operational Panel

## Status

Accepted

## Context

Active bonuses need a compact in-game display. The game can have up to around 10 active bonuses, so showing full card text for every bonus would make the HUD noisy.

Reward item cards already use `RewardItemView` for shop items, bonus item surfaces, and other reward item displays. That view is card-shaped: title, subtitle, description, icon, and optional button. A compact HUD hotbar has different needs: a small icon, a short value, and for timed passive effects, a radial cooldown.

There is also a planned operational panel where players can inspect detailed bonus information, but that panel still needs further design discussion.

## Decision

Active bonuses in the HUD will be displayed with a dedicated `ActiveBonusSlotView`, not with `RewardItemView`.

The active bonus hotbar will use one reusable `ActiveBonusSlotView` prefab. A hotbar controller will receive the slot prefab and parent transform in the Inspector, then instantiate one slot per active bonus, analogous to how shop items are spawned from a configured prefab.

Each active bonus slot will show all active bonuses in a compact form:

- Use `RewardItem.effectIcon` as the primary mechanical icon in the hotbar slot.
- For timed passive bonuses, show a radial cooldown overlay.
- For non-timed bonuses, show a short numeric value such as `-20%` or `+25%`.
- Do not show full descriptions or long effect text in the hotbar.

`RewardItem` will have a stable `effectIcon` field separate from the existing main display icon. The main icon represents the reward item as a card or choice. The effect icon represents the mechanical effect for compact UI.

Shop item cards will continue to use `RewardItemView`, with:

- Main item icon.
- Cost text over the main icon, formatted like `$ 25`.
- `effectIcon` and formatted effect value shown separately.

Bonus details in a future operational panel will also use the effect icon and formatter, but that panel is not part of this task.

## Consequences

`RewardItemView` stays focused on card-style reward displays and does not grow hotbar-specific radial cooldown behavior.

`ActiveBonusSlotView` can stay small and HUD-specific.

The future operational panel can be designed separately without blocking the hotbar implementation.

The hotbar needs Inspector setup for its prefab and parent, and active bonus slot prefabs need references for the effect icon, value text, and optional radial cooldown image.
