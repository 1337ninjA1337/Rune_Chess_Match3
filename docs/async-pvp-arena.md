# Async PvP Arena — Design

Source of truth: `GDD_Rune_Chess_Match3.md` → "Будущие режимы" → "Async PvP Arena":

> Игрок сражается против записанных составов других игроков. Это проще и стабильнее
> для мобильной версии, чем live PvP.

This document fixes the design for the async arena so the follow-up implementation
tasks (`Реализовать сохранение состава`, `Реализовать подбор записанных составов`)
build against one agreed schema. It is a backlog feature: it must not pull focus from
the P0 PvE MVP, and nothing here adds live netcode.

## Goal

Let a player fight a steady stream of opponents without a live match. Each opponent is
a **recorded composition** — another player's finished board, saved as data and replayed
against the player as an AI-controlled enemy. This sidesteps the hard parts of live PvP
(matchmaking latency, reconnection, parallel rounds) while still feeling competitive.

## Why recorded compositions

- **Mobile stability.** No persistent connection, no opponent drop-outs. A run can be
  fought offline against a snapshot already on device.
- **Reuse of the PvE core.** A recorded composition is just an enemy board. The existing
  `BattleState` auto-battle + match-3 loop replays it with no new combat code.
- **Deterministic fairness.** Opponents are chosen by rating only, never by spend, so the
  arena stays non-pay-to-win (GDD: "Монетизация не должна давать прямое преимущество в PvP").

## What gets recorded

A snapshot stores only gameplay choices, never account or purchase state. The schema is
`ArenaCompositionSnapshot` in `Packages/com.runechess.core/Runtime`:

| Field          | Meaning                                                        |
| -------------- | -------------------------------------------------------------- |
| `SnapshotId`   | Stable id for the saved record (also the matchmaking tie-break)|
| `OwnerName`    | Display name of the player who recorded it                     |
| `Rating`       | Matchmaking rating (MMR) used to pair fair opponents          |
| `CommanderId`  | Commander the owner played                                     |
| `Heroes`       | Placed heroes: `HeroId`, `Stars`, `Row`, `Column`             |
| `ArtifactIds`  | Artifacts the owner held, since they shape combat             |

Each hero is an `ArenaHeroPlacement`: a flat, serializable value (ids + grid coordinates)
rather than a live `BoardHero`, because a snapshot must survive being saved, shipped to
another device, and replayed long after the original run ended. Placements are stored in
the **owner's own player-side frame**; mirroring them onto the enemy half is a replay-time
concern (see "Replaying a snapshot").

### Validation invariants

`ArenaCompositionSnapshot` enforces, at construction:

- non-blank `SnapshotId`, `OwnerName`, `CommanderId`;
- `Rating >= 0`;
- at least one hero and at most `MaxHeroes` (every player-side cell);
- each hero on a player-side cell, no two heroes sharing a cell;
- `1 <= Stars <= 3`;
- no blank or duplicate artifact ids.

This guarantees any snapshot in a pool is structurally replayable. Catalog existence of
hero/commander ids is the builder's concern, because the builder captures from a live run
where those ids are already valid.

## Saving a composition

`ArenaSnapshotBuilder.Capture(run, snapshotId, ownerName, rating)` records the placed
team from a `RunState` into a snapshot. It is a pure transform with no I/O — persistence
and any future upload belong to the presentation/backend layer. It refuses to record a run
with no placed heroes.

When to capture is a product decision left to presentation: the natural trigger is the end
of a successful PvE run, recording the final board the player won with.

## Matchmaking

`ArenaMatchmaker.FindOpponent(playerRating, pool, excludeOwner, bracket)` returns the
fairest opponent from a supplied pool of snapshots:

- candidates are filtered to exclude the player's own recorded runs (`excludeOwner`);
- the closest rating **inside the bracket** wins; ties break by ordinal `SnapshotId` so
  the result is deterministic and smoke-testable;
- if nothing sits inside the bracket, it falls back to the globally closest opponent so a
  non-empty pool always yields a match.

The pool is passed in by the caller. For the MVP this can be a small bundled set of seed
compositions; a later backend can serve a live pool by the same interface.

## Replaying a snapshot (future task)

Turning a snapshot into a fought battle is **not** part of these foundation tasks. The
intended path, recorded here so the schema fits it:

1. Mirror each `ArenaHeroPlacement` from the owner's player-side frame to the enemy half
   (`row → Rows - 1 - row`).
2. Rebuild `HeroInstance`s from `HeroId` + `Stars` via `HeroCatalog`.
3. Apply the owner's `CommanderId` and `ArtifactIds` to the enemy side.
4. Run the standard `BattleState` loop; the player prepares and fights as in PvE.

This reuses the existing combat module wholesale, which is the whole point of recording
compositions instead of building live PvP.

## Rating update (future task)

Win/loss rating adjustment (e.g. Elo) is intentionally deferred. The schema already carries
`Rating`, so an update step can be added without changing stored snapshots.

## Out of scope

- Live netcode, real-time opponents, reconnection.
- Backend storage, upload/download, anti-cheat.
- Rating/ladder progression and seasonal resets.

These belong to later backlog tasks; this design only fixes the on-device data schema, the
save transform, and the matchmaking selection so those tasks have a stable base.
