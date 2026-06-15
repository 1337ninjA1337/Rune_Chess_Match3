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
   A flat-perspective (pseudo-3D) tilt shrinks and insets the far enemy rows
   while the near player rows render full-size. The cell-state palette, mid-line
   divider, hover/selection overlay and depth projection are the single source of
   truth in `TacticalBoardStyle`
   (`Packages/com.runechess.core/Runtime/TacticalBoardStyle.cs`); `GameColors`
   delegates to it. The arena background comes from `PlaceholderAssetCatalog`
   (`field`/`elite`/`throne` by round archetype).
3. **Match-3 board** — the 7x7 rune board directly under the arena, the player's
   primary input surface in the thumb zone.
4. **Bottom panel** — phase-specific controls. This project has **no shop and no
   bench** (a deliberate departure from the genre); how heroes are acquired and
   placed is being redesigned and the prep-phase surface is TBD. The
   alliance/synergy panel is a slide-out from the side so it never crowds the
   action.

Off-screen, the prep and combat phases share this skeleton; the bottom panel
swaps the (TBD) prep acquisition/placement surface for combat controls (speed
button, pause).

## Component anatomy

- **Unit on board**: facing sprite, star pips (tier-coloured) above, thin HP bar
  and mana bar below (`UnitBarHeight`), rarity frame, status icons. These are the
  single source of truth in `UnitBoardPresentation`
  (`Packages/com.runechess.core/Runtime/UnitBoardPresentation.cs`): sprite facing
  per side, bronze/silver/gold star-tier pip colours, clamped HP/mana bar
  fractions, the rarity frame (shared `UiTheme.RarityColor` /
  `PlaceholderAssetCatalog.RarityFrame`), the status-badge set
  (`status.*` placeholder icons) derived from a live `BattleUnit`, and ordered
  positive animation-cue durations (attack / damage flash / ability burst / death
  fade). `GameColors` delegates star and status tints.
- **Hero card** (acquisition/placement surface, TBD mechanic): portrait, name,
  rarity-coloured border, faction/class icons. The rarity frame still uses
  `GameColors.RarityColor` regardless of how heroes are obtained.
- **Alliance row**: icon + `current/threshold` count, tier colour, next-threshold
  hint and the heroes that would complete it (drives off `SynergyPanelModel`). The
  presentation glue is `SynergyPanelPresentation`
  (`Packages/com.runechess.core/Runtime/SynergyPanelPresentation.cs`): the
  faction/class placeholder icon key, the `UiTheme.SynergyTierColor` tier colour,
  the `current/threshold` label, the expand/tooltip effect text and the beginner
  spotlight flag — so the Unity panel renders without re-deriving synergy maths.
- **Main menu** (GDD UI screen 1 «Главный экран»): five ordered entry tiles — the
  start-run hero tile (primary call-to-action, warm `UiTheme.GoldColor` accent) plus
  the commander, collection, cosmetics-shop and settings shortcuts (neutral). The
  presentation glue is `MainMenuPresentation`
  (`Packages/com.runechess.core/Runtime/MainMenuPresentation.cs`): the tile order, each
  tile's title and meta line (read from `MainMenuModel`, so the run tile flips to
  «Продолжить забег» mid-run), the primary-CTA flag and the per-destination navigation
  placeholder icon (`nav.*`) — so the Unity menu lays out the tiles without re-deriving
  any labels.
- **Reward screen** (GDD UI screen «Экран награды»): gold breakdown, three artifact choice
  cards, hero reward and the continue control (drives off `RewardScreenModel`). The
  presentation glue is `RewardScreenPresentation`
  (`Packages/com.runechess.core/Runtime/RewardScreenPresentation.cs`): each artifact card
  borrows the shared rarity-tier border (`UiTheme.RarityColor` /
  `PlaceholderAssetCatalog.RarityFrame`, resolved from the artifact's full rarity via
  `ArtifactCatalog`), and the gold total / continue CTA reuse the one warm
  `UiTheme.GoldColor` accent — so the Unity reward screen renders cards and the CTA from
  the same token sources as the rest of the overhaul.
