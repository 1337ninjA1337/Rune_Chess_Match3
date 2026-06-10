# Monetization — Design

Source of truth: `GDD_Rune_Chess_Match3.md` → "Метапрогрессия" / "Монетизация":

> Баланс должен избегать pay-to-win. Монетизация не должна давать прямое преимущество в PvP.

This document fixes the monetization design so the backlog tasks under
"Монетизация без pay-to-win" build against one agreed model. It is a backlog feature: it
must not pull focus from the P0 PvE MVP, and nothing here ships a real store, payment SDK,
or live offers — only the **design of what may be sold** and the **enforceable rule that it
stays cosmetic**.

The whole design is data in `Packages/com.runechess.core/Runtime`:
`MonetizationCatalog` (acceptable formats + prohibitions), `MonetizationFormat`,
`MonetizationFormatKind`, `MonetizationProhibitionKind`, and `MonetizationPolicy` (the
enforceable guard).

## Principle

Everything sellable is **cosmetic or cosmetic-convenience**. Nothing sellable touches
combat power, hero strength, artifacts, or the ability to play. This keeps PvP — both the
Async Arena and Live PvP modes — decided by squad-building skill and match-3 play, never by
spend (GDD: "Монетизация не должна давать прямое преимущество в PvP").

The existing metaprogression already encodes the spirit: `CosmeticDefinition` carries no
numeric field, and account unlocks are gated by account level (earned by playing), never by
`SoftCurrency`. This design names the full set of acceptable formats and makes the
prohibitions a guard.

## Acceptable formats (GDD "Подходящие форматы")

Eight formats, each cosmetic-only (`MonetizationCatalog.AcceptableFormats`):

| Format (`MonetizationFormatKind`) | What it is                                    | Reskins run surface (`AppliesTo`) |
| --------------------------------- | --------------------------------------------- | --------------------------------- |
| `BattlePass`                      | Seasonal cosmetic reward track                | — (own track)                     |
| `HeroSkin`                        | Alternative hero look                         | `CosmeticKind.HeroSkin`           |
| `BoardSkin`                       | Alternative match-3 board look                | `CosmeticKind.BoardSkin`          |
| `RuneEffect`                      | Visual effect on rune matches                 | `CosmeticKind.RuneEffect`         |
| `CommanderPortrait`               | Alternative commander avatar art              | — (avatar)                        |
| `Emote`                           | Expressive in-match emote                     | — (social)                        |
| `CosmeticFinisher`                | Visual flourish on a kill/win                 | — (effect)                        |
| `CosmeticProgressBoost`           | Faster **cosmetic-only** progression          | — (progression)                   |

Three formats map directly onto the existing in-run `CosmeticKind` surfaces (hero/board/
rune), so they flow through the already-built `CosmeticLoadout`/`CosmeticShopModel`. The
other five live outside the run loadout (a battle pass track, an avatar, social emotes, a
finisher effect, a progression multiplier) and are surfaced by their own presentation later.

### Design notes per format

- **Battle pass** — a seasonal track of the cosmetics above; both a free and a paid lane,
  the paid lane granting only extra cosmetics, never power.
- **Hero / board / rune cosmetics** — reuse the metaprogression cosmetic pipeline; a paid
  unlock simply joins the same `CosmeticCatalog` surface without a power field.
- **Commander portraits / emotes / finishers** — pure presentation; no combat hook.
- **Cosmetic progress boost** — the one "convenience" format. It may only speed **cosmetic**
  progression (e.g. battle-pass XP), explicitly not run rewards, gold, or unlocks that affect
  power. This is why `MonetizationProgressBoost` is named "cosmetic" and validated as
  cosmetic-only.

## Prohibited formats (GDD "Нежелательные форматы")

Four formats the game must never ship (`MonetizationCatalog.Prohibitions`,
`MonetizationProhibitionKind`):

| Prohibition (`MonetizationProhibitionKind`) | Why forbidden                                      |
| ------------------------------------------- | -------------------------------------------------- |
| `SellHeroPower`                             | Direct combat advantage for money                  |
| `PaidAdvantageHeroes`                       | Breaks squad equality                              |
| `PaidCombatArtifacts`                       | Paid combat-balance shift                          |
| `MandatoryEnergyToPlay`                     | Gates play itself behind payment                   |

`MonetizationPolicy.IsProhibited` reports every one as forbidden unconditionally — there is
no "acceptable amount" of these.

## How the policy is enforced

`MonetizationPolicy` turns the principle into guards that smoke checks assert:

- **Cosmetic-only has teeth.** `IsCosmeticOnly(format)` reflects over a format's type and
  fails if it exposes *any* numeric (int/long/short/byte/float/double/decimal) property —
  the structural way a "power stat" could sneak in. `MonetizationFormat` only carries
  ids/name/kind/description/`AppliesTo`, so every catalog format passes; a future format that
  added a numeric stat would fail the guard (and `EnsureCosmeticOnly` would throw).
- **Catalog honours policy.** `CatalogHonoursPolicy()` is the single check a store/UI layer
  can call before showing any offer: every acceptable format is cosmetic-only **and** all
  four prohibitions are recognised.
- **No mandatory energy.** The run never gates play on a stamina resource: `RunState.NewRun`
  takes no energy/stamina and a fresh run advances without spending any meta resource. (The
  "energy" in the game is commander energy and the white-rune energy effect — both gameplay,
  not a pay-to-play gate.) Smoke asserts a run starts and progresses with no such resource.
- **Soft currency buys no power.** Already enforced by `AccountProgress`: spending
  `SoftCurrency` changes no unlock count; only account XP (earned by playing) advances
  unlocks. Smoke re-asserts this alongside the monetization policy.

## Out of scope

- A real store, payment SDK, receipts, restore-purchases, regional pricing.
- Live battle-pass seasons, rotation schedules, and pricing tables.
- Presentation for the five non-loadout formats (battle pass screen, emote wheel, etc.).

These belong to later backlog tasks; this design fixes only the **set** of acceptable
formats, the **prohibitions**, and the **enforceable cosmetic-only guard**, so those tasks
have a stable, non-pay-to-win base.
