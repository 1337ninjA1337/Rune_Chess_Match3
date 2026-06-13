# Visual style — portrait auto-battler overhaul

Status: living document. Source of truth for the visual overhaul that brings the
presentation in line with the auto-battler ("auto-chess") genre while keeping the
match-3 board that makes this project its own thing.

## Guardrail (read first)

- This is an **original** art direction. We adopt only **genre conventions**
  (board + shop + bench + alliance panel + tier-coloured cards), which are not
  protectable, on our **own** palette, names, runes, and iconography.
- We do **not** reproduce the visual style, hero designs/names, or lore of Dota
  or any Valve product, or any other third party. This mirrors the rule in
  `codex.md`.
- Orientation is **portrait (iOS)**, confirmed with the product owner. This
  matches the existing `codex.md` mandate, so no rule change is required.

## Design tokens (single source of truth)

All token *values* live in the engine-agnostic `UiTheme`
(`Packages/com.runechess.core/Runtime/UiTheme.cs`) so the core package and its
`Core Smoke` checks own them. The Unity layer maps them to `Color` in
`GameColors` (`Assets/Scripts/Presentation/GameColors.cs`). Do not hardcode new
colours or sizes in views — add a token.

### Rarity tiers (card borders / cost gem)

| Rarity    | Token            | Packed hex |
|-----------|------------------|------------|
| Common    | `CommonColor`    | `#9AA4B0`  |
| Rare      | `RareColor`      | `#4A7ED1`  |
| Epic      | `EpicColor`      | `#8662BD`  |
| Legendary | `LegendaryColor` | `#D9A441`  |

Progression reads cool-neutral → warm-gold as value rises, so a card's worth is
legible at a glance from its frame alone.

### Rune palette (match-3 board)

`UiTheme.RuneColor(RuneType)` is the one source for the six colours
(red `#C94B4B`, blue `#4A7ED1`, green `#54A06A`, yellow `#DFBF4F`,
purple `#8662BD`, white `#E8E2D2`). Runes must also carry a distinct **shape/
symbol**, not colour alone, for colour-blind readability.

### Synergy strength (alliance panel)

`UiTheme.SynergyTierColor(SynergyStrength)`: Building `#6B7280` (warming up),
Active `#5BC0A6` (online), Maxed `#E2B84B` (capped).

### Spacing and type scale

- Spacing (`UiTheme.SpacingScale`): 4 / 8 / 12 / 16 / 24 / 32. Use steps, never
  arbitrary gaps.
- Type (`UiTheme.TypeScale`): 12 caption / 16 body / 20 subtitle / 28 title /
  40 display.
- Shape: radius 6 / 10 / 16; border 1 / 2 / 4; unit bar height 6; HUD bar 14.

## Portrait combat layout

Stacked top-to-bottom to suit a single thumb on a tall screen:

1. **Top HUD bar** — round/phase, run HP, gold, player level + XP, win/loss
   streak, phase timer, menu.
2. **Tactical arena** — stylised 6x4 / 7x4 grid with a clear mid-line splitting
   the player half (lower) from the enemy half (upper); units face each other.
3. **Match-3 board** — the 7x7 rune board directly under the arena, the player's
   primary input surface in the thumb zone.
4. **Bottom panel** — shop row (5 tier-coloured cards), reroll and buy-XP
   buttons, and the bench slots; the alliance/synergy panel is a slide-out from
   the side so it never crowds the action.

Off-screen, the prep and combat phases share this skeleton; only the bottom
panel swaps shop/bench (prep) for combat controls (speed button, pause).

## Component anatomy

- **Unit on board**: facing sprite, star pips (tier-coloured) above, thin HP bar
  and mana bar below (`UnitBarHeight`), rarity frame, status icons.
- **Shop card**: portrait, name, cost gem, rarity-coloured border, faction/class
  icons; buy by tap or drag.
- **Bench slot**: mini-card with stars; drag to/from board; glows when a 3-copy
  merge is ready.
- **Alliance row**: icon + `current/threshold` count, tier colour, next-threshold
  hint and the heroes that would complete it (drives off `SynergyPanelModel`).

## Placeholder asset pipeline (planned)

Until original art exists, ship neutral primitives keyed to tokens (coloured
quads/frames sized by the spacing scale, rune symbols as simple glyphs). Real
sprites drop in behind the same token API without touching view code. Generating
the placeholder sprite set is a Unity-side task and is **not** covered by the
headless `Core Smoke` suite (documented verification gap — no Unity/.NET SDK in
the automation environment).

## Verification

`UiTheme` tokens are covered by `tools/CoreSmoke` (distinct rarity/rune/synergy
colours, strictly-ascending positive scales, channel unpack, unknown-enum
guards), which the `Core Smoke` GitHub Actions workflow runs on every push/PR.
Rendering itself is Unity-only and remains a documented verification gap in this
environment.
