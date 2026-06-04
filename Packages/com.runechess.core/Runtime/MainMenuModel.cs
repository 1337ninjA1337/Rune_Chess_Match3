using System;

namespace RuneChess.Core
{
    /// <summary>
    /// View-model for the main screen (GDD UI screen 1 "Главный экран"). It gathers the
    /// data the five main-screen elements need: the start-run call to action, the
    /// selected commander shortcut, account progress, hero collection access, and the
    /// settings entry. Keeping it in core lets the labels and counts be smoke-tested
    /// without Unity; the presentation layer only draws the buttons and reads these
    /// fields.
    /// </summary>
    public sealed record MainMenuModel(
        bool RunInProgress,
        int CurrentRound,
        int FinalRound,
        string CommanderId,
        string CommanderName,
        string CommanderPassive,
        AccountProgress Account,
        int CollectionUnlockedHeroes,
        int CollectionTotalHeroes)
    {
        /// <summary>Primary call-to-action label for the start-run button.</summary>
        public string StartRunLabel => RunInProgress ? "Продолжить забег" : "Начать забег";

        /// <summary>Meta label for the start-run button, e.g. "ROUND 1 / 10".</summary>
        public string StartRunMeta => $"ROUND {CurrentRound} / {FinalRound}";

        /// <summary>"unlocked / total" label for the hero collection access button.</summary>
        public string CollectionLabel => $"{CollectionUnlockedHeroes} / {CollectionTotalHeroes}";

        /// <summary>
        /// Build the main-menu model from the live run and account progress. A run is
        /// considered "in progress" once it has advanced past the first preparation
        /// phase or already has a team on the board.
        /// </summary>
        public static MainMenuModel Build(RunState run, AccountProgress account)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (account is null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            var commander = CommanderCatalog.Get(run.Commander.Id);
            var runInProgress = run.Round > 1
                || run.Team.Count > 0
                || run.Phase != RunPhase.Preparation;

            return new MainMenuModel(
                RunInProgress: runInProgress,
                CurrentRound: run.Round,
                FinalRound: PveRunSchedule.FinalRound,
                CommanderId: commander.Id,
                CommanderName: commander.Name,
                CommanderPassive: commander.Passive,
                Account: account,
                CollectionUnlockedHeroes: account.UnlockedHeroes,
                CollectionTotalHeroes: account.TotalHeroes);
        }
    }
}
