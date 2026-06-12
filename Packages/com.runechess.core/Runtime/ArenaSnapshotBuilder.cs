using System;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Saves a player's finished board as an <see cref="ArenaCompositionSnapshot"/> for the
    /// Async PvP Arena (GDD "Будущие режимы": "Реализовать сохранение состава игрока для async
    /// PvP"). The arena never fights a live opponent; instead it records each player's run and
    /// replays the saved composition later, which is "проще и стабильнее для мобильной версии,
    /// чем live PvP" (GDD).
    ///
    /// Capture reads only gameplay choices from the run — the placed team, the commander, and
    /// the owned artifacts — so the resulting snapshot carries no paid advantage into PvP. It is
    /// a pure transform with no I/O: persistence and networking belong to the presentation/
    /// backend layer, keeping this logic smoke-testable without Unity.
    /// </summary>
    public static class ArenaSnapshotBuilder
    {
        /// <summary>
        /// Records the current placed team in <paramref name="run"/> as a snapshot owned by
        /// <paramref name="ownerName"/> at the given matchmaking <paramref name="rating"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The run is null.</exception>
        /// <exception cref="InvalidOperationException">The run has no placed heroes to record.</exception>
        public static ArenaCompositionSnapshot Capture(
            RunState run,
            string snapshotId,
            string ownerName,
            int rating)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (run.Team.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot record an arena composition from a run with no placed heroes.");
            }

            var heroes = run.Team
                .Select(ArenaHeroPlacement.From)
                .ToList();

            var artifactIds = run.Artifacts
                .Select(artifact => artifact.Id)
                .ToList();

            return new ArenaCompositionSnapshot(
                snapshotId,
                ownerName,
                rating,
                run.Commander.Id,
                heroes,
                artifactIds);
        }

        /// <summary>
        /// Returns <paramref name="snapshot"/> with its matchmaking rating moved by one Elo step
        /// after a recorded match against <paramref name="opponentRating"/> (GDD "Будущие режимы":
        /// async PvP против записанных составов). <see cref="ArenaSnapshotBuilder.Capture"/> stamps
        /// a snapshot with a rating once; this carries the post-match rating back onto the stored
        /// record so the next matchmaking pass pairs it fairly. The composition (heroes, commander,
        /// artifacts, owner, id) is untouched — only <see cref="ArenaCompositionSnapshot.Rating"/>
        /// changes — so a recorded run keeps its identity while its ladder position self-corrects.
        ///
        /// It is a thin, pure wrapper over <see cref="ArenaRatingRules.Update"/>: a win raises the
        /// stored rating, a loss lowers it, the swing is capped by <paramref name="kFactor"/>, and
        /// the result never goes negative. Rating reads only the two ratings, never spend, so the
        /// ladder stays non-pay-to-win (GDD: "Монетизация не должна давать прямое преимущество в
        /// PvP"). No I/O — persistence belongs to the backend layer — so it stays smoke-testable.
        /// </summary>
        /// <exception cref="ArgumentNullException">The snapshot is null.</exception>
        public static ArenaCompositionSnapshot WithUpdatedRating(
            ArenaCompositionSnapshot snapshot,
            int opponentRating,
            bool won,
            int kFactor = ArenaRatingRules.DefaultKFactor)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var updatedRating = ArenaRatingRules.Update(snapshot.Rating, opponentRating, won, kFactor);
            return snapshot with { Rating = updatedRating };
        }
    }
}