- **Event screen** (GDD UI screen «Экран события»): a single roguelite encounter card with
  risk/reward copy and accept/decline controls (drives off `EventScreenModel`). The
  presentation glue is `EventScreenPresentation`
  (`Packages/com.runechess.core/Runtime/EventScreenPresentation.cs`): it ties the defining
  risk-vs-reward contrast to the shared rune palette (red token for the cost/downside, green
  token for the gain) and the accept CTA to the warm `UiTheme.GoldColor` accent, with a
  `HasRisk` flag so a no-downside windfall drops the risk accent and reads as a pure gain.
- **Settings screen** (GDD UI screen 10 «Настройки»): sound/music/vibration toggles, language,
  graphics and battle-speed options, and the reset-tutorial action (drives off `SettingsModel`).
  The presentation glue is `SettingsPresentation`
  (`Packages/com.runechess.core/Runtime/SettingsPresentation.cs`): it flattens the seven controls
  into one ordered, uniformly-rendered `SettingsRow` list (control, label, readable Russian value
  text, row kind — toggle/option/action, and an on-flag), and ties an enabled toggle to the green
  rune token — so the Unity settings screen renders every control the same way from one source.
- **Run summary** (GDD UI screen «Итог забега»): result headline, round progress, final roster,
  best hero and the meta rewards/unlocks (drives off `RunSummaryModel`). The presentation glue is
  `RunSummaryPresentation` (`Packages/com.runechess.core/Runtime/RunSummaryPresentation.cs`): each
  roster card borrows the shared rarity-tier border (`UiTheme.RarityColor` /
  `PlaceholderAssetCatalog.RarityFrame`), the best hero takes the warm `UiTheme.GoldColor`
  spotlight, and the result headline takes a win/loss rune-palette accent (green cleared / red
  lost) — so the Unity summary renders the roster and outcome from the same token sources.

## Placeholder asset pipeline (planned)

Until original art exists, ship neutral primitives keyed to tokens (coloured
quads/frames sized by the spacing scale, rune symbols as simple glyphs). Real
sprites drop in behind the same token API without touching view code. Generating
the placeholder sprite set is a Unity-side task and is **not** covered by the
headless `Core Smoke` suite (documented verification gap — no Unity/.NET SDK in
the automation environment).

The *required* asset set is described as data by `PlaceholderAssetCatalog`
(`Packages/com.runechess.core/Runtime/PlaceholderAssetCatalog.cs`): the single
source of truth the Unity import/generation pipeline and tooling enumerate so the
placeholder set stays complete. It lists a neutral facing unit sprite, one card
frame per rarity (tinted by `UiTheme.RarityColor`), the six rune icons (tinted by
`UiTheme.RuneColor`), one icon per faction and per class (synergy-tier tinted at
runtime), the battle arena backgrounds (`field`/`elite`/`throne`, mapped from
`PveRoundType` via `ArenaBackgroundFor`), and the main-menu navigation icons (`nav.*`,
one per entry point, runtime tinted). Each entry carries a stable `Key`; real
sprites later bind to the same key. The catalog *contract* — full category
coverage, unique keys, token-tied colours, arena mapping — is verified headless by
`tools/CoreSmoke`; the sprites themselves remain the documented Unity-only gap.

## Verification

`UiTheme` tokens are covered by `tools/CoreSmoke` (distinct rarity/rune/synergy
colours, strictly-ascending positive scales, channel unpack, unknown-enum
guards), which the `Core Smoke` GitHub Actions workflow runs on every push/PR.
`TacticalBoardStyle` is covered there too (distinct per-state fills, brighter
placement border, distinct positive mid-line, ordered hover/selection overlay,
monotonic far→near depth scale, arena-mapping parity with the asset catalog).
`UnitBoardPresentation` likewise (per-side facing, distinct star-tier colours,
clamped bar fractions, shared rarity frame, one status icon per kind with unique
keys, status derivation from a live unit, positive/ordered animation durations).
`SynergyPanelPresentation` too (icon keys that resolve in the catalog, tier colour
by strength, `current/threshold` label, tooltip that names the focus and next
breakpoint effect, a single beginner spotlight flag matching the model).
Rendering itself is Unity-only and remains a documented verification gap in this
environment.
