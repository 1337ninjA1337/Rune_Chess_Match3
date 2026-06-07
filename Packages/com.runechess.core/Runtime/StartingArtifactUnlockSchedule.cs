using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One starting-artifact unlock entry: the artifact and the account level that unlocks
    /// it as a run-start option (GDD "Метапрогрессия": после забега игрок получает новые
    /// стартовые артефакты).
    /// </summary>
    public sealed record StartingArtifactUnlock(string ArtifactId, int RequiredAccountLevel);

    /// <summary>
    /// The account-level schedule that unlocks the MVP starting artifacts as the player gains
    /// account levels across runs (GDD "Метапрогрессия": новые стартовые артефакты). Like
    /// <see cref="CommanderUnlockSchedule"/> the gating is non-pay-to-win: unlocks are earned
    /// by playing, never bought, and only grant access to a run-start option — not combat
    /// power beyond what the artifact already does in a run.
    ///
    /// The curated pool is the mild <see cref="ArtifactRarity.Common"/> economy/utility
    /// artifacts so a starting choice never front-loads a powerful rare effect. The first
    /// entry is available from account level one so a fresh account always has a starting
    /// artifact to pick; the rest unlock one per account level so early metaprogression has a
    /// visible payoff. Pure data so the unlock maths can be smoke-tested without Unity.
    /// </summary>
    public static class StartingArtifactUnlockSchedule
    {
        /// <summary>
        /// Starting-artifact unlocks in unlock order. The first entry unlocks at level one; the
        /// rest unlock one per account level. Every id resolves to an
        /// <see cref="ArtifactCatalog"/> entry (validated in the static constructor).
        /// </summary>
        public static IReadOnlyList<StartingArtifactUnlock> Entries { get; } = Array.AsReadOnly(new[]
        {
            new StartingArtifactUnlock("merchant_seal", 1),
            new StartingArtifactUnlock("apprentice_tome", 2),
            new StartingArtifactUnlock("iron_banner", 3),
            new StartingArtifactUnlock("spark_capacitor", 4),
            new StartingArtifactUnlock("warding_totem", 5)
        });

        private static readonly IReadOnlyDictionary<string, int> RequiredLevelById =
            Entries.ToDictionary(
                entry => entry.ArtifactId,
                entry => entry.RequiredAccountLevel,
                StringComparer.OrdinalIgnoreCase);

        static StartingArtifactUnlockSchedule()
        {
            foreach (var entry in Entries)
            {
                if (!ArtifactCatalog.TryGet(entry.ArtifactId, out _))
                {
                    throw new InvalidOperationException(
                        $"Starting-artifact unlock '{entry.ArtifactId}' has no catalog definition.");
                }

                if (entry.RequiredAccountLevel < 1)
                {
                    throw new InvalidOperationException(
                        $"Starting-artifact unlock '{entry.ArtifactId}' declares an account level below one.");
                }
            }
        }

        /// <summary>How many starting artifacts exist in the unlock pool.</summary>
        public static int TotalCount => Entries.Count;

        /// <summary>The account level required to unlock a starting artifact; throws for unknown ids.</summary>
        public static int RequiredLevel(string artifactId)
        {
            if (artifactId is not null && RequiredLevelById.TryGetValue(artifactId.Trim(), out var level))
            {
                return level;
            }

            throw new ArgumentException($"Starting artifact '{artifactId}' has no unlock entry.", nameof(artifactId));
        }

        /// <summary>True when the starting artifact is unlocked at the given account level.</summary>
        public static bool IsUnlocked(string artifactId, int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return artifactId is not null
                && RequiredLevelById.TryGetValue(artifactId.Trim(), out var level)
                && accountLevel >= level;
        }

        /// <summary>How many starting artifacts are unlocked at the given account level.</summary>
        public static int UnlockedCountForLevel(int accountLevel) => UnlockedIdsForLevel(accountLevel).Count;

        /// <summary>
        /// The starting-artifact ids unlocked at the given account level, in unlock (schedule)
        /// order so callers list them the same way every time.
        /// </summary>
        public static IReadOnlyList<string> UnlockedIdsForLevel(int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return Entries
                .Where(entry => accountLevel >= entry.RequiredAccountLevel)
                .Select(entry => entry.ArtifactId)
                .ToList();
        }

        /// <summary>
        /// The next starting-artifact unlock strictly above the given account level, or
        /// <c>null</c> when every starting artifact is already unlocked. Used to show a
        /// "next unlock" hint.
        /// </summary>
        public static StartingArtifactUnlock? NextUnlock(int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return Entries
                .Where(entry => entry.RequiredAccountLevel > accountLevel)
                .OrderBy(entry => entry.RequiredAccountLevel)
                .ThenBy(entry => entry.ArtifactId, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }
}
