using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One commander unlock entry: the commander and the account level that unlocks it
    /// (GDD "Метапрогрессия": после забега игрок получает новых командиров).
    /// </summary>
    public sealed record CommanderUnlock(string CommanderId, int RequiredAccountLevel);

    /// <summary>
    /// The account-level schedule that unlocks the MVP commanders as the player gains
    /// account levels across runs (GDD "Метапрогрессия": новых командиров). This is the
    /// non-pay-to-win progression the <see cref="AccountProgress"/> comment defers to:
    /// unlocks are gated by account level (earned by playing), never by purchase, and grant
    /// access only — not combat power.
    ///
    /// The catalog default commander is available from account level one so a fresh account
    /// can always start a run; the remaining commanders unlock one per account level so early
    /// metaprogression has a visible payoff. Pure data so the unlock maths can be smoke-tested
    /// without Unity.
    /// </summary>
    public static class CommanderUnlockSchedule
    {
        /// <summary>
        /// Commander unlocks in unlock order. The catalog default unlocks at level one; the
        /// remaining commanders unlock one per account level. Every commander in
        /// <see cref="CommanderCatalog.All"/> has exactly one entry here.
        /// </summary>
        public static IReadOnlyList<CommanderUnlock> Entries { get; } = Array.AsReadOnly(new[]
        {
            new CommanderUnlock(CommanderCatalog.RuneArchon.Id, 1),
            new CommanderUnlock(CommanderCatalog.Warlord.Id, 2),
            new CommanderUnlock(CommanderCatalog.Alchemist.Id, 3)
        });

        private static readonly IReadOnlyDictionary<string, int> RequiredLevelById =
            Entries.ToDictionary(
                entry => entry.CommanderId,
                entry => entry.RequiredAccountLevel,
                StringComparer.OrdinalIgnoreCase);

        /// <summary>The account level required to unlock a commander; throws for unknown ids.</summary>
        public static int RequiredLevel(string commanderId)
        {
            if (commanderId is not null && RequiredLevelById.TryGetValue(commanderId.Trim(), out var level))
            {
                return level;
            }

            throw new ArgumentException($"Commander '{commanderId}' has no unlock entry.", nameof(commanderId));
        }

        /// <summary>True when the commander is unlocked at the given account level.</summary>
        public static bool IsUnlocked(string commanderId, int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return commanderId is not null
                && RequiredLevelById.TryGetValue(commanderId.Trim(), out var level)
                && accountLevel >= level;
        }

        /// <summary>How many commanders are unlocked at the given account level.</summary>
        public static int UnlockedCountForLevel(int accountLevel) => UnlockedIdsForLevel(accountLevel).Count;

        /// <summary>
        /// The commander ids unlocked at the given account level, in <see cref="CommanderCatalog.All"/>
        /// order so callers list them the same way the selection screen does.
        /// </summary>
        public static IReadOnlyList<string> UnlockedIdsForLevel(int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return CommanderCatalog.All
                .Where(commander => accountLevel >= RequiredLevel(commander.Id))
                .Select(commander => commander.Id)
                .ToList();
        }

        /// <summary>
        /// The next commander unlock strictly above the given account level, or <c>null</c> when
        /// every commander is already unlocked. Used to show a "next unlock" hint.
        /// </summary>
        public static CommanderUnlock? NextUnlock(int accountLevel)
        {
            if (accountLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(accountLevel), "Account level starts at one.");
            }

            return Entries
                .Where(entry => entry.RequiredAccountLevel > accountLevel)
                .OrderBy(entry => entry.RequiredAccountLevel)
                .ThenBy(entry => entry.CommanderId, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }
}
