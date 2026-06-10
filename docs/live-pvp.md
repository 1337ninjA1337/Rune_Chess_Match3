# Live PvP — Design

Source of truth: `GDD_Rune_Chess_Match3.md` → "Будущие режимы" → "Live PvP":

> Матч на 4 или 8 игроков. Игроки параллельно проходят раунды и теряют здоровье после
> поражений.

This document fixes the design for Live PvP so the implementation tasks
(`Реализовать параллельное прохождение раундов`, `Реализовать потерю здоровья после
поражений`) build against one agreed model. It is a backlog feature: it must not pull
focus from the P0 PvE MVP, and — unlike a full networked mode — this slice adds **no live
netcode**. It fixes only the deterministic, on-device lobby state machine.

## Goal

Run an auto-battler-style elimination lobby of **4 or 8** players. Every player runs their
own board; each round is fought by all alive players **at the same time**; losers spend
from a shared health pool; the **last player standing wins**. This is the familiar
"battler lobby" shape (think the round/health loop of an auto-battler), reusing the
existing PvE combat for each individual fight.

## Why this slice first

Live multiplayer has two separable halves:

1. **The lobby rules** — who fights when, how health drains, who is eliminated, final
   placement. This is pure, deterministic game logic and is what the GDD line actually
   specifies.
2. **The transport** — sockets, matchmaking servers, reconnection, lock-step
   synchronisation, anti-cheat.

Only (1) is designed and built here. It can be fully smoke-tested without Unity or a
network, and a later transport task can drive the same model from real player input. This
mirrors how Async PvP Arena split its schema/selection from any backend (see
`docs/async-pvp-arena.md`).

## Model

The model lives in `Packages/com.runechess.core/Runtime` as pure C# records:

| Type                  | Responsibility                                                       |
| --------------------- | ------------------------------------------------------------------- |
| `LivePvpConfig`       | Balance knobs: starting health, the valid lobby sizes (4 or 8)      |
| `LivePvpParticipant`  | One seat: stable id/name, shared-pool health, final placement       |
| `LivePvpRoundOutcome` | One player's fight result for a round: won/lost and health lost     |
| `LivePvpPhase`        | `InProgress` / `Finished`                                           |
| `LivePvpMatch`        | The lobby state machine: parallel round progression + health loss   |

A participant carries **no board of its own**. Each player's actual fight is resolved by
the existing combat module (`BattleState`) and reported back to the lobby as a
`LivePvpRoundOutcome`. The lobby stays agnostic of combat numbers, which keeps the two
modules decoupled and the lobby trivially testable.

### Opening a match

`LivePvpMatch.Create(seats, config)` validates the GDD constraints at construction:

- the lobby size is **exactly 4 or 8** (`LivePvpConfig.IsValidLobbySize`);
- seat ids are distinct and non-blank;
- every player starts on `LivePvpConfig.StartingHealth` (default 100);
- the match opens at round 1, `InProgress`.

## Parallel round progression

> "Игроки параллельно проходят раунды."

`LivePvpMatch.ResolveRound(outcomes)` advances **every alive player by exactly one shared
round at once**. It requires exactly one outcome per alive participant — no extras, no
duplicates, none missing — so a round cannot be resolved with a player's fight left
unreported. Eliminated players sit the round out and are neither expected nor accepted in
the outcome set. The shared `Round` counter then advances by one for the whole lobby,
which is what "parallel" means here: there is a single global round clock, not a per-player
one.

## Health loss after defeats

> "...и теряют здоровье после поражений."

Each `LivePvpRoundOutcome` reports whether the player won and, for a loser, the `Damage`
the defeat costs. The invariant is enforced at the outcome level: a **winner takes zero**
damage, a **defeat costs at least one** health. `ResolveRound` drains each loser's reported
damage from the shared pool, clamped at zero (`LivePvpParticipant.TakeDamage`).

Damage is supplied by the caller rather than hardcoded in the lobby, because the natural
auto-battler rule scales a loss by the surviving enemy units — a computation that belongs to
the combat layer, not the lobby. The lobby only needs the final number, which keeps all
combat-balance tuning in one place (codex: "Все числовые значения баланса держи в явных
конфигурациях").

### Elimination and placement

- A player hitting **zero health is eliminated** and leaves the alive set.
- Newly eliminated players are **placed just above the survivors** (`aliveAfter + 1`), so a
  player knocked out while three remain finishes 4th.
- Players eliminated in the **same round share** that placement — a deterministic tie rule.
- The match **finishes** once one (or, in a rare mutual knockout, zero) players remain; the
  survivor is placed **1st** (`Winner`). A mutual knockout ties the last players as
  co-winners at 1st.
- `Standings` returns players best-first for an end-screen.

Resolving a round on a `Finished` match throws, so a decided lobby cannot keep fighting.

## Out of scope

- Live netcode, real-time transport, reconnection, lock-step synchronisation.
- Matchmaking/lobby services, presence, anti-cheat.
- Per-player combat itself (reused wholesale from the PvE combat module).
- Rewards, ladder rating, and seasonal progression for the mode.

These belong to later backlog tasks; this design fixes only the on-device lobby rules —
the round clock, the shared health pool, elimination, and placement — so those tasks have a
stable base.
