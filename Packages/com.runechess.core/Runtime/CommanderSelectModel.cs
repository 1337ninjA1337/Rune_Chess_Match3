using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One commander entry on the commander-selection screen (GDD UI screen 2). It
    /// carries the data the screen lists for each commander: passive description,
    /// starting bonus, recommended styles, and whether it is the current selection.
    /// </summary>
    public sealed record CommanderCard(
        string Id,
        string Name,
        string Passive,
        string StartingBonusDescription,
        IReadOnlyList<string> RecommendedStyles,
        bool IsSelected,
        bool IsUnlocked = true,
        int RequiredAccountLevel = 1)
    {
        /// <summary>Recommended styles joined for compact display, e.g. "Маги / Синие руны".</summary>
        public string RecommendedStylesLabel => string.Join(" / ", RecommendedStyles);

        /// <summary>
        /// Lock hint for a locked card (GDD "Метапрогрессия": новых командиров), or <c>null</c>
        /// when the commander is already unlocked.
        /// </summary>
        public string? UnlockHint => IsUnlocked
            ? null
            : $"Откроется на уровне аккаунта {RequiredAccountLevel}";
    }

    /// <summary>
    /// View-model for the commander-selection screen (GDD UI screen 2 "Выбор командира":
    /// list of commanders, passive description, starting bonus, recommended styles, and
    /// a confirm action). The selection logic lives here so it can be smoke-tested
    /// without Unity; the presentation layer renders the cards and a confirm button.
    /// </summary>
    public sealed record CommanderSelectModel(
        IReadOnlyList<CommanderCard> Commanders,
        string SelectedId)
    {
        /// <summary>The currently selected commander card.</summary>
        public CommanderCard Selected => Commanders.First(card => card.IsSelected);

        /// <summary>
        /// Build the model from the catalog, marking <paramref name="selectedId"/> as chosen.
        /// Every commander is treated as unlocked; use the account-aware overload to gate
        /// commanders by account level.
        /// </summary>
        public static CommanderSelectModel Build(string selectedId)
        {
            return Build(selectedId, _ => true);
        }

        /// <summary>
        /// Build the model gated by the player's account, marking each commander locked or
        /// unlocked per <see cref="CommanderUnlockSchedule"/> (GDD "Метапрогрессия": новых
        /// командиров). The selection still highlights <paramref name="selectedId"/>; the
        /// presentation layer is responsible for disabling selection of a locked commander.
        /// </summary>
        public static CommanderSelectModel Build(string selectedId, AccountProgress account)
        {
            if (account is null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            return Build(selectedId, account.IsCommanderUnlocked);
        }

        private static CommanderSelectModel Build(string selectedId, Func<string, bool> isUnlocked)
        {
            var selected = CommanderCatalog.Get(selectedId);
            var cards = CommanderCatalog.All
                .Select(commander => new CommanderCard(
                    Id: commander.Id,
                    Name: commander.Name,
                    Passive: commander.Passive,
                    StartingBonusDescription: commander.StartingBonus.Description,
                    RecommendedStyles: commander.RecommendedStyles,
                    IsSelected: string.Equals(commander.Id, selected.Id, StringComparison.OrdinalIgnoreCase),
                    IsUnlocked: isUnlocked(commander.Id),
                    RequiredAccountLevel: CommanderUnlockSchedule.RequiredLevel(commander.Id)))
                .ToList();

            return new CommanderSelectModel(cards, selected.Id);
        }

        /// <summary>Return a new model with a different commander highlighted as selected.</summary>
        public CommanderSelectModel WithSelection(string id) => Build(id);
    }
}
