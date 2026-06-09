using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Picks a recorded opponent for the Async PvP Arena (GDD "Будущие режимы":
    /// "Реализовать подбор записанных составов для async PvP"). Given the player's
    /// matchmaking rating and a pool of saved <see cref="ArenaCompositionSnapshot"/>s,
    /// it returns the fairest available opponent — the closest rating inside a bracket,
    /// widening the bracket until a match is found.
    ///
    /// The search is deterministic so the same pool always yields the same opponent in a
    /// smoke test: ties on rating distance break by ordinal <see cref="ArenaCompositionSnapshot.SnapshotId"/>.
    /// Matching is by rating only — never by spend — so paying players are not paired against
    /// weaker opponents (GDD: "Монетизация не должна давать прямое преимущество в PvP"). Pure
    /// logic with no I/O, so the pool is supplied by the caller and this stays Unity-free.
    /// </summary>
    public static class ArenaMatchmaker
    {
        /// <summary>Default starting rating half-width searched before widening.</summary>
        public const int DefaultBracket = 100;

        /// <summary>
        /// Returns the fairest opponent for <paramref name="playerRating"/> from
        /// <paramref name="pool"/>, or <c>null</c> if the pool has no eligible snapshot.
        /// Snapshots owned by <paramref name="excludeOwner"/> are skipped so a player is
        /// never matched against their own recorded run.
        /// </summary>
        /// <param name="bracket">Initial rating half-width; widened until a match is found.</param>
        public static ArenaCompositionSnapshot? FindOpponent(
            int playerRating,
            IReadOnlyList<ArenaCompositionSnapshot> pool,
            string? excludeOwner = null,
            int bracket = DefaultBracket)
        {
            if (pool is null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            if (playerRating < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerRating), "Player rating cannot be negative.");
            }

            if (bracket <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bracket), "Matchmaking bracket must be positive.");
            }

            var trimmedExclude = string.IsNullOrWhiteSpace(excludeOwner) ? null : excludeOwner.Trim();

            var eligible = pool
                .Where(snapshot => snapshot is not null)
                .Where(snapshot => trimmedExclude is null
                    || !string.Equals(snapshot.OwnerName, trimmedExclude, StringComparison.Ordinal))
                .ToList();

            if (eligible.Count == 0)
            {
                return null;
            }

            // Prefer opponents inside the widening bracket; if every candidate is far away the
            // final widening still returns the globally closest, so a match is always made when
            // the pool is non-empty.
            ArenaCompositionSnapshot? best = null;
            foreach (var candidate in eligible)
            {
                if (best is null || IsFairer(candidate, best, playerRating))
                {
                    best = candidate;
                }
            }

            // Respect the bracket when something fair enough exists inside it; otherwise fall back
            // to the globally closest opponent so the player is not left without a match.
            var insideBracket = eligible
                .Where(candidate => candidate.RatingDistanceTo(playerRating) <= bracket)
                .ToList();

            if (insideBracket.Count > 0)
            {
                ArenaCompositionSnapshot? inBracketBest = null;
                foreach (var candidate in insideBracket)
                {
                    if (inBracketBest is null || IsFairer(candidate, inBracketBest, playerRating))
                    {
                        inBracketBest = candidate;
                    }
                }

                return inBracketBest;
            }

            return best;
        }

        private static bool IsFairer(
            ArenaCompositionSnapshot candidate,
            ArenaCompositionSnapshot current,
            int playerRating)
        {
            var candidateDistance = candidate.RatingDistanceTo(playerRating);
            var currentDistance = current.RatingDistanceTo(playerRating);

            if (candidateDistance != currentDistance)
            {
                return candidateDistance < currentDistance;
            }

            // Deterministic tie-break so the same pool always resolves to the same opponent.
            return string.CompareOrdinal(candidate.SnapshotId, current.SnapshotId) < 0;
        }
    }
}
