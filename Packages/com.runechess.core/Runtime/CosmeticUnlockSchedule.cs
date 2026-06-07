using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One cosmetic unlock entry: the cosmetic and the account level that unlocks it
    /// (GDD "Метапрогрессия": после забега игрок получает косметику и визуальные эффекты рун).
    /// </summary>
    public sealed record CosmeticUnlock(string CosmeticId, int RequiredAccountLevel);

    /// <summary>
    /// The account-level schedule that unlocks the MVP cosmetics as the player gains account
    /// levels across runs (GDD "Метапрогрессия": косметику, визуальные эффекты рун). Like
    /// <see cref="CommanderUnlockSchedule"/> and <see cref="StartingArtifactUnlockSchedule"/>
    /// the gating is non-pay-to-win: cosmetics are earned by playing, never bought, and
    /// purely visual — they grant no combat power at all (enforced by
    /// <see cref="CosmeticDefinition"/> having no stat fields).
    ///
    /// The first entry (the default board skin) is available from account level one so a
    /// fresh account always has a cosmetic applied; the rest unlock one per account level so
    /// early metaprogression has a visible payoff. Pure data so the unlock maths can be
    /// smoke-tested without Unity.
    /// </summary>
    public static class CosmeticUnlockSchedule
    {
        /// <summary>
        /// Cosmetic unlocks in unlock order. The first entry unlocks at level one; the rest
        /// unlock one per account level. Every id resolves to a <see cref="CosmeticCatalog"/>
        /// entry (validated in the static constructor) and every catalog cosmetic has exactly
        /// one entry here.
        /// </summary>
        public static IReadOnlyList<CosmeticUnlock> Entries { get; } = Array.AsReadOnly(new[]
        {
            new CosmeticUnlock("board_classic", 1),
            new CosmeticUnlock("rune_glow", 2),
            new CosmeticUnlock("board_obsidian", 3),
            new CosmeticUnlock("rune_ember_trail", 4),
            new CosmeticUnlock("hero_banner_gold", 5)
        });

        private static readonly IReadOnlyDictionary<string, int> RequiredLevelById =
            Entries.ToDictionary(
                entry => entry.CosmeticId,
                entry => entry.RequiredAccountLevel,
                StringComparer.OrdinalIgnoreCase);

        static CosmeticUnlockSchedule()
        {
            foreach (var entry in Entries)
            {
                if (!CosmeticCatalog.TryGet(entry.CosmeticId, out _))
                {
                    throw new InvalidOperationException(
                        $"Cosmetic unlock '{entry.CosmeticId}' has no catalog definition.");
                }

                if (entry.RequiredAccountLevel < 1)
                {
                    throw new InvalidOperationException(
                        $"Cosmetic unlock '{entry.CosmeticId}' declares an account level below one.");
                }
            }

            if (Entries.Count != CosmeticCatalog.All.Count)
            {
                throw new InvalidOperationException(
                    "Every catalog cosmetic must have exactly one unlock entry.");
            }
        }

        /// <summary>How many cosmetics exist in the unlock pool.</summary>
        public static int TotalCount => Entries.Count;

        /// <summary>The account level required to unlock a cosmetic; throws for unknown ids.</summary>
        public static int RequiredLevel(string cosmeticId)
        {
            if (cosmeticId is not null && RequiredLevelById.TryGetValue(cosmeticId.Trim(), out var level))
            {
                return level;
            }

            throw new ArgumentException($"Cosmetic '{cosmeticId}' has no unlock entry.", nameof(cosmeticId));
        }

        /// <summary>True when the cosmetic is unlocked at the given account level.</summary>
        public static bool IsUnlocked(string cosmeticId, int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return cosmeticId is not null
                && RequiredLevelById.TryGetValue(cosmeticId.Trim(), out var level)
                && accountLevel >= level;
        }

        /// <summary>How many cosmetics are unlocked at the given account level.</summary>
        public static int UnlockedCountForLevel(int accountLevel) => UnlockedIdsForLevel(accountLevel).Count;

        /// <summary>
        /// The cosmetic ids unlocked at the given account level, in unlock (schedule) order
        /// so callers list them the same way every time.
        /// </summary>
        public static IReadOnlyList<string> UnlockedIdsForLevel(int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return Entries
                .Where(entry => accountLevel >= entry.RequiredAccountLevel)
                .Select(entry => entry.CosmeticId)
                .ToList();
        }

        /// <summary>
        /// The next cosmetic unlock strictly above the given account level, or <c>null</c>
        /// when every cosmetic is already unlocked. Used to show a "next unlock" hint.
        /// </summary>
        public static CosmeticUnlock? NextUnlock(int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return Entries
                .Where(entry => entry.RequiredAccountLevel > accountLevel)
                .OrderBy(entry => entry.RequiredAccountLevel)
                .ThenBy(entry => entry.CosmeticId, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }
}
